# Technical Documents

## 1. Tổng quan

Module quản lý tất cả tài liệu kỹ thuật: bản vẽ, chương trình G-code, route card, fixture drawing, thread drawing, tool list, CAM file, CAD file.

**Người dùng liên quan:** Engineer (upload), Inspector/QC (duyệt), Operator (xem tại máy).

---

## 2. Khái niệm cốt lõi

Tất cả tài liệu được quản lý trong **một bảng duy nhất** `tech_documents`, phân loại bằng `file_types`.

### Các loại file (`file_types`)

| Code | Tên | Gắn với | Ghi chú |
|---|---|---|---|
| `DRW` | Drawing | Part + Job | Bản vẽ 2D sản phẩm |
| `GCD` | G-Code | OP | Chương trình gia công CNC |
| `RTC` | Route Card | OP | Phiếu công nghệ gia công |
| `FXT` | Fixture Drawing | OP | Bản vẽ đồ gá |
| `THD` | Thread Drawing | OP | Bản vẽ ren |
| `TLS` | Tool List | OP | Danh sách dao cụ |
| `CAM` | CAM File | OP | File CAM (Mastercam, Hypermill...) |
| `CAD` | CAD Drawing | Part | File CAD 3D |

---

## 3. Business Rules

### 3.1 Trạng thái tài liệu (Status)
```
pending (3) → approved (1)
           → rejected (2) → [kỹ thuật viên sửa và upload lại] → pending
```
- Mọi file upload đều bắt đầu ở `pending`.
- Chỉ Inspector/QC mới được Approve hoặc Reject.
- File bị Reject kèm theo lý do từ chối (`inspect_note`).
- Chỉ file `approved` mới được sử dụng trong sản xuất (hiển thị trên MES).

### 3.2 File Code & Segment
- `code`: nhóm nhiều file thuộc cùng một lần upload/phiên bản (ví dụ: tất cả drawing của OP 10 cùng một lần nộp).
- `segment`: dùng khi file G-code bị chia thành nhiều phần (Segment 1, 2, 3...) do giới hạn bộ nhớ máy CNC.

### 3.3 Storage Path (MinIO)
File được lưu trên MinIO theo cấu trúc:
```
shopfloor-storage/
├── drawings/{part_number}/{revision}/{timestamp}_{filename}
├── gcodes/{part_number}/{op_number}/{revision}/{timestamp}_{filename}
├── routecards/{job_number}/{op_number}/{timestamp}_{filename}
├── fixtures/{job_number}/{op_number}/{timestamp}_{filename}
└── cad/{part_number}/{revision}/{timestamp}_{filename}
```
- `storage_path` trong DB lưu path tương đối trong MinIO bucket.
- Không lưu URL tuyệt đối — tránh thay đổi server.

### 3.4 Upload flow (Pre-signed URL)
File **không** đi qua API server để tránh bottleneck:
1. Client xin pre-signed URL từ API.
2. Client upload thẳng lên MinIO.
3. Client xác nhận với API → tạo record trong `tech_documents`.

### 3.5 Revision
- `revision`: phiên bản của tài liệu (ví dụ: "A", "B", "Rev.01").
- Khi upload revision mới: **không xóa** file cũ, tạo record mới với revision mới.
- Chỉ file approved mới nhất của mỗi (file_type, job/part/op) được dùng trong sản xuất.

### 3.6 G-code Library (Thư viện chung)
- `gcode_library`: G-code không gắn với job/part cụ thể (chương trình tái sử dụng, fixture program).
- Phân loại theo `fixture_category_id`.
- Cũng có workflow `pending → approved/rejected`.
- Khi download về máy CNC → ghi `gcode_receive_logs`.

### 3.7 Thông báo
- Khi upload file → Inspector nhận **SignalR notification** "Có tài liệu mới cần duyệt".
- Khi Approve/Reject → Engineer nhận notification kết quả.
- Teams webhook (nếu cấu hình): gửi message khi có tài liệu quan trọng bị reject.

---

## 4. Workflow

### Upload tài liệu
```
Engineer chọn Job → OP → loại file (GCD, RTC...)
  → Chọn file từ máy tính
  → Gọi POST /files/upload-url → nhận pre-signed URL
  → Upload file thẳng lên MinIO
  → Gọi POST /tech-documents với { fileTypeId, jobId, partOpId, storageKey, revision, code }
  → Status = pending
  → Inspector nhận SignalR notification
```

### Duyệt tài liệu (Inspector)
```
Inspector mở danh sách pending:
  → Xem file (click → mở MinIO pre-signed download URL)
  → Approve:
      PUT /tech-documents/{id}/inspect { status: 'approved' }
      → Status = approved
      → Engineer nhận notification
  → Reject:
      PUT /tech-documents/{id}/inspect { status: 'rejected', inspectNote: '...' }
      → Status = rejected, ghi lý do
      → Engineer nhận notification + lý do
      → Engineer sửa và upload lại → tạo record mới
```

