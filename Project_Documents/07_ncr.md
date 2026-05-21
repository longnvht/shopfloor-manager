# NCR — Non-Conformance Report

## 1. Tổng quan

Module xử lý toàn bộ vòng đời của một sự kiện không phù hợp (non-conformance) — từ khi phát hiện kích thước fail, tạo NCR, xử lý (CPAR/Rework), đến khi đóng NCR.

**Người dùng liên quan:**
- **Operator / QC Inspector**: phát hiện và tạo NCR tại máy.
- **Manager / Engineer**: gán CPAR, gán Rework, duyệt và đóng NCR.
- **Assigned person**: nhận CPAR/Rework, thực hiện và báo hoàn thành.

---

## 2. Khái niệm cốt lõi

```
MeasureValue (Result = fail)
 └── NCR (sự kiện không phù hợp)
      └── NCRCode (tracking unit — 1 code = 1 sự kiện cần xử lý)
           ├── CPAR (Corrective & Preventive Action)
           └── Rework (Sửa chữa lại)
```

- **NCR**: ghi nhận một kích thước cụ thể bị fail, thuộc product serial nào, phòng ban nào chịu trách nhiệm, lý do là gì.
- **NCRCode**: mã theo dõi chung của sự kiện, có thể gộp nhiều NCR lại (ví dụ: nhiều kích thước fail trong cùng một lần kiểm).
- **CPAR**: hành động khắc phục và phòng ngừa — giải quyết nguyên nhân gốc rễ.
- **Rework**: sửa chữa lại chi tiết bị lỗi.

---

## 3. Business Rules

### 3.1 NCR Code
Format: `NCR-{YY}-{NNNN}` (ví dụ: `NCR-24-0001`, `NCR-24-0002`...)
- `year_code`: năm hiện tại.
- `sequence`: số thứ tự tự tăng trong năm (reset về 0001 mỗi năm mới).
- `machine`: tên/code máy CNC nơi phát sinh lỗi (ghi text để dễ đọc).

### 3.2 NCRCode Action (Trạng thái xử lý)
```
pending  → approve   (lỗi được chấp nhận sử dụng — Use As Is)
         → rework    (cần sửa lại)
         → reject    (phế phẩm — scrap)
```

### 3.3 NCRCode Status
```
open   → closed   (sau khi CPAR/Rework hoàn thành và Inspector xác nhận)
```

### 3.4 NCR Reasons
- Danh mục lý do không phù hợp, mỗi lý do gắn với một phòng ban chịu trách nhiệm.
- Ví dụ lý do:
  - "Tool wear" → Production Department
  - "Wrong material" → Material Department
  - "Drawing error" → Engineering Department
  - "Setup error" → Production Department
  - "CMM error" → QC Department

### 3.5 CPAR (Corrective & Preventive Action)
- Bắt buộc khi NCRCode.Action = `rework` hoặc `reject`.
- Gán cho một người chịu trách nhiệm (`assigned_to`).
- Có ngày thực hiện (`implement_date`) và ngày hoàn thành dự kiến (`done_date`).
- Người được gán submit CPAR document → upload lên MinIO.
- Inspector review và Approve/Reject CPAR.

### 3.6 Rework
- Gán cho operator/team thực hiện sửa chữa.
- Sau rework → FAI Final để xác nhận kích thước đã đạt.
- Nếu FAI Final Pass → Rework complete → NCR có thể đóng.

### 3.7 Teams Notification
Khi tạo NCR mới → gửi Teams webhook message với:
- NCR Code
- Job Number, Part Number
- Balloon Number, giá trị đo, tolerance
- Operator tạo NCR
- Màu card: đỏ (fail)

---

## 4. Workflow

### Tạo NCR tại máy (Desktop MES)
```
Measure Value = fail (từ 06_dimensions_fai.md)
  → Hệ thống tự mở màn hình NCR:
      1. Chọn Department (phòng ban chịu trách nhiệm)
      2. Chọn Reason (lý do — filter theo Department)
      3. Confirm
  → Hệ thống tạo:
      - NCRCode (NCR-24-XXXX)
      - NCR record (gắn MeasureValue.id)
      - Cập nhật MeasureValue.has_ncr = true, ncr_code = 'NCR-24-XXXX'
  → Gửi Teams notification
  → Balloon đỏ vẫn giữ, operator tiếp tục đo các balloon khác
```

