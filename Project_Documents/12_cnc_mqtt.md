# CNC Data & MQTT

## 1. Tổng quan

Module thu thập dữ liệu real-time từ máy CNC thông qua giao thức MQTT, lưu trữ vào database và hiển thị trên dashboard.

**Người dùng liên quan:** Manager (xem dashboard), Engineer (theo dõi máy), Planner.

---

## 2. Kiến trúc

```
Máy CNC (FANUC/Siemens/Mitsubishi)
  │
  │ FOCAS API / Serial port / OPC-UA
  ▼
MDC Agent (chạy trên PC tại máy hoặc gateway)
  │
  │ Publish MQTT message
  ▼
Mosquitto Broker (port 1883)
  │
  │ Subscribe (MQTTnet trong API)
  ▼
ShopfloorManager.API → Lưu vào PostgreSQL
  │
  │ SignalR push
  ▼
Web Client — Dashboard máy real-time
```

---

## 3. MQTT Topics

```
factory/cnc/{machineCode}/status     — Trạng thái máy (Running, Idle, Alarm, Off)
factory/cnc/{machineCode}/program    — Chương trình đang chạy
factory/cnc/{machineCode}/spindle    — Tốc độ/tải trục chính
factory/cnc/{machineCode}/feedrate   — Tốc độ tiến dao
factory/cnc/{machineCode}/position   — Tọa độ X, Y, Z
factory/cnc/{machineCode}/alarm      — Mã alarm + message
factory/cnc/{machineCode}/partcount  — Số chi tiết đã gia công
```

Topic pattern toàn bộ: `factory/cnc/#` (subscribe wildcard).

### Payload format (JSON)
```json
{
  "machineCode": "MC-01",
  "timestamp": "2024-11-15T08:30:00Z",
  "tmMode": "AUTO",
  "atMode": "MEMORY",
  "runMode": "START",
  "alarm": null,
  "alarmMessage": null,
  "spindleSpeed": 2500,
  "spindleLoad": 45,
  "feedrate": 800,
  "xPosition": 125.430,
  "yPosition": 0.000,
  "zPosition": -45.200,
  "program": "O1234",
  "partCount": 12,
  "toolNumber": 3
}
```

---

## 4. Business Rules

### 4.1 Machine Event
- Ghi `machine_events` khi có **thay đổi trạng thái** (không ghi mỗi giây):
  - Trạng thái: `IDLE → RUNNING`, `RUNNING → ALARM`, `ALARM → IDLE`...
  - Mã alarm thay đổi.
- Không ghi nếu trạng thái giống lần trước (tránh flood database).

### 4.2 Machine Code Mapping
- `machineCode` trong MQTT payload phải khớp với `machines.code` trong DB.
- Nếu không khớp: log warning, bỏ qua message.

### 4.3 Availability Calculation
```
Availability = (Total_Time - Downtime) / Total_Time × 100%
Downtime = thời gian ở trạng thái IDLE, ALARM, OFF
```
- Tính theo ca, ngày, tuần — dùng `machine_events` để tính khoảng thời gian từng trạng thái.

### 4.4 Real-time Dashboard
- API subscribe MQTT → nhận message → parse → broadcast qua SignalR Hub `MachineStatusHub`.
- Client web subscribe Hub → update UI mà không cần polling.
- Cache trạng thái mới nhất của mỗi máy trong MemoryCache (key: `machine_status_{machineCode}`).

---

## 5. Data Model

```sql
machine_events (
    id [BIGSERIAL],
    machine_id  → machines,
    created_by  → users [nullable — null = từ MQTT tự động],
    created_at,
    tm_mode     [TEXT],     -- MANUAL, AUTO, MDI
    at_mode     [TEXT],     -- MEMORY, TAPE, MDI
    run_mode    [TEXT],     -- RESET, START, ACTIVE
    alarm       [TEXT],     -- mã alarm
    alarm_message [TEXT]    -- mô tả alarm
)
```

Dữ liệu real-time (spindle, feedrate, position) **không lưu DB** — chỉ cache trong bộ nhớ và broadcast SignalR. Lý do: dữ liệu cập nhật mỗi giây → DB sẽ phình to nhanh chóng.

---

## 6. API Endpoints

```
-- Machine Status (real-time từ cache) --
GET    /api/v1/machines/status              -- Trạng thái tất cả máy
GET    /api/v1/machines/{id}/status         -- Trạng thái 1 máy
GET    /api/v1/machines/{id}/events?date=   -- Lịch sử events theo ngày

-- SignalR Hub --
Hub:   /hub/machine-status
       Client join group: "machine_{machineCode}"
       Server push: "MachineStatusUpdated" → payload: MachineStatusDto
```

---

## 7. MQTT Service Implementation

```csharp
// BackgroundService trong Infrastructure
public class MqttBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var client = new MqttFactory().CreateMqttClient();
        await client.ConnectAsync(options, ct);
        await client.SubscribeAsync("factory/cnc/#", ct);

        client.ApplicationMessageReceivedAsync += async e =>
        {
            var payload = JsonSerializer.Deserialize<CncPayload>(e.ApplicationMessage.Payload);
            var machine = await _machineRepo.FindByCodeAsync(payload.MachineCode);
            if (machine == null) return; // bỏ qua máy không có trong DB

            // Ghi event nếu trạng thái thay đổi
            var prev = _cache.Get<CncPayload>($"machine_status_{machine.Code}");
            if (HasStateChanged(prev, payload))
                await _eventRepo.AddAsync(MapToEvent(machine.Id, payload));

            // Cache trạng thái mới nhất
            _cache.Set($"machine_status_{machine.Code}", payload, TimeSpan.FromMinutes(5));

            // Broadcast SignalR
            await _hub.Clients.Group($"machine_{machine.Code}")
                      .SendAsync("MachineStatusUpdated", MapToDto(payload));
        };
    }
}
```

---

## 8. Edge Cases

- **Máy tắt nguồn**: MQTT client disconnect → Mosquitto phát Will Message → API cập nhật status = `OFF`.
- **Network intermittent**: MQTTnet hỗ trợ auto-reconnect với backoff policy.
- **Message queue khi offline**: Mosquitto lưu message (persistent) → deliver khi connection restore.
- **Nhiều nhà máy**: topic phân biệt bằng `machineCode` (đã unique toàn hệ thống).
- **Dữ liệu cũ**: nếu `timestamp` trong payload cách hiện tại > 10 phút → bỏ qua (stale data).
