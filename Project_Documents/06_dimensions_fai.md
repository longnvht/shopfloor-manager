# Dimensions & FAI (First Article Inspection)

## 1. Tổng quan

Module cốt lõi của hệ thống chất lượng — định nghĩa kích thước cần kiểm tra và ghi nhận kết quả đo kiểm thực tế trong 3 giai đoạn độc lập: Inprocess FAI (Operator), QC Inline (QC ngẫu nhiên trên chuyền), QC Final (QC xuất xưởng).

**Người dùng liên quan:**
- **Engineer**: định nghĩa Dimension Sheet (kích thước, dung sai).
- **Operator**: nhập kết quả đo Inprocess FAI tại máy (Desktop MES).
- **Leader**: quyền Operator mở rộng — nhập thay bất kỳ Operator nào.
- **QC Inspector**: nhập QC Inline (trên chuyền) và QC Final (xuất xưởng).

---

## 2. Khái niệm cốt lõi

```
PartOp (Công đoạn)
 └── Dimension (Kích thước cần kiểm)     ← Engineer định nghĩa
      ├── BalloonNumber: "1", "*20", "3"...  (* = kích thước trung gian)
      ├── NominalValue: 25.0000
      ├── MaxValue: 25.0200  (= Nominal + Tolerance+)
      ├── MinValue: 24.9800  (= Nominal - Tolerance-)
      ├── IsFinal: true/false             (bắt buộc đo tại QC Final)
      └── MeasureValue (Kết quả đo)       ← nhập theo Stage
           ├── Value: 25.0050
           ├── Result: Pass / Fail
           ├── MeasureStage: InprocessFAI | QCInline | QCFinal
           └── GageID (dụng cụ đo sử dụng)
```

**3 giai đoạn đo kiểm — hoàn toàn độc lập:**

| Stage | Enum | Người thực hiện | Phạm vi | Thời điểm |
|---|---|---|---|---|
| **Inprocess FAI** | `0` | Operator (người gia công SP đó tại OP đó) | Dimensions của OP đó | Sau khi session OP kết thúc |
| **QC Inline** | `1` | QC Inspector | Bất kỳ balloon nào của bất kỳ SP nào | Ngẫu nhiên trong quá trình sản xuất |
| **QC Final** | `2` | QC Inspector | Tất cả Dimensions (ưu tiên `IsFinal=true`) | Khi SP hoàn thiện gia công, trước khi xuất xưởng |

**Nguyên tắc cô lập:**
- Mỗi stage thấy và khóa riêng: `(DimensionId, ProductId, MeasureStage)` chỉ được nhập **một lần** trong một stage
- Không ghi đè — tạo record mới mỗi lần đo (giữ lịch sử toàn bộ)
- Mỗi nhóm chỉ thấy data của stage mình khi tra cứu

---

## 3. Business Rules

### 3.1 Balloon Number
- Số hiệu kích thước trên bản vẽ (hình tròn bao quanh số → "balloon").
- Format tự do: `"1"`, `"2A"`, `"3B"`, `"10"`, `"10A"`, `"*20"`, `"*30"`...
- **UNIQUE trong phạm vi một PartOp** — không được trùng trong cùng một OP.
- `balloon_sort`: parse từ balloon_number để sort đúng thứ tự số (1, 2, 3... thay vì 1, 10, 2, 3...).

### 3.2 Kích thước số vs Kích thước text
- **Số** (phần lớn): lưu `nominal_value`, `max_value`, `min_value` dạng `DECIMAL(14,4)`.
  - Pass khi: `min_value ≤ measured_value ≤ max_value`.
- **Text** (`is_text_type = true`): kích thước không phải số — ren, ký hiệu hình học, note đặc biệt.
  - Ví dụ: `"M10x1.5-6H"`, `"Ra 0.8"`
  - Lưu vào `nominal_text`, không có min/max.
  - Kết quả đo: operator/QC chọn Pass/Fail thủ công.

### 3.3 Tolerance
- `tolerance_plus`: dung sai dương (trên) → `max_value = nominal + tolerance_plus`.
- `tolerance_minus`: dung sai âm (dưới) → `min_value = nominal - tolerance_minus`.
- Cả hai lưu dạng dương: dung sai ±0.02 → `tolerance_plus = 0.02`, `tolerance_minus = 0.02`.
- Dung sai lệch tâm: `+0.05 / -0.02` → `tolerance_plus = 0.05`, `tolerance_minus = 0.02`.

