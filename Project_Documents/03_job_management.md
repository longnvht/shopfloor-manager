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

---

## Bulk Import — Import đồng thời Job + Part + Routing + OP (đề xuất, chưa triển khai)

### Bối cảnh & mục tiêu

Hệ thống chạy **song song với ERP** (Epicor) — Engineer nhận dữ liệu Job/Part/OP từ ERP và hiện phải nhập lại thủ công theo từng bước riêng lẻ:

```
Hiện tại (chậm):
1. Vào /parts → "+ Tạo Part" (PartNumber, Description, RevCode)
2. Vào /parts/{id}/operations → import OP theo RoutingRev (ImportOpsDialog, đã có)
3. Vào /jobs → "+ Tạo Job" chọn PartRevId + RoutingRevId, nhập RunQty/ShipBy
```

Với khối lượng Job/Part theo ngày từ ERP, cần **1 file Excel → 1 lần import** tạo/cập nhật toàn bộ chuỗi `Part → PartRev → Routing → RoutingRev → PartOp → PoLine → Job → Product`.

### 1. Tham chiếu cấu trúc Epicor (Job/Part/Operation)

Không có kết nối DB/API trực tiếp tới Epicor (kể cả ManageData cũ cũng vậy) — nguồn dữ liệu thực tế là **BAQ (Business Activity Query) export ra Excel**, do Engineer tự chạy trong Epicor. BAQ kiểu "Job Operations" luôn trả về **dạng FLAT, 1 dòng = 1 (Job × Operation)** — các field cấp Job/Part được lặp lại trên mọi dòng OP của job đó:

| Field BAQ điển hình (Epicor) | Nguồn Epicor | Map vào entity ShopfloorManager |
|---|---|---|
| PartNum | `Erp.Part` | `Part.PartNumber` |
| PartDescription | `Erp.Part` | `Part.Description` |
| RevisionNum | `Erp.Part`/ECO | `PartRev.RevCode` |
| JobNum | `Erp.JobHead` | `Job.JobNumber` |
| OrderNum / OrderLine | `Erp.JobHead` → `OrderHed/OrderDtl` | `PoLine.PoNumber` / `PoLine.PoLineNumber` |
| ProdQty | `Erp.JobHead` | `Job.RunQty` |
| RequestDate | `Erp.JobHead` | `Job.ShipBy` |
| OprSeq | `Erp.JobOper` | `PartOp.OpNumber` |
| OpCode / ResourceGrpID | `Erp.JobOper` | `PartOp.OpTypeId` (lookup theo `OpType.Code`) |
| OpDesc | `Erp.JobOper` | `PartOp.Description` |
| EstSetHrs / EstProdHrs | `Erp.JobOper` (`JobOpDtl`) | `PartOp.SetupTime` / `PartOp.ProdTime` |

Lưu ý: `JobOper` trong Epicor về bản chất **thuộc về Job** (được copy từ "Method of Manufacturing" của Part khi tạo Job), nhưng trong thực tế các Job cùng Part+Rev thường có OP list giống nhau → có thể coi đây là **routing template** của PartRev (giống `RoutingRev.PartOps` trong hệ thống mới).

### 2. So sánh với import cũ (ManageData)

`ManageData` (`longnv-vinam/ManageData`) đã làm Excel import qua `OleDbConnection` (ACE OLEDB), nhưng **tách 2 bước**:
- `Template Import Jobs.xlsx` (JobNumber, PO, POLine, PartNumber, PartDescription, Revision, RunQty, ShipBy) → tạo Part (auto sinh RoutingRevision kế tiếp) → tạo Job → sinh Product serial
- `TemplateImportOP.xlsx` (JobNumber, OPNumber, OPType, ForOnlyJob, Description, Note, SetupTime, ProdTime) → import riêng OP cho từng Job

Đây chính là việc 2 bước gây mất thời gian mà bạn đang gặp lại. Phương án mới **gộp 2 template thành 1**, vì BAQ Epicor đã sẵn flat theo (Job, Op) — không cần Engineer tách file.

### 3. Thiết kế Excel input

**1 sheet, 1 dòng = 1 (Job, Operation)**. Header (không phân biệt hoa/thường, bỏ space — theo `ExcelImportReader.Normalize`):

