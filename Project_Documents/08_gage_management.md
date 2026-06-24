# Gage Management — Quản lý dụng cụ đo

## 1. Tổng quan

Module quản lý toàn bộ vòng đời dụng cụ đo: danh mục, vị trí lưu trữ, mượn/trả và theo dõi trạng thái hiệu chuẩn.

**Người dùng liên quan:**
- **QC Manager**: quản lý danh mục gage, phê duyệt mượn/trả.
- **Inspector / Operator**: mượn gage để đo kiểm, trả gage sau khi dùng.
- **Engineer**: xem gage phù hợp cho từng dimension category.

---

## 2. Khái niệm cốt lõi

```
GageType (Loại dụng cụ: Micrometer, Caliper, Ring Gauge...)
 └── Gage (Từng dụng cụ cụ thể — có GageNo, SerialNo)
      ├── GageStatus (Valid / Expired / Damaged / Borrowed...)
      ├── GageLocation (Vị trí lưu trữ: Tủ A, Phòng QC...)
      │    └── GageSlot (Ngăn cụ thể: A1, A2...)
      └── BorrowTransaction (Giao dịch mượn/trả)
```

---

## 3. Business Rules

### 3.1 Gage Number (GageNo)
- `gage_no`: mã định danh dụng cụ, **UNIQUE** toàn hệ thống.
- Thường là mã in trên nhãn dán của dụng cụ (ví dụ: "MIC-001", "CAL-023").
- Không thể thay đổi sau khi tạo (dùng để trace lịch sử calibration, borrow...).

### 3.2 Gage Status
| Code | Tên | is_valid | Ý nghĩa |
|---|---|---|---|
| `VALID` | Hợp lệ | true | Đang trong hạn hiệu chuẩn, sử dụng được |
| `EXPIRED` | Hết hạn | false | Quá hạn hiệu chuẩn, không được dùng |
| `DAMAGED` | Hư hỏng | false | Bị hỏng, cần sửa chữa |
| `BORROWED` | Đang mượn | true | Đang được mượn, biết vị trí |
| `CALIB` | Đang hiệu chuẩn | false | Gửi đi hiệu chuẩn |

- Chỉ gage có `is_valid = true` mới được chọn khi nhập measure value.
- Trạng thái `EXPIRED` tự động set khi `last_calibration + calib_frequency_days < TODAY`.

### 3.3 Gage Type & Category
- `gage_types.category_id → gage_categories`: liên kết loại gage với loại kích thước.
- Khi operator đo kích thước thuộc category `LIN` → chỉ hiện gage có `gage_type.category_id = 'LIN'`.
- Một gage type có một `procedure_id` (quy trình hiệu chuẩn mặc định).

### 3.4 Calibration Due Date
```
due_date = last_calibration + calib_frequency_days
```
- `calib_frequency_days`: tần suất hiệu chuẩn (ví dụ: 180 ngày, 365 ngày).
- Khi `due_date - TODAY ≤ 30 ngày` → hiển thị cảnh báo màu vàng.
- Khi `due_date < TODAY` → status chuyển `EXPIRED`, không dùng được.
- Dashboard Gage hiển thị danh sách gage sắp hết hạn (view `v_gage_calib_due`).

### 3.5 Vị trí Gage (Location & Slot)
- `GageLocation`: phòng/khu vực lưu trữ (ví dụ: "Phòng QC", "Tủ Dụng cụ Xưởng A").
- `GageSlot`: ngăn cụ thể trong location (ví dụ: "Ngăn A1", "Khay 3").
- Mỗi gage có `default_location / default_slot` (vị trí gốc) và `current_location / current_slot` (vị trí hiện tại).
- Sau khi trả gage → `current_location = default_location`.

### 3.6 Borrow Status Flags
- `is_borrowed`: gage đang được mượn (`BorrowTransaction` status = `active`).
- `has_pending_calib`: đang có yêu cầu hiệu chuẩn chờ xử lý.
- Hai flags này là **denormalized** để query nhanh — cập nhật đồng thời với borrow_transactions và calib_requests.

