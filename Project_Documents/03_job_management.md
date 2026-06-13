# Job Management

## 1. Tổng quan

Module quản lý đơn sản xuất (Job), thông tin sản phẩm (Part), và serial từng sản phẩm (Product).

**Người dùng liên quan:** Manager, Engineer, Planner.

---

## 2. Khái niệm cốt lõi

```
PO Line (Đơn hàng)
 └── Job (Lệnh sản xuất)
      └── Part (Sản phẩm/bản vẽ)
           └── Product (Serial — từng chiếc cụ thể)
```

- **Part**: định nghĩa sản phẩm — mã, mô tả, revision. Một Part có thể xuất hiện trong nhiều Job khác nhau.
- **Job**: lệnh sản xuất — "sản xuất X chiếc của Part Y, giao trước ngày Z".
- **Product**: từng chiếc cụ thể trong Job — được đánh serial (01, 02, 03...). Đây là đơn vị để theo dõi đo kiểm.

---

## 3. Business Rules

### 3.1 Part
- `(part_number, revision)` phải **UNIQUE** — cùng một part nhưng revision khác nhau là 2 bản ghi khác nhau.
- **Trạng thái Part**:
  - `status = 0` — Draft, chưa được duyệt routing.
  - `status = 1` — Active, đã có routing.
  - `is_complete = true` — Part đã hoàn thành toàn bộ, không tạo Job mới.
- **Confirm Part**: Manager/Engineer xác nhận Part đã đầy đủ tài liệu trước khi đưa vào sản xuất. Ghi vào `confirm_logs`.
- Khi thay đổi bản vẽ (revision mới): tạo bản ghi Part mới với revision mới, giữ nguyên bản ghi cũ.

### 3.2 Job
- `job_number` là **business key duy nhất** (ví dụ: "JB-2024-001") — do kỹ sư tự đặt, không auto-generate.
- Mỗi Job gắn với **1 Part** (1 loại sản phẩm).
- `run_qty`: số lượng sản phẩm cần sản xuất.
- `ship_by`: ngày giao hàng — dùng để cảnh báo trễ trong Dashboard.
- Sau khi tạo Job, hệ thống **tự tạo Product serials** theo `run_qty`.

### 3.3 Product (Serial)
- `serial_number` chỉ unique trong phạm vi **một Job** (không phải toàn bộ hệ thống).
- Serial được tạo tự động: "01", "02", ... "99" hoặc theo format tùy chỉnh.
- `is_complete = true` khi **tất cả dimension** trong tất cả OP của Job đều đã được đo kiểm Pass (hoặc NCR đã được đóng).
- `sort_order`: thứ tự hiển thị trong danh sách (operator chọn serial để đo kiểm).

### 3.4 PO Line
- Tùy chọn, không bắt buộc.
- Lưu thông tin đơn hàng khách hàng: PONumber, POLineNumber.
- Dùng để tracking cho báo cáo xuất hàng.

### 3.5 Import từ Excel
- Engineer thường nhận danh sách Job từ bộ phận kinh doanh dưới dạng Excel.
- Hệ thống hỗ trợ import hàng loạt với validation:
  - `job_number` không được trùng với Job đã tồn tại.
  - `part_number` + `revision` phải tồn tại trong hệ thống (hoặc tạo Part mới nếu chưa có).
  - `run_qty` phải > 0.
  - `ship_by` phải là ngày hợp lệ (không phải quá khứ).
- Import bất đồng bộ (background), trả về kết quả qua polling hoặc SignalR.

---

## 4. Workflow

### Tạo Job mới (manual)
```
Engineer tạo Job:
  1. Nhập job_number (unique), chọn Part, nhập run_qty, ship_by
  2. Optionally gắn PO Line
  → Hệ thống tạo Job record
  → Hệ thống tự tạo Product serials (01 → run_qty)
  → Job sẵn sàng nhận OP routing
```

### Import Job từ Excel
```
1. Engineer upload file Excel (.xlsx)
2. API validate từng dòng:
   - Thiếu field bắt buộc → lỗi dòng đó
   - job_number trùng → bỏ qua hoặc update (theo cấu hình)
   - part chưa tồn tại → tạo Part mới tự động
3. Tạo Job + Products hàng loạt
4. Trả về report: X thành công, Y lỗi (kèm chi tiết từng dòng lỗi)
```

### Theo dõi tiến độ Job
```
Job có 3 chỉ số:
  - TotalDim: tổng số kích thước cần đo của tất cả OP
  - CompleteDim: số kích thước đã được đo (có MeasureValue)
  - PassDim: số kích thước đo Pass
  - FailDim: số kích thước đo Fail (có NCR)

completion_pct = CompleteDim / TotalDim * 100
→ Hiển thị trong Dashboard và Process Monitor (MES)
```