### 3.4 IsFinal — Kích thước bắt buộc QC Final

`Dimension.IsFinal = true` đánh dấu kích thước **bắt buộc phải kiểm tra** tại QC Final trước khi xuất xưởng.

- Mục đích: thay vì phải đo 100% kích thước, Engineer chọn sẵn các kích thước quan trọng → QC Final chỉ cần đo các kích thước này
- **Tiến độ QC Final** = số `IsFinal` dimensions đã có `MeasureStage=QCFinal` / tổng `IsFinal` dimensions của sản phẩm
- Các kích thước không có `IsFinal=true` tại QC Final: được phép bỏ qua
- QC Final vẫn **có thể đo** các kích thước không `IsFinal` nếu muốn — không bị cấm

> Legacy: 90,061/94,100 dimensions (96%) có `is_final=1` → hầu hết đều bắt buộc kiểm tra QC Final.

### 3.5 Kích thước trung gian (Intermediate Dimensions)

Kích thước **không có trên bản vẽ** — do Engineer tạo thêm khi xây dựng quy trình gia công để kiểm soát chất lượng trung gian (process control).

- Ví dụ: Để gia công OD đạt đường kính X, chia 2 bước: B1 gia công đến X' (trung gian), B2 gia công đến X (cuối). Kích thước X' là kích thước trung gian.
- **Ký hiệu**: prefix `*` trước balloon number — `*20`, `*30`, `*15A`...
- Kỹ sư đánh dấu `*` thủ công trước khi import Excel
- **Hiển thị**: hiện mặc định trong Dimsheet, có visual style khác (màu/icon) để phân biệt
- **Không dùng** để kiểm soát chất lượng sản phẩm cuối — chỉ dùng trong process control
- Người dùng có thể toggle ẩn/hiện kích thước trung gian trên Dimsheet

### 3.6 Dimension → GageType (KHÔNG còn CategoryId riêng — đổi 2026-06-24)

`Dimension.GageTypeId` (nullable, FK → `GageType`) là **nguồn duy nhất** xác định dụng cụ đo cho 1 dimension — chi tiết hơn hẳn "category" cũ (ví dụ "MIC" panme, không chỉ "LIN"). `Dimension` **không còn cột `CategoryId`** — nhóm rộng (LIN/ANG/THD/GEO/SFC) lấy **gián tiếp** qua `GageType.CategoryId → GageCategory`, tránh lưu trùng 2 lần cùng một thông tin phân loại.

```
Dimension.GageTypeId → GageType.CategoryId → GageCategory (LIN/ANG/THD/GEO/SFC)
```

`DimensionDto.CategoryCode` (API response) vẫn tồn tại nhưng giờ là **giá trị suy ra** (qua `GageType.Category.Code`), không phải cột riêng — khi `GageTypeId = null`, `CategoryCode` cũng `null`.

**`GageType` code "VIS"** (Visual Inspection, `CategoryId = NULL`) là GageType đặc biệt đại diện "kiểm bằng mắt — không cần dụng cụ đo". Desktop FAI (`FaiViewModel.ShowGageSelection`) ẩn hẳn bước chọn gage khi `GageTypeCode == "VIS"`. Chỉ dimension thực sự kiểm bằng mắt (không có min/max số, text mô tả tiêu chí định tính — "No burr", "Visual Inspection") mới gán GageType này; dimension đo góc/ren/độ nhám thiếu tolerance số nhưng vẫn cần dụng cụ thật (thước góc, dưỡng ren, máy đo nhám) **không** gán VIS.

| GageType code (ví dụ) | Category suy ra | 
|---|---|
| CAL, MIC, BOR, DPG, HEG | LIN |
| ANG | ANG |
| PLG, RIG, PDG, ETG, THG | THD |
| CMM, IND, PPM, RAD | GEO |
| SRM, SRT | SFC |
| VIS | *(không có category)* |
| HRT, PRESSURE GAGE, TACH | *(không có category — không đo kích thước)* |

