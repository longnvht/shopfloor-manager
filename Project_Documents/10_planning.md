# Planning — Lập kế hoạch sản xuất

## 1. Tổng quan

Module lập kế hoạch sản xuất — phân công Job/OP vào máy và operator, hiển thị dưới dạng Gantt chart.

**Người dùng liên quan:** Planner, Manager.

---

## 2. Khái niệm cốt lõi

```
PlanningItem
 ├── Job → OP (công đoạn cần thực hiện)
 ├── Machine (máy được phân công)
 ├── Operator (người vận hành)
 ├── Shift (ca làm việc)
 └── StartTime → EndTime (thời gian dự kiến)

ShiftAssignment
 ├── User (operator)
 ├── Machine (máy)
 ├── Shift (ca)
 └── AssignedDate (ngày cụ thể)
```

---

## 3. Business Rules

### 3.1 Planning Item
- Một OP có thể được lên kế hoạch trên **nhiều máy** (split) — mỗi lần split là 1 `planning_item`.
- `start_time` và `end_time` là datetime chính xác (không chỉ ngày).
- Thời gian ước tính: `duration ≈ (setup_time + prod_time × run_qty) giờ`, nhưng Planner có thể điều chỉnh.

### 3.2 Gantt Chart
- Trục X: thời gian (ngày, tuần, tháng).
- Trục Y: máy móc (mỗi máy là 1 hàng).
- Mỗi bar = 1 `planning_item` = 1 Job/OP trên máy đó trong khoảng thời gian.
- Màu sắc bar: theo trạng thái hoặc Job.
- Hover: hiện thông tin Job, OP, Operator, thời gian.
- Drag-drop: di chuyển bar để reschedule.

### 3.3 Break Times
- Định nghĩa thời gian nghỉ giữa ca (ví dụ: 12:00–13:00, 15:00–15:30).
- Khi tính `end_time` tự động: cộng thêm thời gian nghỉ vào duration.
- Break times áp dụng cho tất cả máy, tất cả ngày (có thể mở rộng thêm exception sau).

### 3.4 Shifts
- Định nghĩa ca làm việc: `name`, `start_time`, `end_time`.
- Ví dụ: Ca 1 (06:00–14:00), Ca 2 (14:00–22:00), Ca 3 (22:00–06:00).
- `shift_assignments`: gán operator vào ca + máy + ngày cụ thể.
- Dùng để biết ai vận hành máy nào trong ngày nào — hỗ trợ audit log tại MES.

### 3.5 Conflict Detection
- Khi tạo/chỉnh sửa planning_item: kiểm tra máy có bị trùng lịch không.
- Trùng lịch = 2 item trên cùng machine có overlap về thời gian.
- Hệ thống **cảnh báo** nhưng không block (Planner quyết định).

---

## 4. Workflow

### Lập kế hoạch
```
Planner mở Planning view:
  → Chọn khoảng thời gian (tuần/tháng)
  → Xem Gantt theo máy
  → Kéo Job/OP từ danh sách chưa lên kế hoạch vào Gantt
  → Hệ thống:
      - Tính end_time = start_time + setup_time + (prod_time × run_qty) + break_times
      - Kiểm tra conflict
      - Tạo planning_item
  → Gán operator (từ shift_assignments hoặc chọn thủ công)
```

### Quản lý ca
```
Manager mở Shift management:
  → Định nghĩa shifts (Ca 1, Ca 2, Ca 3)
  → Gán từng operator vào ca + máy theo ngày
  → Xem ai làm máy nào hôm nay/tuần này
```

---

## 5. Data Model

```sql
shifts (
    id, name, start_time [TIME], end_time [TIME]
)

break_times (
    id, from_time [TIME], to_time [TIME]
)

planning_items (
    id,
    job_id      → jobs,
    part_op_id  → part_ops,
    machine_id  → machines,
    operator_id → users,
    shift_id    → shifts,
    start_time  [TIMESTAMPTZ],
    end_time    [TIMESTAMPTZ],
    note,
    created_by  → users, created_at,
    updated_by  → users, updated_at
)

shift_assignments (
    id,
    user_id       → users,
    machine_id    → machines,
    shift_id      → shifts,
    assigned_date [DATE],
    created_at
)
```

---

## 6. API Endpoints

```
-- Planning Items --
GET    /api/v1/planning?machineId=&startDate=&endDate=
       Response: [{ id, jobNumber, opNumber, machineName, operatorName,
                    startTime, endTime, shiftName }]
POST   /api/v1/planning
PUT    /api/v1/planning/{id}
DELETE /api/v1/planning/{id}

GET    /api/v1/planning/unscheduled    -- Jobs/OPs chưa có planning

-- Shifts --
GET    /api/v1/shifts
POST   /api/v1/shifts
PUT    /api/v1/shifts/{id}
DELETE /api/v1/shifts/{id}

GET    /api/v1/break-times
POST   /api/v1/break-times
PUT    /api/v1/break-times/{id}

-- Shift Assignments --
GET    /api/v1/shift-assignments?date=&machineId=&userId=
POST   /api/v1/shift-assignments
DELETE /api/v1/shift-assignments/{id}
```

---

## 7. Edge Cases

- **OP chưa có routing**: không cho lên kế hoạch — phải có OP trước.
- **Kéo bar vượt ca làm việc**: cảnh báo "Ngoài giờ ca", nhưng vẫn cho lưu.
- **Thay đổi run_qty sau khi đã lên kế hoạch**: cảnh báo "Kế hoạch có thể lỗi thời", không tự cập nhật.
- **Xóa planning_item**: chỉ cho xóa nếu chưa có `productionevent` gắn với item đó.
- **Nhiều shift cùng ngày cùng máy**: hợp lệ (máy 3 ca/ngày).
