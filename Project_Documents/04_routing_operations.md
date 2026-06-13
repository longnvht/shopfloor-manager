# Routing & Operations

## 1. Tổng quan

Module định nghĩa quy trình sản xuất — chuỗi các công đoạn (Operation) mà sản phẩm phải đi qua từ nguyên liệu đến thành phẩm.

**Người dùng liên quan:** Engineer, Manager.

---

## 2. Khái niệm cốt lõi

```
Part
 └── PartOP (Operation)  ← các bước gia công theo thứ tự
      ├── OPNumber: "10", "20", "10.1", "10.2"...
      ├── OPType: Turning, Milling, Grinding, Inspection...
      ├── SetupTime + ProdTime (giờ)
      ├── TechDocuments (Drawing, G-code, Route Card, Fixture...)
      └── Dimensions (kích thước cần kiểm tra sau OP này)
```

---

## 3. Business Rules

### 3.1 Operation Number (OPNumber)
- Dạng string nhưng thực chất là số thập phân: "10", "20", "10.1", "20.5"...
- Quy tắc đánh số: bội số 10 cho công đoạn chính, số lẻ thập phân cho công đoạn phụ/chia nhỏ.
- Sắp xếp theo `op_number_sort` (DECIMAL) — được parse từ `op_number` khi lưu.
- Không có quy tắc bắt buộc, nhưng convention thông thường:
  - 10 = Tiện thô / Rough turning
  - 20 = Phay / Milling
  - 30 = Mài / Grinding
  - 99 = Kiểm tra cuối / Final inspection

### 3.2 Routing cho Part vs Routing cho Job
- **Routing Part** (`job_id = NULL`): áp dụng cho tất cả Jobs của Part này.
- **Routing Job** (`job_id != NULL`, `is_for_job_only = true`): OP đặc biệt chỉ áp dụng cho 1 Job cụ thể.
  - Ví dụ: Job đặc biệt có thêm bước kiểm tra bổ sung không có trong routing chuẩn.
- Khi hiển thị routing của một Job: lấy tất cả OP của Part + OP riêng của Job, sort theo `op_number_sort`.

### 3.3 OP Types
- Loại công đoạn xác định: máy nào làm được, menu nào hiện trên MES, dimension loại nào cần kiểm.
- Ví dụ OPType: `CNC_TURNING`, `CNC_MILLING`, `GRINDING`, `CMM_INSPECTION`, `MANUAL_INSPECTION`.
- `mes_menu_op_types` liên kết OPType với menu MES — OP type `CMM_INSPECTION` hiện menu "FAI" trên màn hình Desktop.

### 3.4 Visibility
- `is_visible = false`: ẩn OP khỏi danh sách chọn trong MES (operator không thấy) nhưng vẫn tồn tại trong hệ thống.
- Dùng khi OP tạm thời ngừng sản xuất hoặc đang sửa đổi.

### 3.5 OP Complete
- `is_complete = true`: OP đã hoàn thành toàn bộ sản xuất (tất cả serial đã đi qua và đo kiểm xong).
- Thường set thủ công bởi Engineer/Manager sau khi xác nhận.
- OP complete → không hiện trong Process Monitor của MES.

### 3.6 Routing Revision
- `part.routing_revision`: tracking phiên bản routing hiện tại của Part.
- Khi thay đổi OP sequence, cập nhật `routing_revision` + ghi `part_op_logs`.

---

## 4. Workflow

### Tạo Routing cho Part mới
```
Engineer mở Part → tab Routing:
  1. Thêm OP: nhập OPNumber, chọn OPType, nhập SetupTime + ProdTime
  2. Hệ thống tự tính op_number_sort = CAST(op_number AS DECIMAL)
  3. Thêm Description, Note nếu cần
  4. Lặp lại cho tất cả OP
  5. Confirm routing (đánh số routing_revision)
  6. Ghi part_op_log cho mỗi thay đổi
```

### Thêm OP riêng cho một Job
```
Engineer mở Job → tab Operations:
  1. Thêm OP với is_for_job_only = true, gắn job_id
  2. OP này chỉ hiện khi làm Job đó, không ảnh hưởng Part routing chung
```

### Xem Routing tại MES (Desktop)
```
Operator chọn Job → chọn OP:
  API trả về danh sách OP sorted:
    [Part OPs (job_id IS NULL)] UNION [Job-specific OPs (job_id = this_job)]
    ORDER BY op_number_sort
  → Operator xem được tài liệu đính kèm (Drawing, G-code, Route Card)
  → Chọn OP → chọn Product Serial → vào màn hình FAI
```

---

## 5. Data Model