> Lịch sử: trước 2026-06-24, `Dimension` có cả `CategoryId` (nhóm rộng) **và** `GageTypeId` (cụ thể) — gây trùng lặp dữ liệu (2 chỗ lưu cùng ý nghĩa phân loại). Đã bỏ hẳn `CategoryId` khỏi `Dimension`, migration `RemoveDimensionCategoryId`. Excel import cũng đổi cột `Category` → `GageType` (vẫn nhận alias `category` để không phá file cũ). Cùng ngày: đổi tên bảng `dimension_categories` → `gage_categories` (entity `DimensionCategory` → `GageCategory`, endpoint `/api/v1/dimension-categories` → `/api/v1/gage-categories`) — tên cũ gây hiểu nhầm vì giờ chỉ còn `GageType` tham chiếu tới nó, không phải `Dimension`.

### 3.7 Lịch sử thay đổi Dimension
- Mỗi khi thay đổi nominal/tolerance → tạo record trong `dimension_history`.
- Dữ liệu `measure_values` cũ vẫn giữ nguyên và valid (so với spec tại thời điểm đo).

### 3.8 Dimension Status (Approval Workflow)

Tương tự TechDocuments — dimensions phải được duyệt trước khi dùng trong FAI.

```
Pending → Approved   (Lead Engineer / Manager / Administrator duyệt)
        → Rejected   (kèm lý do) → Engineer chỉnh sửa → import lại → Pending
```

- Import bulk → mọi dimension tạo ra đều là `Pending`
- Import per-OP (hiện tại) → cũng là `Pending` (consistent)
- Chỉ dimension `Approved` xuất hiện trong Desktop FAI measurement
- **Batch approve**: Lead Engineer/Manager/Admin chọn tất cả pending của 1 import → Approve All
- **Reject từng dòng**: với lý do → Engineer xem lý do, sửa Excel, import lại phần bị reject
- **Existing dimensions** (trước khi có tính năng này): migration default = `Approved`
- **Quyền**: `Lead Engineer`, `Manager`, `Administrator` — không phải `QC Inspector`

### 3.9 Import từ Excel

**Import per-OP** (hiện tại — `/parts/[id]/operations`):
- Engineer import Dimension cho từng OP riêng lẻ
- Cột Excel: `BalloonNumber`, `Code`, `Description`, `Nominal`, `TolPlus`, `TolMinus`, `Unit`, `GageType` (giá trị là `GageType.Code` như "MIC"/"PLG" — đổi từ `Category` 2026-06-24, vẫn nhận alias cột `Category` cho file cũ)
- Validation: BalloonNumber không trùng trong OP, Nominal phải là số (trừ `is_text_type`)

**Import bulk — toàn bộ Part** (kế hoạch — `/dimsheet`):
- Sau khi Engineer hoàn tất xây dựng quy trình, import 1 file Excel duy nhất chứa **toàn bộ dimensions của mọi OP** trong 1 Part
- Cột Excel thêm `OpNumber` để phân loại dimension vào đúng PartOp
- Cột `IsFinal` (0/1) để đánh dấu bắt buộc QC Final
- BalloonNumber với prefix `*` → hệ thống nhận diện là kích thước trung gian (không cần cột riêng)
- Kết quả trả `ImportResultDto` gộp cho toàn bộ các OP: created/skipped/errors

### 3.9 QC Inline Rate (Tỷ lệ kiểm tra ngẫu nhiên)

- **Factory-wide default**: tỷ lệ % sản phẩm cần kiểm tra QC Inline cho toàn nhà máy
- **Override theo Job → OP**: công đoạn đặc thù có tỷ lệ riêng cao hơn/thấp hơn default
- Tỷ lệ cụ thể hơn (Job→OP level) thì override factory-wide
- Lưu trong `qc_inline_rates` hoặc là field trên `PartOp` (thiết kế chi tiết Phase 4b)

### 3.10 Quy tắc "Sản phẩm hoàn thiện gia công" (điều kiện QC Final)

**Định nghĩa**: Sản phẩm được coi là "hoàn thiện gia công" khi **tất cả PartOp trong routing có ít nhất 1 Dimension đều đã có session completed**.