| Cột | Bắt buộc | Ghi chú |
|---|---|---|
| PartNumber | ✅ | |
| PartDescription | – | dùng khi tạo Part mới; bỏ qua nếu Part đã tồn tại |
| Revision | – | mặc định `"A"` nếu trống |
| JobNumber | ✅ | |
| PONumber | – | |
| POLine | – | |
| RunQty | – | |
| ShipBy | – | ngày (Excel date hoặc `yyyy-MM-dd`) |
| OpNumber | ✅ | |
| OpType | – | code (CNC/GRIND...) — không match → warning, `OpTypeId=null` |
| OpDescription | – | |
| SetupTime | – | |
| ProdTime | – | |

### 4. Luồng xử lý (algorithm)

```
1. ExcelImportReader.Read() → List<Dictionary<string,string>>
2. Map sang ImportJobBatchRow, group theo JobNumber
3. Với mỗi nhóm (1 Job):
   a. Resolve/tạo Part theo PartNumber
   b. Resolve/tạo PartRev theo (PartId, Revision)
   c. Resolve RoutingRev active của PartRev đó (Routing "Standard")
   d. Upsert PartOps vào RoutingRev — TÁI DÙNG logic ImportOpsCommand
      (match theo OpNumber; dòng trùng OpNumber trong cùng group → dòng sau
      đè dòng trước, dedupe trước khi upsert)
   e. Resolve/tạo PoLine theo (PONumber, POLineNumber) nếu có
   f. JobNumber đã tồn tại → update RunQty/ShipBy nếu khác (++JobsUpdated);
      PartRevId/RoutingRevId/PoLineId của Job GIỮ NGUYÊN (snapshot, không đổi
      qua import). RunQty tăng → generate thêm Products (serial tiếp theo,
      giống GenerateProductsCommand); RunQty giảm → chỉ update field, KHÔNG
      xoá Products dư (có thể đã có MeasureValue), log warning.
      JobNumber mới → tạo Job (PartRevId, RoutingRevId, PoLineId, RunQty, ShipBy)
      + auto-generate Products theo RunQty (tái dùng CreateJobCommand logic) (++JobsCreated)
   g. SaveChangesAsync() — commit theo TỪNG NHÓM JOB (không phải 1 transaction
      toàn batch); lỗi ở 1 nhóm không ảnh hưởng nhóm khác — nếu exception,
      db.ChangeTracker.Clear() rồi ghi ImportRowError cho các dòng của nhóm đó,
      tiếp tục nhóm kế tiếp
4. Trả về GlobalImportResultDto (tổng hợp theo entity) + errors theo dòng
```

### 5. Quy tắc resolve/tạo từng entity

- **Part**: chưa tồn tại theo `PartNumber` → tạo mới (`Description` = `PartDescription` hoặc `""`)
- **PartRev**: chưa có `RevCode` cho Part đó → tạo PartRev mới, **set `IsActive=true` + deactivate các PartRev khác cùng Part** (đúng business rule "tạo PartRev mới → deactivate cái cũ" trong CLAUDE.md) → tự tạo `Routing "Standard"` + `RoutingRev "R1"` (giống `CreatePartCommand`)
- **RoutingRev**: luôn dùng RoutingRev đang `IsActive=true` của Routing "Standard" thuộc PartRev — **không** tạo RoutingRev mới từ import (đơn giản hoá: nếu cần đổi routing, dùng "+ Routing Rev" trong `/parts` như hiện tại)
- **PartOp**: upsert theo `OpNumber` trong RoutingRev (giống `ImportOpsCommand` — update field nếu tồn tại, tạo mới nếu chưa)
- **PoLine**: chưa có `(PoNumber, PoLineNumber)` → tạo mới (`CustomerId=null`)
- **Job**: `JobNumber` đã tồn tại → **update `RunQty`/`ShipBy` nếu khác giá trị hiện tại** (`PartRevId`/`RoutingRevId`/`PoLineId` giữ nguyên — Job là snapshot, import không đổi); `RunQty` tăng → generate thêm `Product` (serial tiếp theo, tái dùng `GenerateProductsCommand`), `RunQty` giảm → chỉ update field, không xoá `Product` dư (đã có thể có `MeasureValue`); chưa có `JobNumber` → tạo Job mới + generate Products theo RunQty

### 6. Edge cases

- Thiếu `PartNumber`/`JobNumber`/`OpNumber` ở 1 dòng → lỗi dòng đó, **không** chặn các dòng/job khác
- `OpType` không match `OpTypes.Code` → warning, vẫn tạo OP với `OpTypeId=null` (giống `ImportOpsCommand`)
- Nhiều dòng cùng `OpNumber` trong 1 Job (lỗi dữ liệu nguồn) → dedupe, giữ dòng cuối, log warning
- `RunQty` trống/0 → tạo Job nhưng không generate Product (giống `CreateJobCommand` hiện tại)
- `Revision` trống → dùng `"A"`
- **Dimension KHÔNG nằm trong scope import này** — vẫn dùng `ImportDimensionsCommand` riêng theo OP như hiện tại (dimension đến từ bản vẽ, không có trong BAQ Epicor)