```sql
op_types (id, code, name, description)

mes_menu_op_types (
    mes_menu_id → mes_menus,
    op_type_id  → op_types,
    UNIQUE(mes_menu_id, op_type_id)
)

part_ops (
    id, op_number, op_number_sort,
    part_id → parts,
    op_type_id → op_types,
    job_id → jobs [nullable — NULL = áp dụng cho tất cả job],
    is_for_job_only,
    description, note,
    setup_time, prod_time,
    is_visible, is_complete,
    completed_by → users,
    created_by → users, created_at
)

part_op_logs (
    id, part_op_id → part_ops,
    action,               -- 'create', 'update', 'complete', 'document_attached'...
    user_id → users,
    tech_document_id → tech_documents [nullable],
    part_id → parts,
    created_at
)
```

---

## 6. API Endpoints

```
-- OP Types --
GET    /api/v1/op-types
POST   /api/v1/op-types
PUT    /api/v1/op-types/{id}
DELETE /api/v1/op-types/{id}

-- Part Operations (Routing) --
GET    /api/v1/parts/{partId}/ops               -- Routing của Part
GET    /api/v1/jobs/{jobId}/ops                 -- Routing hiệu lực của Job (Part OPs + Job OPs)
POST   /api/v1/parts/{partId}/ops               -- Thêm OP vào Part
POST   /api/v1/jobs/{jobId}/ops                 -- Thêm OP riêng cho Job
PUT    /api/v1/ops/{id}
DELETE /api/v1/ops/{id}
PUT    /api/v1/ops/{id}/complete                -- Đánh dấu OP hoàn thành
PUT    /api/v1/ops/{id}/visibility              -- Ẩn/hiện OP

-- Import --
POST   /api/v1/ops/import                       -- Import hàng loạt từ Excel

-- MES --
GET    /api/v1/mes/jobs/{jobId}/ops             -- Compact: id, op_number, op_type, paths tài liệu
```

---

## 7. Edge Cases

- **Trùng OPNumber trong cùng Part**: không cho phép — UNIQUE(part_id, op_number) ở tầng application (không DB constraint vì có job_id).
- **Xóa OP đã có Dimension**: không cho xóa, chỉ được ẩn (`is_visible = false`).
- **Xóa OP đã có MeasureValue**: cấm tuyệt đối vì ảnh hưởng đến audit trail chất lượng.
- **Import OP**: nếu OPNumber đã tồn tại thì update (không tạo mới) — giữ dimensions và tài liệu đính kèm.
- **op_number_sort khi OPNumber không phải số**: ví dụ "10A" → CAST sẽ fail → set `op_number_sort = NULL`, sort về cuối danh sách.

---

## UI Redesign — Phase B (đề xuất, chưa triển khai)

**Trang mới "Dimension Sheet" (`/dimsheet`)** — sidebar nhóm "Kỹ thuật".

- Mục đích: 1 trang tổng hợp toàn bộ Dimension của một Part (theo RoutingRev active), thay vì phải mở từng OP riêng lẻ — đúng workflow "Dimension Sheet" mà Engineer làm hàng ngày.
- Luồng: chọn Part (search) → tự load PartRev active + RoutingRev active → hiển thị bảng tổng hợp tất cả Dimension thuộc các PartOp của RoutingRev đó.
- Cột bảng: OP Number | Balloon | Nominal | Tol(+/−) | Max/Min | Category | IsFinal | IsTextType | Unit.
- Sort: theo `op_number_sort` rồi `balloon_sort`.
- **API mới (additive, không migration)**: `GET /api/v1/routing-revs/{id}/dimensions` → query mới `GetDimensionsByRoutingRevQuery` — JOIN PartOp → Dimension cho tất cả PartOp thuộc RoutingRev (`is_for_job_only = false`, ForJobOnly OP thuộc Job nên không xuất hiện ở đây), trả `DimensionDto[]` kèm `opId`/`opNumber`/`opNumberSort` để group/hiển thị.
- Inline edit Nominal/Tolerance: tái sử dụng API `PUT /api/v1/dimensions/{id}` đã có.

---

## UI Redesign — Phase G (đề xuất, chưa triển khai)

**Redesign `/parts/[id]` (Part & Routing detail)**

- **KPI strip** đầu trang: tổng số OP, tổng số Dimension (toàn routing), số tài liệu Approved/Pending — client-side aggregate từ data đã load (ops + dimensions per OP), không cần API mới.
- **Revision history timeline**: liệt kê PartRev + RoutingRev theo `created_at` (đã có sẵn từ `BaseEntity`), kèm `changeNote` (RoutingRev đã có field này) — hiển thị dạng timeline dọc thay cho dãy chip ngang hiện tại.
- **Drawing 2D placeholder**: khu vực preview bản vẽ DRW mới nhất (approved) của PartRev đang chọn — dùng presigned download URL từ TechDocument (API đã có `GET /api/v1/tech-documents/{id}/download-url`), hiển thị ảnh/PDF embed cạnh thông tin Part.
- **OP detail tabs**: thay 2 nút rời "Tài liệu →" / "⤓ Dims" bằng panel chi tiết OP có 2 tab — "Tài liệu" (list TechDocument theo `partOpId`, link `/documents?partOpId=...`) và "Dimension" (list Dimension theo OP, link sang `/dimsheet` lọc theo OP — Phase B).