### Xem tài liệu tại MES (Desktop Operator)
```
Operator chọn Job → OP
  → Hiện buttons: [Drawing] [G-code] [Route Card] [Fixture]
     (chỉ hiện nếu có file approved)
  → Click Drawing → gọi GET /mes/ops/{id}/documents?type=DRW
  → API trả về pre-signed download URL (thời hạn 15 phút)
  → Desktop mở file PDF/viewer
```

---

## 5. Data Model

```sql
file_types (
    id, name, code [UNIQUE],
    folder,                      -- thư mục MinIO
    is_gcode, is_segment,
    requires_job_number, requires_part_number,
    requires_op_number, requires_revision,
    sort_order
)

tech_documents (
    id [BIGSERIAL], file_type_id → file_types,
    job_id → jobs [nullable],
    part_id → parts [nullable],
    part_op_id → part_ops [nullable],
    storage_path,       -- path trong MinIO
    description, revision,
    code,               -- nhóm file cùng lần upload
    segment,            -- phân đoạn G-code
    status [file_status ENUM: pending/approved/rejected],
    inspector_id → users,
    inspected_at, inspect_note,
    created_by → users, created_at,
    deleted_at          -- soft delete
)

file_logs (
    id, tech_document_id → tech_documents,
    action,             -- 'upload', 'approve', 'reject', 'delete'
    user_id → users,
    note, created_at
)

gcode_library (
    id, name, file_name, revision, description,
    storage_path, fixture_category_id → fixture_categories,
    status [file_status],
    inspector_id → users, inspected_at,
    created_by → users, created_at,
    updated_by → users, updated_at,
    deleted_at
)

gcode_receive_logs (
    id, job_id, part_op_id, machine_id, user_id,
    storage_path, created_at
)
```

---

## 6. API Endpoints

```
-- File upload --
POST   /api/v1/files/upload-url
       Body: { fileName, contentType, folder }
       Response: { uploadUrl, storageKey }  -- pre-signed PUT URL

GET    /api/v1/files/download-url/{storageKey}
       Response: { downloadUrl }            -- pre-signed GET URL (15 phút)

-- Tech Documents --
GET    /api/v1/tech-documents?jobId=&partOpId=&fileTypeCode=&status=&page=
POST   /api/v1/tech-documents
       Body: { fileTypeId, jobId, partOpId, storageKey, revision, description, code, segment }
PUT    /api/v1/tech-documents/{id}/inspect
       Body: { status: 'approved'|'rejected', inspectNote? }
DELETE /api/v1/tech-documents/{id}         -- soft delete

GET    /api/v1/tech-documents/pending      -- shortcut: tất cả pending cần duyệt

-- G-code Library --
GET    /api/v1/gcode-library?fixtureCategoryId=&status=
POST   /api/v1/gcode-library
PUT    /api/v1/gcode-library/{id}/inspect

-- MES --
GET    /api/v1/mes/ops/{opId}/documents    -- Lấy docs approved cho 1 OP (kèm download URL)
GET    /api/v1/mes/files/{storageKey}/url  -- Pre-signed URL để xem file
```

---

## 7. Edge Cases

- **File quá lớn**: giới hạn upload 100MB (cấu hình Nginx `client_max_body_size`).
- **File đã Approved bị xóa**: không cho xóa hard, chỉ soft delete. File vẫn tồn tại trên MinIO.
- **Approve hàng loạt**: Inspector có thể chọn nhiều file cùng OP/code → approve tất cả cùng lúc.
- **Revision cũ sau khi có revision mới**: file revision cũ vẫn `approved`, nhưng khi hiển thị trên MES chỉ lấy revision mới nhất.
- **G-code segment**: nếu `is_segment = true` cho file type, upload phải có `segment` field. Khi xem trên MES, download tất cả segment theo thứ tự.
- **File bị reject nhưng sản xuất đang chạy**: sản xuất vẫn tiếp tục với file approved trước đó (nếu có). Hiện cảnh báo cho Engineer.

---

## UI Redesign — Phase E (đề xuất, chưa triển khai)

**`/documents` — cascading filter cho truy cập top-level (sidebar nhóm "Kỹ thuật")**

- Hiện `/documents` chỉ filter hiệu quả khi có query params truyền từ context (`partRevId`/`partOpId`/`jobId`, từ `/parts/[id]` hoặc `/jobs/[id]`). Khi vào trực tiếp từ sidebar (top-level), trang không có context để lọc.
- Thêm bộ chọn xếp lớp (cascading selector) phía trên bảng: **Part** (search) → **Drawing Rev** → (tùy chọn) **Routing Rev** → **OP**.
  - Chọn Part → load `api.parts.revisions(partId)` (đã có)
  - Chọn Drawing Rev → load `api.parts.routingRevs(revId)` (đã có)
  - Chọn Routing Rev → load `api.operations.listForRoutingRev(rrId)` (đã có)
  - Chọn OP (tùy chọn) → set `partOpId`; nếu dừng ở Drawing Rev → set `partRevId`
- Sau khi chọn xong, set query params tương ứng → tái sử dụng 100% logic filter/hiển thị hiện có của `/documents` — **không cần API mới**.