### Xử lý NCR (Web App — Manager/Engineer)
```
Manager mở danh sách NCR (status = open):
  Chọn NCRCode → xem chi tiết (Job, OP, Balloon, fail value)
  Quyết định Action:

  1. Approve (Use As Is):
     → NCRCode.action = 'approve'
     → NCRCode.status = 'closed'
     → Không cần CPAR/Rework

  2. Rework:
     → NCRCode.action = 'rework'
     → Tạo Rework: gán AssignTo (operator), set implement_date
     → Tạo CPAR: gán AssignTo (engineer), set dates
     → Notify người được gán qua SignalR/email

  3. Reject (Scrap):
     → NCRCode.action = 'reject'
     → NCRCode.status = 'closed'
     → Ghi note lý do scrap
```

### Hoàn thành CPAR
```
Engineer được gán CPAR:
  → Thực hiện hành động khắc phục
  → Upload CPAR document
  → Mark CPAR.status = 'completed'
  → Inspector review → Approve CPAR
```

### Đóng NCR sau Rework
```
Rework xong → FAI Final Pass (xem 06_dimensions_fai.md)
  → Rework.status = 'completed'
  → Inspector xác nhận → NCRCode.status = 'closed'
  → MeasureValue của FAI Final ghi vào DB
```

---

## 5. Data Model

```sql
ncr_reasons (
    id, name, tag, title, sort_order,
    department_id → departments,
    created_by, created_at, updated_by, updated_at
)

ncr_codes (
    id, ncr_code [UNIQUE],
    year_code, sequence,
    machine_id → machines,
    action [ncr_action ENUM: pending/approve/rework/reject],
    status [ncr_status ENUM: open/closed],
    inspector_id → users,
    inspected_at, note,
    created_by → users, created_at
)

ncrs (
    id,
    ncr_code_id  → ncr_codes,
    measure_id   → measure_values,
    department_id → departments,
    reason_id    → ncr_reasons,
    created_by   → users, created_at,
    note
)

cpars (
    id, ncr_code_id → ncr_codes,
    assigned_to   → users,
    department_id → departments,
    status        [file_status ENUM: pending/approved/rejected],
    storage_path,
    implement_date, done_date,
    note,
    completed_by → users, completed_at,
    inspector_id → users, inspected_at,
    created_by → users, created_at
)

reworks (
    id, ncr_code_id → ncr_codes,
    assigned_to   → users,
    department_id → departments,
    status        [file_status ENUM],
    storage_path,
    implement_date, done_date,
    note,
    completed_by → users, completed_at,
    inspector_id → users, inspected_at,
    created_by → users, created_at
)
```

---

## 6. API Endpoints

```
-- NCR Reasons --
GET    /api/v1/ncr-reasons?departmentId=
POST   /api/v1/ncr-reasons
PUT    /api/v1/ncr-reasons/{id}
DELETE /api/v1/ncr-reasons/{id}

-- NCR Codes --
GET    /api/v1/ncr-codes?status=open&page=&jobId=
GET    /api/v1/ncr-codes/{id}
PUT    /api/v1/ncr-codes/{id}/action
       Body: { action: 'approve'|'rework'|'reject', note? }

-- NCRs --
GET    /api/v1/ncrs?ncrCodeId=
POST   /api/v1/ncrs                     -- Tạo NCR (Web)
DELETE /api/v1/ncrs/{id}

-- CPAR --
GET    /api/v1/cpars?ncrCodeId=&assignedTo=&status=
POST   /api/v1/cpars
PUT    /api/v1/cpars/{id}/complete
       Body: { storageKey, note }
PUT    /api/v1/cpars/{id}/inspect
       Body: { status: 'approved'|'rejected', note? }

-- Rework --
GET    /api/v1/reworks?ncrCodeId=&assignedTo=&status=
POST   /api/v1/reworks
PUT    /api/v1/reworks/{id}/complete
PUT    /api/v1/reworks/{id}/inspect

-- MES (tạo nhanh tại máy) --
POST   /api/v1/mes/ncr/quick
       Body: { measureValueId, departmentId, reasonId, machineId }
       Response: { ncrCode, ncrCodeId }

GET    /api/v1/mes/ncr/reasons?departmentId=
GET    /api/v1/mes/ncr/departments
```

---

## 7. Edge Cases

- **Nhiều kích thước fail trong cùng 1 lần kiểm**: mỗi fail → 1 NCR record, nhưng có thể gộp chung 1 NCRCode (operator chọn).
- **NCR bị tạo nhầm**: Inspector có thể xóa NCR record (không phải NCRCode) nếu chưa có CPAR/Rework.
- **CPAR reject**: Engineer phải submit lại, Inspector review lại. Không giới hạn số lần.
- **Rework xong nhưng FAI Final vẫn fail**: tạo NCR mới từ FAI Final, NCR cũ vẫn giữ nguyên để trace lịch sử.
- **NCR của Job đã hoàn thành**: chỉ xem, không được tạo mới.
- **NCRCode.sequence reset năm mới**: cần migration/cron job reset `sequence` counter đầu năm, không phải DB reset.