### 3.7 Gage Selection trong FAI (Desktop MES) — ✅ Done (2026-06-23, cập nhật 2026-06-24)

Khi nhập kết quả đo, hệ thống filter gage theo **`Dimension.GageTypeId`** khi có (chính xác — đúng 1 loại dụng cụ, ví dụ chỉ Micrometer), fallback về `CategoryCode` (suy ra qua `GageType.Category`) khi dimension chưa được gán GageType cụ thể. Xem chi tiết thiết kế tại `06_dimensions_fai.md` §3.6 (Dimension không còn cột `CategoryId` riêng từ 2026-06-24 — tránh trùng lặp với phân loại trên `GageType`).

- **`GET /api/v1/mes/gages?gageTypeId=` ưu tiên hơn `?categoryCode=`** — `GetMesGagesQueryHandler` chọn `gageTypeId` nếu có, không kết hợp cả hai.
- **GageType code `'VIS'`** (Visual, `CategoryId = NULL`) → **bỏ qua hoàn toàn** bước chọn dụng cụ đo — dùng cho dimension kiểm bằng mắt (không có min/max số, ví dụ "No burr, no scratch"). Dimension đo góc/ren/độ nhám không có tolerance số (ví dụ "45°", "HƯỚNG REN PHẢI") **không** gán GageType `VIS` — vẫn cần GageType thật (thước góc, dưỡng ren...) và vẫn cần chọn dụng cụ.
- **UI tìm dụng cụ** (touch-friendly): nhập để filter theo `GageNo`/`Description` → kết quả hiện dạng thẻ. Click chỉ highlight (không xung đột với kéo-thả cuộn danh sách) — double-click hoặc nút "Chọn" mới xác nhận.
- **Đổi dimension → luôn yêu cầu chọn lại gage** (không carry-over từ dimension trước), **trừ khi** cùng balloon đã được đo ở serial khác trong cùng Job — khi đó tự động gợi ý lại gage đã dùng lần trước (`WorkContext.LastGageIdByBalloon`, key `PartOpId:BalloonNumber`, reset khi đổi Job).
- `MeasureValue.GageId` lưu lựa chọn — gage là **tùy chọn**, không chặn lưu kết quả đo nếu không chọn.
- **Excel import dimension** (`/dimsheet`, bulk import RoutingRev): cột `Category` đổi tên thành `GageType` (giá trị là `GageType.Code` như "MIC", "PLG" — không phải mã category rộng như "LIN" nữa). Vẫn nhận alias cột cũ `category` để tương thích file đã phát hành trước đó.

---

## 4. Workflow Mượn / Trả Gage

### Mượn gage
```
Operator/Inspector chọn gage cần mượn:
  → Kiểm tra: gage.is_borrowed = false (chưa bị mượn)
  → Kiểm tra: gage status is_valid = true (hợp lệ)
  → Điền: borrower_id, use_location_id, expected_return_date
  → Manager xác nhận
  → Tạo BorrowTransaction (status = active)
  → Cập nhật:
      gage.is_borrowed = true
      gage.current_location = use_location_id
      gage.status = 'BORROWED'
```

### Trả gage
```
Operator mang gage về:
  → Tìm BorrowTransaction đang active của gage đó
  → Xác nhận trả: return_date = TODAY
  → BorrowTransaction.status = 'returned'
  → Cập nhật:
      gage.is_borrowed = false
      gage.current_location = gage.default_location
      gage.current_slot = gage.default_slot
      gage.status = 'VALID' (nếu vẫn trong hạn calib)
```

### Hủy giao dịch mượn
```
Manager hủy giao dịch chưa hoàn tất:
  → BorrowTransaction.status = 'cancelled'
  → Reset gage về trạng thái trước khi mượn
```