- Logic: OP nào có Dimension = OP gia công thực sự cần kiểm soát chất lượng. OP admin/hành chính (IST, ISS, Feature Prepare, Issue Documents...) thường không có Dimension → tự động bị loại khỏi điều kiện.
- Query: `PartOps WHERE routingRevId = job.RoutingRevId AND EXISTS(Dimensions) AND NOT EXISTS(Sessions WHERE status = Completed)` → nếu rỗng → product hoàn thiện.
- **ForJobOnly OPs** cũng áp dụng cùng rule nếu có Dimension.

### 3.11 OP INS — OP kiểm tra (không sở hữu Dimension riêng)

`OpType.Code = "INS"` (so khớp cố định, không phân biệt hoa thường) đánh dấu một **OP kiểm tra** — dùng để QC đo lại các Dimension đã định nghĩa ở các OP gia công trước đó, KHÔNG sở hữu Dimension riêng của chính nó.

- **Một routing có thể có nhiều OP INS** — điển hình quanh 1 bước Coating/Finishing. Ví dụ thật:
  ```
  ...OP90 → OP100 STP → OP110 INS → OP120 PPG (Phosphating) → OP130 INS
  ```
  Trước khi mạ, tất cả kích thước gia công cần được kiểm tra (OP110 INS); sau khi mạ, kiểm tra lại toàn bộ vì mạ có thể ảnh hưởng kích thước (OP130 INS).
- **Quy tắc gom Dimension**: khi xem FAI sheet của 1 OP INS, hệ thống gom Dimension của **các PartOp có `OpNumberSort` nhỏ hơn OP INS đó** (không phải toàn bộ Job, không phải chỉ OP liền trước). Vì các OP trung gian không sở hữu Dimension (OP INS khác, OP Coating...) không góp phần vào tập hợp, nhiều OP INS liên tiếp quanh 1 bước Coating sẽ tự nhiên cho ra **cùng một tập Dimension** — đúng ý đồ nghiệp vụ (đo lại y nguyên bộ kích thước, trước và sau mạ).
  - `OpNumberSort` null → fallback `9999` khi so sánh.
  - Áp dụng cho cả `GetFaiSheetQueryHandler` (ma trận đo) và `GetJobOpsQueryHandler` (đếm số dimension hiển thị trên dropdown chọn OP).
- **"Tất cả OP"** (lựa chọn riêng trên web FAI, không phải OP INS): gom Dimension của **toàn bộ PartOp trong routing của Job** (không lọc theo `OpNumberSort`). Dùng khi cần xem/duyệt toàn bộ kích thước của Job bất kể OP nào sở hữu.
- Cả 2 trường hợp trên đều gắn thêm `OpNumber` (OP gốc sở hữu Dimension) vào `DimensionDto` để phân biệt khi nhiều OP gộp lại trên cùng 1 ma trận.

---

## 4. Workflow FAI — 3 giai đoạn

### 4.1 Inprocess FAI (Operator)

**Ai**: Operator gia công — chỉ được nhập cho sản phẩm **do chính mình** gia công tại OP đó.
**Leader**: được nhập thay bất kỳ Operator nào.

```
Operator login (Desktop) → chọn Job → chọn OP → chọn Product Serial
  → Điều kiện check: ProductionSession của (product, OP) này
      → session.UserId == currentUser.Id (hoặc role = Leader)
      → session.CompletedAt IS NOT NULL  (gia công đã kết thúc)
  → FAI Basic mở:
      Hiện danh sách Dimensions của OP (sorted theo balloon_sort)
      Màu: Xám=chưa đo | Xanh=Pass | Đỏ=Fail (lọc MeasureStage=InprocessFAI)
      Counter: "5 / 20"
  → Chọn Balloon:
      → Kích thước số: nhập giá trị → hệ thống tính Pass/Fail tự động
      → Kích thước text: chọn PASS / FAIL thủ công
      → Pass → Balloon xanh, tự động chuyển tiếp
      → Fail → Balloon đỏ → tạo NCR (→ xem 07_ncr.md)
  → Đã đo tất cả → session cho phép "Kết thúc"
```

**Khi Fail trong Inprocess FAI:**
- Operator tạo NCR ngay → không được đo lại trong InprocessFAI
- Sau rework, kết quả quyết định cuối cùng đến từ **QC Final** (MeasureStage=QCFinal)