### 7. Backend design

```csharp
// ShopfloorManager.Application/Production/JobBatchImportCommands.cs

public record ImportJobBatchRow(
    string PartNumber, string? PartDescription, string? Revision,
    string JobNumber, string? PoNumber, string? PoLineNumber,
    int? RunQty, DateOnly? ShipBy,
    string OpNumber, string? OpTypeCode, string? OpDescription,
    decimal? SetupTime, decimal? ProdTime);

public record GlobalImportResultDto(
    int PartsCreated, int PartRevsCreated,
    int OpsCreated, int OpsUpdated,
    int JobsCreated, int JobsUpdated, int ProductsCreated,
    List<ImportRowError> Errors);

public record ImportJobBatchCommand(List<ImportJobBatchRow> Rows, int? RequesterId)
    : IRequest<Result<GlobalImportResultDto>>;
```

- Handler nhóm `Rows` theo `JobNumber`, xử lý tuần tự theo §4–§5, `SaveChangesAsync` per-group.
- Endpoint mới trong `JobsController` (multipart, giống `OperationsController.ImportOps`):
  ```
  POST /api/v1/jobs/import-batch          -- file Excel, role Administrator/Manager/Engineer/Planner
  GET  /api/v1/jobs/import-batch/template -- ExcelTemplateBuilder.BuildJobBatchTemplate()
  ```
- `ExcelTemplateBuilder.BuildJobBatchTemplate()` — thêm template mới (cùng pattern `BuildOpsTemplate`/`BuildDimensionsTemplate`) với 13 cột ở §3 + 1 dòng ví dụ.

### 8. Frontend design

- `/jobs`: thêm nút **"⬆⬆ Import hàng loạt"** (cạnh "+ Tạo Job") mở dialog mới `BulkJobImportDialog` (`components/jobs/`), theo đúng pattern `ImportOpsDialog` — **không có bước preview** (đơn giản, nhất quán với import OP/Dimension hiện có): chọn file → nút "⬇ Tải file mẫu" → submit → hiển thị bảng kết quả `GlobalImportResultDto` (Parts/PartRevs/Ops created/updated/Jobs created/skipped/Products) + danh sách lỗi theo dòng
- Sau khi đóng dialog → refetch `/jobs` list
- i18n: namespace `jobs.bulkImport` (vi+en) theo pattern `documents.bulkUpload` đã có

### 9. Migration

**Không cần migration** — tái sử dụng toàn bộ entity hiện có (`Part`, `PartRev`, `Routing`, `RoutingRev`, `PartOp`, `PoLine`, `Job`, `Product`).

### 10. Quyết định đã xác nhận

1. **PartRev mới từ import**: `IsActive=true` + deactivate PartRev cũ cùng Part (đúng business rule chuẩn trong CLAUDE.md) — khác với `AddPartRevCommand` hiện tại (`IsActive=false`); import dùng logic mới, không tái dùng `AddPartRevCommand`. ✅ Đã xác nhận.
2. **JobNumber đã tồn tại**: **update `RunQty`/`ShipBy` nếu khác** giá trị hiện tại (xem §4 bước f, §5 "Job"). ✅ Đã xác nhận — đây là điểm khác biệt chính so với hành vi skip của ManageData cũ.
3. **RoutingRev**: import chỉ upsert OP vào RoutingRev `IsActive=true` hiện tại, không tạo RoutingRev mới (R2, R3...). Cần versioning routing → dùng "+ Routing Rev" thủ công ở `/parts`. ✅ Đã xác nhận.
4. **OpType code**: cột `OpType` trong file Epicor/BAQ khớp trực tiếp với `op_types.code` hiện có trong hệ thống — match case-insensitive như `ImportOpsCommand`. ✅ Đã xác nhận.

**Thiết kế đã chốt — sẵn sàng implement** theo "Triển khai tính năng — quy trình bắt buộc" (CLAUDE.md): Domain (không cần entity mới) → Application (`JobBatchImportCommands.cs`) → API (`JobsController` + `ExcelTemplateBuilder.BuildJobBatchTemplate()`) → Frontend (`BulkJobImportDialog` + i18n) → build & verify thủ công → cập nhật CLAUDE.md.