---

## 5. Data Model

```sql
po_lines (id, po_number, po_line_number, customer_id)

parts (
    id, part_number, description, revision, routing_revision,
    status, is_active, is_complete,
    confirmed_by → users, confirmed_at,
    created_by, created_at, updated_by, updated_at
)

jobs (
    id, job_number [UNIQUE], run_qty, ship_by,
    part_id → parts,
    po_line_id → po_lines [nullable],
    created_at
)

products (
    id, serial_number, job_id → jobs,
    is_complete, sort_order, created_at
    UNIQUE(serial_number, job_id)
)

confirm_logs (id, user_id, part_id, action, created_at)
```

### View: v_product_completion
```sql
-- Xem nhanh tiến độ đo kiểm của từng product
SELECT product_id, serial_number, job_number, part_op_id,
       total_dims, measured_dims, pass_count, fail_count, completion_pct
FROM v_product_completion;
```

---

## 6. API Endpoints

```
-- Parts --
GET    /api/v1/parts?search=&isActive=&isComplete=
POST   /api/v1/parts
PUT    /api/v1/parts/{id}
POST   /api/v1/parts/{id}/confirm        -- Confirm part đã sẵn sàng sản xuất
DELETE /api/v1/parts/{id}               -- soft delete

-- Jobs --
GET    /api/v1/jobs?page=&search=&partId=&shipByFrom=&shipByTo=&isComplete=
POST   /api/v1/jobs
PUT    /api/v1/jobs/{id}
DELETE /api/v1/jobs/{id}
POST   /api/v1/jobs/import              -- Upload Excel
GET    /api/v1/jobs/import/{importId}   -- Polling kết quả import

-- Products --
GET    /api/v1/jobs/{jobId}/products
POST   /api/v1/jobs/{jobId}/products    -- Thêm serial thủ công (ít dùng)
PUT    /api/v1/products/{id}
DELETE /api/v1/products/{id}

-- Dành cho MES Desktop --
GET    /api/v1/mes/jobs                 -- Compact: job_number, part, ship_by, completion_pct
GET    /api/v1/mes/jobs/{id}/products   -- Danh sách serial + completion per serial
```

---

## 7. Edge Cases

- **Thêm serial sau khi đã sản xuất**: cho phép thêm Product mới vào Job đang chạy (khi run_qty thay đổi).
- **Job bị cancel**: set `is_complete = true` với note "Cancelled", không tạo NCR.
- **Trùng job_number khi import**: mặc định skip (không update), log lại để user xử lý.
- **Part không có revision**: revision có thể NULL, khi đó unique constraint chỉ trên `part_number`.
- **Xóa Job có Products đã đo kiểm**: không cho xóa, chỉ có thể đánh dấu complete.
- **ship_by trong quá khứ**: cho phép nhập nhưng hiển thị cảnh báo trong Dashboard ("Overdue").

---

## UI Redesign — Phase D (đề xuất, chưa triển khai)

**`/jobs/[id]` — Tiến độ đo kiểm + routing reference**

- Thêm progress bar "Tiến độ đo kiểm" = `CompleteDim / TotalDim` (đúng định nghĩa §4 "Theo dõi tiến độ Job"). **API mới**: `GET /api/v1/jobs/{id}/progress` → trả `{ totalDim, completeDim, passDim, failDim }` (additive query, aggregate MeasureValue theo Dimension của routing hiệu lực của Job).
- OP routing strip hiện tại trong `/jobs/[id]` (danh sách PartOp) chuyển thành **card tham chiếu read-only**: hiển thị số OP + link "Xem chi tiết routing →" sang `/parts/[id]` (Part & Routing, Phase G) — bỏ các action edit trùng lặp đang có ở trang Jobs (routing chỉ sửa từ `/parts`).

---

## UI Redesign — Phase F (đề xuất, chưa triển khai)

**`/jobs/[id]` — Serial/Product grid 4 trạng thái**

- Đổi bảng serial hiện tại thành card grid 4 màu trạng thái — giống Desktop `ProductListPage`:
  - `available` — chưa ai chọn (không có ProductionSession nào)
  - `claimed` — đã chọn (có session nhưng `started_at IS NULL`)
  - `inprogress` — đang gia công (session `started_at IS NOT NULL`, chưa complete)
  - `complete` — đã hoàn thành (`product.is_complete = true`)
- **Cần bổ sung `ProductDto`** (additive field, không migration): `sessionStatus` (`"none" | "claimed" | "inprogress" | "complete"`) + `claimedByName` — derive trong query handler từ `ProductionSession` mới nhất (theo `product_id`, chưa cancelled) tại thời điểm query.