---

## 5. Data Model

```sql
gage_statuses (
    id, code [UNIQUE], description, is_valid, group_code
)

gage_types (
    id, code [UNIQUE], name, description,
    procedure_id → calib_procedures,
    category_id  → gage_categories
)

gage_locations (
    id, code [UNIQUE], description,
    factory_id → factories
)

gage_slots (
    id, code, description,
    location_id → gage_locations
)

gages (
    id, gage_no [UNIQUE], serial_no,
    description, measuring_range, accuracy, unit, manufacturer,
    calib_frequency_days,
    last_calibration [DATE],
    in_service_date  [DATE],
    gage_type_id    → gage_types,
    status_id       → gage_statuses,
    default_location_id → gage_locations,
    default_slot_id     → gage_slots,
    current_location_id → gage_locations,
    current_slot_id     → gage_slots,
    is_borrowed     [BOOLEAN],
    has_pending_calib [BOOLEAN],
    vendor_id       → calib_vendors,
    note,
    factory_id → factories,
    created_at, deleted_at
)

borrow_transactions (
    id,
    gage_id      → gages,
    borrower_id  → users,
    manager_id   → users,
    borrow_date  [DATE],
    expected_return_date [DATE],
    return_date  [DATE],
    from_location_id → gage_locations,
    from_slot_id     → gage_slots,
    use_location_id  → gage_locations,
    status [borrow_status ENUM: active/returned/cancelled],
    note, created_at
)
```

### View: v_gage_calib_due
```sql
-- Gage sắp hết hạn hiệu chuẩn, sorted by due_date
SELECT id, gage_no, description, last_calibration,
       due_date, days_remaining, status, location
FROM v_gage_calib_due;
```

---

## 6. API Endpoints

```
-- Gage Master Data --
GET    /api/v1/gages?search=&statusCode=&gageTypeId=&locationId=&isBorrowed=
POST   /api/v1/gages
PUT    /api/v1/gages/{id}
DELETE /api/v1/gages/{id}          -- soft delete
GET    /api/v1/gages/calib-due     -- Danh sách sắp hết hạn

-- Gage Types / Locations / Slots --
GET    /api/v1/gage-types?categoryId=
POST   /api/v1/gage-types
PUT    /api/v1/gage-types/{id}

GET    /api/v1/gage-locations
POST   /api/v1/gage-locations
GET    /api/v1/gage-locations/{id}/slots
POST   /api/v1/gage-slots

-- Borrow Transactions --
GET    /api/v1/borrow-transactions?gageId=&borrowerId=&status=
POST   /api/v1/borrow-transactions        -- Tạo mượn
PUT    /api/v1/borrow-transactions/{id}/return    -- Trả
PUT    /api/v1/borrow-transactions/{id}/cancel    -- Hủy

-- MES --
GET    /api/v1/mes/gages?categoryCode={code}
       -- Chỉ trả gage is_valid=true, chưa bị mượn, sorted by gage_no
```

---

## 7. Edge Cases

- **Gage đang mượn hết hạn calib**: cập nhật status = `EXPIRED`, cảnh báo Manager. Người đang mượn vẫn giữ nhưng không được dùng cho lần đo mới.
- **Mượn gage đang ở trạng thái CALIB**: không cho mượn khi đang gửi hiệu chuẩn.
- **Tìm gage trong MES**: chỉ hiện gage `is_valid = true` và `is_borrowed = false` (hoặc đang được mượn bởi chính user này).
- **Xóa gage**: chỉ soft delete. Gage đã tham gia `measure_values` không thể xóa hoàn toàn.
- **Import gage hàng loạt**: từ Excel — thường dùng khi thiết lập hệ thống lần đầu.
- **Reminder**: cron job hàng ngày kiểm tra `v_gage_calib_due` → tạo `reminders` → gửi Teams/email khi `days_remaining ≤ 30`.