**IsInputLocked**: Balloon đã đo (có MeasureValue với stage=InprocessFAI) → lock toàn bộ input, hiển thị giá trị cũ, không cho nhập lại.

### 4.2 QC Inline (QC Inspector — ngẫu nhiên trên chuyền)

**Ai**: QC Inspector.
**Điều kiện**: Product đã có ít nhất 1 session completed tại OP được chọn (Operator đã gia công xong OP đó).

```
QC Inspector login (Desktop) → FAI Basic
  → Chọn Job → chọn OP → chọn Product Serial
  → Điều kiện check:
      EXISTS(Session WHERE productId = X AND partOpId = Y AND completedAt IS NOT NULL)
  → Hiện danh sách Dimensions (lọc MeasureStage=QCInline)
  → Nhập kết quả → lưu với MeasureStage=QCInline
  → (DimensionId, ProductId, QCInline) đã tồn tại → lock (không đo lại)
```

**Tỷ lệ QC Inline**: số lượng sản phẩm/balloons cần kiểm tra theo `QcInlineRate` (factory-wide default hoặc override Job→OP).

### 4.3 QC Final (QC Inspector — xuất xưởng)

**Ai**: QC Inspector. **Operator và Leader không truy cập được**.
**Điều kiện**: Product "hoàn thiện gia công" (xem §3.10).
**Phạm vi**: Toàn bộ Dimensions của sản phẩm (tất cả OP) — ưu tiên `IsFinal=true`.

```
QC Inspector login (Desktop) → Tiện ích "FAI Final" (ẩn với Operator/Leader)
  → Chọn Job → chọn Product Serial
  → Điều kiện check: tất cả OP có Dimension đều có session completed
  → Hiện TOÀN BỘ Dimensions (không giới hạn theo OP)
      Ưu tiên IsFinal=true (badge đặc biệt)
      Màu: Xám=chưa đo | Xanh=Pass | Đỏ=Fail (lọc MeasureStage=QCFinal)
  → Nhập kết quả → lưu với MeasureStage=QCFinal
  → Tiến độ = đã đo IsFinal dims / tổng IsFinal dims
  → Fail → tạo NCR
  → Hoàn tất đủ IsFinal dims → Product cho phép đóng gói / xuất xưởng
```

### 4.4 Gage Selection (Chọn dụng cụ đo) — kế hoạch Phase 5

Mỗi kết quả đo nên ghi nhận dụng cụ nào đã dùng — phục vụ truy vết sau này khi gage bị thu hồi hoặc calibration lỗi.

```
Tap Balloon Number → Input Panel mở:
  ① Chọn Gage (tùy chọn):
       → GET /api/v1/gages?categoryId={dim.categoryId}&status=available
       → Hiện danh sách: GageNo, GageName, GageType
       → Filter: is_calibrated=true + status=available
  ② Nhập giá trị
  ③ Confirm → POST với gageId (null nếu không chọn)
```

> **Trạng thái implement:** Gage selection chưa có trên Desktop (⏳ Phase 5). Hiện tại `measure_values.gage_id` để null.

---

## 5. Data Model

