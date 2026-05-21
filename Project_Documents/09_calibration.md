# Calibration — Hiệu chuẩn dụng cụ đo

## 1. Tổng quan

Module quản lý toàn bộ chu trình hiệu chuẩn: yêu cầu, thực hiện, lưu kết quả và cập nhật hạn dùng tiếp theo.

**Người dùng liên quan:**
- **QC Manager**: tạo yêu cầu hiệu chuẩn, duyệt kết quả.
- **QC Technician / External Vendor**: thực hiện hiệu chuẩn, upload chứng chỉ.

---

## 2. Business Rules

### 2.1 Chu kỳ hiệu chuẩn
- Mỗi gage có `calib_frequency_days` (ví dụ: 180 ngày, 365 ngày).
- `due_date = last_calibration + calib_frequency_days`.
- Hệ thống nhắc nhở khi `days_remaining ≤ 30`.

### 2.2 Quy trình hiệu chuẩn
```
Pending → Approved → Completed
       → Cancelled
```
- **Pending**: yêu cầu vừa tạo.
- **Approved**: QC Manager duyệt, gage được gửi đi hiệu chuẩn (`gage.status = 'CALIB'`).
- **Completed**: nhận kết quả, upload chứng chỉ, cập nhật `gage.last_calibration`.
- **Cancelled**: hủy yêu cầu (gage vẫn ở trạng thái cũ).

### 2.3 Calib Procedure
- Mỗi `gage_type` có một `procedure_id` mặc định (quy trình hiệu chuẩn theo tiêu chuẩn).
- Procedure lưu: tên, mô tả, revision, ngày ban hành, link tài liệu.
- `is_latest = true` cho version mới nhất.

### 2.4 Calib Record
Sau hiệu chuẩn, ghi lại:
- Người thực hiện (`calibrated_by` — có thể là nhân viên vendor, nhập text).
- Ngày hiệu chuẩn.
- `as_found_conditions`: tình trạng trước hiệu chuẩn (Pass/Fail/Out of tolerance).
- `adjustment_made`: giá trị điều chỉnh (nếu có).
- `temperature`, `humidity`: điều kiện môi trường lúc hiệu chuẩn.
- `storage_path`: chứng chỉ hiệu chuẩn (PDF) lưu trên MinIO.

### 2.5 Cập nhật sau hiệu chuẩn
Khi CalibRecord được tạo → tự động cập nhật:
```
gage.last_calibration = calib_record.calibration_date
gage.status = 'VALID'
gage.has_pending_calib = false
calib_request.status = 'completed'
```

### 2.6 Vendors
- Nhà cung cấp hiệu chuẩn bên ngoài (accredited lab).
- Lưu: tên, liên hệ, địa chỉ, phone, email.
- Mỗi yêu cầu hiệu chuẩn gắn với 1 vendor.

---

## 3. Workflow

### Tạo yêu cầu hiệu chuẩn
```
QC Manager mở danh sách gage sắp hết hạn (v_gage_calib_due):
  → Chọn gage cần hiệu chuẩn
  → Chọn Vendor + Procedure
  → Tạo CalibRequest (status = pending)
  → Cập nhật gage.has_pending_calib = true
```

### Duyệt và gửi đi hiệu chuẩn
```
Manager duyệt request:
  → CalibRequest.status = 'approved'
  → Gage.status = 'CALIB'
  → Ghi nhận ngày gửi
  → Notify vendor (email nếu cấu hình)
```

### Nhận kết quả hiệu chuẩn
```
Gage được trả về cùng chứng chỉ hiệu chuẩn:
  → Technician mở CalibRequest
  → Upload chứng chỉ (PDF) lên MinIO
  → Điền thông tin: calibration_date, calibrated_by, as_found_conditions,
                    adjustment_made, temperature, humidity
  → Tạo CalibRecord
  → Hệ thống tự động:
      - gage.last_calibration = calibration_date
      - gage.status = 'VALID'
      - gage.has_pending_calib = false
      - CalibRequest.status = 'completed'
```

---

## 4. Data Model

```sql
calib_vendors (
    id, name, contact, address, phone, email
)

calib_procedures (
    id, name, description,
    revision, rev_date,
    doc_link,            -- link tài liệu tiêu chuẩn
    is_latest [BOOLEAN]
)

calib_requests (
    id,
    gage_id      → gages,
    vendor_id    → calib_vendors,
    request_date [DATE DEFAULT CURRENT_DATE],
    status       [calib_req_status ENUM: pending/approved/completed/cancelled],
    created_by   → users, created_at
)

calib_records (
    id,
    calib_request_id → calib_requests,
    procedure_id     → calib_procedures,
    calibrated_by    [VARCHAR(100)],  -- tên người thực hiện (text)
    calibration_date [DATE],
    as_found_conditions [VARCHAR(100)],
    adjustment_made     [DECIMAL(8,4)],
    temperature         [DECIMAL(6,2)],
    humidity            [DECIMAL(6,2)],
    storage_path,       -- PDF chứng chỉ trên MinIO
    created_by → users, created_at
)

reminders (
    id, remind_type, remind_date,
    is_sent [BOOLEAN], content,
    created_at
)
```

---

## 5. API Endpoints

```
-- Vendors --
GET    /api/v1/calib-vendors
POST   /api/v1/calib-vendors
PUT    /api/v1/calib-vendors/{id}

-- Procedures --
GET    /api/v1/calib-procedures?isLatest=true
POST   /api/v1/calib-procedures
PUT    /api/v1/calib-procedures/{id}

-- Calib Requests --
GET    /api/v1/calib-requests?status=&gageId=
POST   /api/v1/calib-requests
PUT    /api/v1/calib-requests/{id}/approve
PUT    /api/v1/calib-requests/{id}/cancel

-- Calib Records --
GET    /api/v1/calib-records?gageId=&requestId=
POST   /api/v1/calib-records
       Body: { calibRequestId, procedureId, calibratedBy, calibrationDate,
               asFoundConditions, adjustmentMade, temperature, humidity, storageKey }

-- History --
GET    /api/v1/gages/{id}/calib-history    -- Toàn bộ lịch sử hiệu chuẩn của 1 gage
```

---

## 6. Edge Cases

- **Gage đang mượn cần hiệu chuẩn**: tạo request nhưng cần thu hồi gage về trước khi gửi đi — hệ thống cảnh báo "Gage đang được mượn bởi [tên]".
- **Nhiều request cho cùng 1 gage**: không cho tạo request mới khi đã có request `pending` hoặc `approved`.
- **Procedure hết hạn**: khi procedure có `is_latest = false`, cảnh báo khi tạo request nhưng vẫn cho chọn.
- **Chứng chỉ không đạt**: nếu `as_found_conditions` = Fail → gage vẫn ở trạng thái `EXPIRED` sau hiệu chuẩn, cần ghi nhận sửa chữa hoặc thay thế.
- **Reminder tự động**: background service chạy hàng ngày, query `v_gage_calib_due WHERE days_remaining <= 30`, tạo `reminders` và gửi Teams/email.