```sql
gage_categories (id, code [UNIQUE], name, description)  -- đổi tên từ dimension_categories, 2026-06-24

dimensions (
    id              [BIGSERIAL],
    part_op_id      → part_ops,
    balloon_number  [VARCHAR(20)],  -- prefix * = kích thước trung gian
    balloon_sort    [INTEGER],      -- parse từ balloon_number, bỏ prefix *
    nominal_value   [DECIMAL(14,4)],
    max_value       [DECIMAL(14,4)],
    min_value       [DECIMAL(14,4)],
    tolerance_plus  [DECIMAL(14,4)],
    tolerance_minus [DECIMAL(14,4)],
    nominal_text    [VARCHAR(100)],
    is_text_type    [BOOLEAN],
    is_final        [BOOLEAN],      -- bắt buộc đo tại QC Final
    category_id     → gage_categories,
    status          [SMALLINT DEFAULT 0],  -- Pending=0, Approved=1, Rejected=2
    reviewed_by     → users,        -- người duyệt (Lead Engineer/Manager/Admin)
    reviewed_at     [TIMESTAMPTZ],
    review_note     [TEXT],         -- lý do reject
    created_by, created_at, updated_by, updated_at, deleted_at,
    UNIQUE(part_op_id, balloon_number)
)

dimension_history (
    id, dimension_id → dimensions,
    nominal_value, max_value, min_value,
    tolerance_plus, tolerance_minus,
    nominal_text, category_id, is_final,
    changed_by → users, changed_at, change_reason
)

-- MeasureStage enum: InprocessFAI=0, QCInline=1, QCFinal=2
measure_values (
    id              [BIGSERIAL],
    dimension_id    → dimensions,
    product_id      → products,
    part_op_id      → part_ops,
    measure_stage   [SMALLINT NOT NULL DEFAULT 0],  -- *** MỚI ***
    value           [DECIMAL(14,4)],    -- NULL nếu is_text_type
    result          [SMALLINT],         -- Pass=1, Fail=2
    gage_id         → gages,
    machine_id      → machines,
    measured_by     → users,
    measured_at,
    note,
    -- is_final, final_op_id: legacy columns, deprecated (thay bằng measure_stage=QCFinal)
    has_ncr         [BOOLEAN],
    ncr_code        [VARCHAR(50)],
    created_at,
    UNIQUE(dimension_id, product_id, measure_stage)  -- mỗi stage chỉ 1 lần/balloon/serial
)

measure_value_logs (
    id, measure_id → measure_values,
    old_value, new_value,
    old_result, new_result,
    gage_id, note,
    changed_by, changed_at
)

-- QC Inline Rate (kế hoạch Phase 4b)
qc_inline_rates (
    id,
    job_id      → jobs,        -- NULL = factory-wide default
    part_op_id  → part_ops,    -- NULL = toàn bộ job
    rate_percent [DECIMAL(5,2)],
    created_by, created_at
    -- job_id IS NULL AND part_op_id IS NULL → factory-wide default (chỉ 1 record)
    -- job_id SET, part_op_id NULL → toàn bộ Job override
    -- job_id SET, part_op_id SET → Job→OP override (cụ thể nhất)
)
```

**Constraint UNIQUE trên `measure_values`:**
- `(dimension_id, product_id, measure_stage)` → đảm bảo mỗi giai đoạn chỉ 1 lần đo / balloon / serial
- Áp dụng ở application layer (handler check trước khi INSERT) — không phải DB constraint (vì giữ lịch sử bằng cách tạo record mới, nhưng check existence trước)

---

## 6. API Endpoints

```
-- Dimensions --
GET    /api/v1/operations/{opId}/dimensions/definitions
POST   /api/v1/operations/{opId}/dimensions
PUT    /api/v1/dimensions/{id}
DELETE /api/v1/dimensions/{id}
POST   /api/v1/operations/{opId}/dimensions/import
GET    /api/v1/dimensions/{id}/history
GET    /api/v1/routing-revs/{routingRevId}/dimensions   -- dùng bởi /dimsheet

-- Bulk import toàn bộ Part (kế hoạch)
POST   /api/v1/routing-revs/{routingRevId}/dimensions/import-bulk
       Body: multipart/form-data (file Excel)
       Response: ImportResultDto { created, skipped, errors: [{ rowNumber, opNumber, message }] }
GET    /api/v1/routing-revs/{routingRevId}/dimensions/import-bulk/template
       Response: Excel template (.xlsx)

-- Dimension Approval (kế hoạch — role Lead Engineer/Manager/Admin)
PUT    /api/v1/dimensions/{id}/review
       Body: { approve: true/false, note?: string }
POST   /api/v1/dimensions/review-batch
       Body: { dimensionIds: number[], approve: true/false, note?: string }

-- Measure Values --
GET    /api/v1/fai?jobId=&partOpId=                     -- FaiSheetDto (hiện tại)
POST   /api/v1/fai/measure                              -- lưu MeasureValue (hiện tại)
       Body: { dimensionId, productId, partOpId, value, manualResult, isFinal, measureStage }
GET    /api/v1/measure-values?dimensionId=&productId=&stage=
GET    /api/v1/dimensions/{id}/measure-history?productId=

-- QC Final progress
GET    /api/v1/products/{id}/qcfinal-progress
       Response: { totalFinal, measuredFinal, passedFinal, failedFinal, isFullyMachined }

-- QC Inline Rate (kế hoạch Phase 4b)
GET    /api/v1/qc-inline-rates?jobId=&partOpId=
POST   /api/v1/qc-inline-rates
PUT    /api/v1/qc-inline-rates/{id}
```

---

## 7. FAI Report (PDF)

Nội dung báo cáo:
- **Header**: Job Number, Part Number, Revision, Date, Inspector
- **Bảng kích thước**: BalloonNumber | Nominal | Tol(+/-) | Serial01 | Serial02 | ...
  - Ô Pass: giá trị đo (màu đen)
  - Ô Fail: giá trị đo (màu đỏ, bold)
  - Kích thước trung gian (`*`): hiện trong bảng với ký hiệu riêng
- **Stage filter**: báo cáo theo stage (InprocessFAI / QCInline / QCFinal) hoặc gộp
- **Footer**: tổng Pass/Fail, chữ ký Inspector

---

## 8. Edge Cases

- **Operator Fail → NCR, không đo lại**: Inprocess FAI lock sau lần đo đầu. Fail → tạo NCR. QC Final là lần đo quyết định cuối cùng.
- **QC Inline đo lại cùng balloon/serial**: KHÔNG được phép — `(DimensionId, ProductId, QCInline)` unique.
- **QC Final trước khi hoàn thiện gia công**: chặn tại application layer — check `isFullyMachined` theo §3.10.
- **Xóa Dimension đã có MeasureValue**: cấm tuyệt đối — ảnh hưởng audit trail chất lượng.
- **Tolerance = 0**: max = min = nominal — hiếm nhưng hợp lệ.
- **Value ngoài range rất xa**: cảnh báo "Có thể nhập sai" nếu ngoài ±10× tolerance, nhưng vẫn cho lưu.
- **Kích thước trung gian (`*`) trong QC Final**: được phép đo (không bị cấm), không tính vào progress IsFinal.
- **Precision**: Legacy lưu `FLOAT` → mất chính xác. Hệ thống mới dùng `DECIMAL(14,4)` — cải thiện khi migrate.
- **NominalDimension dạng text trong legacy**: Parse tách `is_text_type=true` + `nominal_text`.
- **Gage thu hồi/hỏng**: `gage_id` trong `measure_values` dùng để truy vết toàn bộ kết quả đo cần re-inspect.
- **OP không có Dimension**: không tính vào điều kiện "hoàn thiện gia công" (§3.10) → IST, ISS, Feature Prepare, Issue Documents... tự động được bỏ qua.

---

## UI Redesign — Phase C ✅ (đã triển khai 2026-06-18)

**`/fai`** (job-list panel trái) + **`/jobs/[id]/fai`** (không panel) — chia sẻ component `FaiMatrix`.

- **Filter bar** (card riêng, tách khỏi topbar): Operation (`FaiOpSelect` — gồm cả lựa chọn "— Tất cả OP —", mặc định khi chọn Job) + Measure Stage (`VASeg` — chỉ 3 lựa chọn InprocessFAI/QCInline/QCFinal, luôn chọn đúng 1 stage, không có "Tất cả").
- **Info bar + Stats strip**: dải KPI (Tổng ô/Đã đo/Pass/Fail·NCR/Pending/Pass rate %), người đo gần nhất — 100% client-side aggregate từ `FaiSheetDto`, không cần API riêng.
- **Matrix**: balloon dạng vòng tròn màu theo `categoryCode`, tooltip nổi theo con trỏ, nhãn nhỏ "OP{n}" khi xem qua OP INS hoặc "Tất cả OP".
- **Backend**: `GetFaiSheetQuery.PartOpId` nullable — null = "Tất cả OP" (xem §3.11).

---

## UI Redesign — Phase J (đề xuất, chưa triển khai)

**Panel "Chi tiết Balloon"** — click vào 1 ô trong `FaiMatrix`

- Mở panel/dialog: Nominal/Tolerance/Category, lịch sử đo theo từng stage (InprocessFAI/QCInline/QCFinal), biểu đồ phân bố (Apache ECharts).
- **API cần thêm**: `GET /api/v1/dimensions/{id}/measure-history?productId=&stage=`
- **Nút "Mở NCR cho ô này"**: pre-fill NCR form khi ô đang Fail.

---

## Kế hoạch triển khai — 3 nhóm

### Nhóm 1 — Roles + Approval permissions (Prerequisite)

| Hạng mục | Loại | Chi tiết |
|---|---|---|
| Add `Lead Engineer` role | Backend | `AppConstants.Roles` + DB seed (Id=8); migration insert vào `roles` |
| TechDocument approval role | Backend | Cập nhật `InspectTechDocumentCommandHandler`: check role `Lead Engineer\|Manager\|Admin` thay `QC Inspector` |
| Migration `AddDimensionStatus` | Backend | Thêm `status SMALLINT DEFAULT 1` (Approved) + `reviewed_by`, `reviewed_at`, `review_note` vào `dimensions`; existing rows → Approved=1 |
| `MeasureStage` enum | Domain | `InprocessFAI=0, QCInline=1, QCFinal=2` |
| Migration `AddMeasureStage` | Backend | Thêm `measure_stage SMALLINT NOT NULL DEFAULT 0` vào `measure_values`; data cũ → InprocessFAI=0 |

### Nhóm 2 — Dimsheet bulk import + layout thống nhất (Web — ưu tiên cao)

| Hạng mục | Loại | Chi tiết |
|---|---|---|
| `ImportBulkDimensionsCommand` | Backend | Nhận `routingRevId` + file Excel; phân loại Dimension vào đúng OP theo `OpNumber`; tạo với `status=Pending` |
| Excel template | Backend | `ExcelTemplateBuilder.BuildDimsheetTemplate()` — cột: BalloonNumber, Code, Desc, Nominal, Tol+, Tol-, Unit, Category, **OpNumber**, **IsFinal** |
| `GET .../template` endpoint | Backend | Download template |
| `POST .../import-bulk` endpoint | Backend | Role Engineer+ |
| `ReviewDimensionCommand` | Backend | `PUT /api/v1/dimensions/{id}/review` + `POST /api/v1/dimensions/review-batch` — role Lead Engineer/Manager/Admin |
| `/dimsheet` — Part list layout | Web | Giữ Part list hiện tại; bổ sung Drawing Rev + Routing Rev vào filter bar (cascade); `*` balloon có visual khác; toggle ẩn kích thước trung gian |
| `/dimsheet` — Approval UI | Web | Badge status Pending/Approved/Rejected; nút "Approve All" (batch) + Reject (từng dòng, nhập lý do); chỉ hiện với Lead Engineer/Manager/Admin |
| `/dimsheet` — Import dialog | Web | `ImportDimsheetDialog`: upload file + download template + hiển thị result per-OP |
| `/documents` — Part list layout | Web | Bỏ combobox Part; thêm Part list panel 220px bên trái; bắt buộc chọn Part; Drawing Rev + Routing Rev vào filter bar; nút Approve/Reject chỉ với 3 role |

### Nhóm 3 — MeasureStage API + QC Final progress (Backend)

| Hạng mục | Loại | Chi tiết |
|---|---|---|
| Update `SaveMeasureCommand` | Backend | Nhận `measureStage`; validate `(dimensionId, productId, stage)` unique trước INSERT |
| `GetQcFinalProgressQuery` | Backend | `GET /api/v1/products/{id}/qcfinal-progress` → `{totalFinal, measuredFinal, isFullyMachined}` |
| `isFullyMachined` helper | Backend | Query: all PartOps có Dimension trong routing của Job → tất cả có session completed |

### Nhóm 4 — Desktop role-aware FAI (Phase 4b — riêng)

| Hạng mục | Loại | Chi tiết |
|---|---|---|
| FAI Basic role-split | Desktop | Operator → InprocessFAI (chỉ sản phẩm mình gia công); QC Inspector → QCInline |
| Leader override | Desktop | Leader đo thay bất kỳ Operator |
| FAI Final utility (QC Final) | Desktop | Ẩn với Operator/Leader; check `isFullyMachined`; hiện toàn bộ dims; ưu tiên IsFinal |
| Gage selection — mandatory | Desktop | Flow: Chọn balloon → **Chọn gage** (filter theo CategoryId) → Nhập giá trị; block nếu chưa chọn gage |
| QC Inline Rate config | Web+API | `qc_inline_rates`: factory-wide default + override Job→OP; quản lý từ `/master` |
