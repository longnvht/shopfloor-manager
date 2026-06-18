# FAI View Redesign — Design Spec

**Ngày:** 2026-06-18
**Phạm vi:** `/fai`, `/jobs/[id]/fai`, component `FaiMatrix`, backend `GetFaiSheetQuery` / `GetJobOpsQuery` / `GetJobsQuery`.
**Tham khảo UI:** `D:\DeveloperData\Shopfloor Manage\screenshots\fai2.png`, `fai2b.png`, `fai2c.png`, source mẫu `D:\DeveloperData\Shopfloor Manage\src\va-fai2.jsx`.

---

## 1. Bối cảnh

View FAI hiện tại (`/fai`, `/jobs/[id]/fai`, `FaiMatrix`) dùng 2 dropdown chọn Job/OP đơn giản, info bar và stats strip dạng hàng phẳng, bảng matrix balloon dạng text. Cần redesign theo mẫu UI mới (ảnh `fai2c.png` + job-list panel từ `fai2.png`/`fai2b.png`), đồng thời sửa một lỗ hổng nghiệp vụ: **OP kiểm tra (OP INS) hiện không được xử lý đặc biệt** — matrix hiện tại chỉ load dimension của đúng 1 PartOp đang chọn, nhưng OP INS không sở hữu dimension riêng, nó dùng để đo lại dimension của các OP gia công trước đó.

## 2. Business rule mới — OP INS

**Phát hiện từ dữ liệu legacy:** `OpType.Code = "INS"` (INSPECTION) là một loại OP có thể xuất hiện **nhiều lần** trong 1 routing — điển hình quanh bước Coating/Finishing (mạ, xi). Ví dụ routing thật:

```
...OP90 WED → OP100 STP → OP110 INS → OP120 PPG (Phosphating) → OP130 INS
```

Lý do: trước khi đưa sản phẩm vào bể mạ Phosphat, **tất cả kích thước gia công cắt gọt phải được kiểm tra**; sau khi mạ xong, **kiểm tra lại toàn bộ** (mạ có thể ảnh hưởng kích thước) — nên cần 2 OP INS bao quanh bước PPG.

**Rule chốt:**
- Dimension vẫn thuộc đúng 1 `PartOp` (`Dimension.PartOpId`) — schema không đổi.
- Khi PartOp được chọn có `OpType.Code` khớp `"INS"` (so khớp cố định, không phân biệt hoa thường), FAI matrix phải hiển thị dimension của **mọi PartOp khác trong routing của Job có `OpNumberSort` nhỏ hơn OP INS đang xem** — không phải toàn bộ job, không phải chỉ OP liền trước.
- Vì các OP trung gian không sở hữu dimension (OP INS, OP Coating...) không góp phần vào tập hợp, nhiều OP INS liên tiếp quanh 1 bước Coating sẽ tự nhiên cho ra **cùng một tập dimension** (đúng ý đồ: đo lại y nguyên bộ kích thước, trước và sau mạ).
- PartOp không phải INS → giữ hành vi cũ (chỉ dimension của chính nó).

## 3. Thay đổi Backend

### 3.1 `GetFaiSheetQueryHandler` (`FaiCommands.cs`)
- `Include(o => o.OpType)` khi load `PartOp`.
- Load toàn bộ PartOps thuộc routing của Job (template `RoutingRevId` + `ForJobOnly` của Job) — tái dùng cách load đã có trong `GetProductMeasureSheetQueryHandler`.
- Nếu `op.OpType?.Code` equals "INS" (OrdinalIgnoreCase):
  `dims = Dimensions WHERE PartOpId IN (PartOps có OpNumberSort < op.OpNumberSort)`
- Ngược lại: `dims = Dimensions WHERE PartOpId == op.Id` (giữ nguyên).
- `DimensionDto` cần thêm field **optional** `OpNumber` (string?, OP gốc sở hữu dimension đó) — null khi xem OP thường (vì đã biết là chính OP đó), có giá trị khi xem OP INS (để FE hiện nhãn nhỏ "OP{n}" cạnh balloon, tránh nhầm OP nào với OP nào khi nhiều OP gộp lại).
- So sánh `OpNumberSort` cần fallback khi null — dùng cùng pattern đã có ở nơi khác (`OpNumberSort ?? 9999`, parse từ `OpNumber` nếu cần) để đảm bảo thứ tự đúng.

### 3.2 `GetJobOpsQueryHandler` (`PartOpCommands.cs`)
- Hiện `DimCount` đang hardcode `0` — sửa thành tính thật:
  - OP thường: `count(Dimensions WHERE PartOpId == op.Id)`.
  - OP INS: `count(Dimensions WHERE PartOpId IN priorOps)` — cùng logic 3.1, để dropdown chọn OP (`FaiOpSelect`) hiển thị đúng "có sheet" / "chưa đo".
- `DocCount` giữ nguyên hành vi cũ (không nằm trong phạm vi spec này), hoặc tính thật nếu không tốn thêm cost đáng kể — không bắt buộc.

### 3.3 `GetJobsQuery` (job list cho `/fai`)
- Thêm field tính toán `OpenNcrCount` vào `JobDto`: `count(Ncr WHERE JobId == job.Id AND Status == Open)`.
- Không đổi schema — chỉ thêm vào DTO/query, join/subquery trong handler.

### 3.4 Không đổi
- `GetProductMeasureSheetQuery` (Serial Measure Sheet) — không nằm trong phạm vi, giữ nguyên.
- Schema/migration — không có migration mới. Mọi thay đổi là DTO + query logic.

## 4. Thay đổi Frontend

### 4.1 `JobDto` (api-client.ts)
Thêm `openNcrCount: number`.

### 4.2 `DimensionDto` (api-client.ts)
Thêm `opNumber: string | null` (OP gốc sở hữu dimension — chỉ có giá trị khi xem qua OP INS).

### 4.3 `PartOpDto` (api-client.ts)
Không đổi field, chỉ backend trả `dimCount` chính xác hơn (bug fix tự nhiên theo §3.2).

### 4.4 Trang `/fai` — layout 2 cột
- **Trái — `FaiJobList`** (component mới, `clients/web/components/fai/fai-job-list.tsx`, rộng 268px):
  - Search box lọc theo `jobNumber`/`partNumber` (client-side filter trên danh sách đã load, debounce qua `api.jobs.list`).
  - Mỗi job-card: `jobNumber` (mono, bold), status badge, `partNumber · Rev {revCode}`, progress bar (`completedCount/runQty`), badge NCR count đỏ (chỉ hiện khi `openNcrCount > 0`).
  - Status badge derive client-side (không qua API mới):
    ```
    isComplete                         → "Xong" (neutral/ok)
    !isComplete && shipBy < today      → "Trễ" (err)
    !isComplete && shipBy - today ≤ 3 ngày → "Rủi ro" (warn)
    else                                → "Đúng hạn" (ok)
    ```
    (không có `shipBy` → luôn "Đúng hạn" trừ khi `isComplete`).
  - Click job-card → set `jobId`, load lại operations + reset `opId`/`stage` (giữ behavior hiện tại của `useEffect` khi `jobId` đổi).
  - Không hiển thị "khách hàng" (không có data nguồn tin cậy — `PoLine.CustomerId` không có bảng `Customer` để resolve tên).
- **Phải**: giữ `VATopbar` + nội dung `FaiMatrix`, bỏ 2 dropdown Job/OP cũ ở trang `/fai/page.tsx` (panel trái đã thay thế việc chọn Job; OP chọn qua `FaiOpSelect` mới nằm trong filter bar của `FaiMatrix`/trang).

### 4.5 Trang `/jobs/[id]/fai`
- Không có `FaiJobList`. Giữ breadcrumb + nút "← Job" như hiện tại.
- Cần thêm UI chọn OP tại trang này (hiện tại trang này nhận `opId` cố định qua query string, không có dropdown đổi OP) — **giữ nguyên hành vi điều hướng hiện tại** (đến từ Job Detail với `opId` đã chọn sẵn), KHÔNG thêm `FaiOpSelect` ở đây. Trang chỉ hưởng phần redesign của `FaiMatrix` (info bar/stats/matrix mới) khi đã có `sheet`.

### 4.6 `FaiOpSelect` (component mới, dùng ở `/fai`)
- Custom dropdown thay cho `<select>` thuần.
- Mỗi OP hiển thị: `OpNumber · Description`, dot xanh + "● sheet" nếu `dimCount > 0`, "chưa đo" (xám) nếu `dimCount === 0`.
- OP có `opTypeCode === "INS"` (cần `PartOpDto` có sẵn field này — kiểm tra hiện trạng, thêm nếu thiếu) hiển thị thêm icon/nhãn riêng (ví dụ 🔍 hoặc badge nhỏ "Kiểm tra") để phân biệt với OP gia công thường.

### 4.7 `FaiMatrix` — redesign toàn bộ
- **Info bar**: card (`VACard`-style hoặc div bo góc + shadow `va.shadow`) nhiều cột có separator dọc: Part number / Mô tả / Rev / Job / Operation (hoặc "Tất cả OP" nếu sau này hỗ trợ) + "Đo gần nhất" (người đo + thời điểm) bên phải.
- **Stats strip**: card, số lớn mono: Tổng ô / Đã đo / Pass / Fail·NCR / Pending / Pass rate %; "Đang xem: {stage label}" bên phải.
- **Matrix card**: bọc trong `VACard` (`title="Serial × Dimension"`, `sub` = "{n} serial × {m} balloon", `right` = legend Pass/Fail/Chưa đo/NCR bằng chip màu nhỏ).
  - Header balloon: vòng tròn 26px, viền màu theo `categoryCode` (LIN=`va.primary`, ANG=`va.primaryLt`, THD=`va.accent`, GEO=`#5D4037`, SFC=`va.text2`; viền `va.err` nếu `isCritical`), số balloon trong vòng tròn; dưới vòng tròn: nominal±tolerance (hoặc `nominalText`), category code; nếu `dim.opNumber` có giá trị (đang xem OP INS) → thêm nhãn nhỏ "OP{n}" mờ dưới cùng.
  - Cell: nền `va.okBg`/`va.errBg` theo `result`; thêm viền trái 2px `va.err` khi Fail; cờ ⚑ góc phải-trên khi `hasNcr`; chưa đo → `—` mờ opacity 0.35.
  - Tooltip: đổi từ `title` (native) sang **div nổi theo con trỏ** (fixed position, theo `onMouseEnter/onMouseMove/onMouseLeave`), nội dung: Giá trị, Người đo, Stage, Gage, NCR (nếu có), Lúc.
  - Sticky cột Serial (trái) và cột Kết quả (phải) dùng border 2px `va.borderStr` (đậm hơn border thường) để tách biệt vùng sticky; header sticky top giữ nguyên.
  - Click Serial number → `/fai/product/{productId}` (giữ nguyên hành vi hiện tại).
- Export Excel/PDF: giữ nguyên cơ chế gọi API hiện tại, chỉ đổi style nút theo mẫu mới.
- Giữ dòng chú thích cuối "View read-only…".

## 5. Ngoài phạm vi
- `/fai/product/[id]` (Serial Measure Sheet) — không đổi.
- i18n — các route `/fai` hiện chưa dịch (theo CLAUDE.md), spec này không bổ sung i18n, giữ nguyên trạng (toàn tiếng Việt hard-coded như hiện tại).
- Gage selection, MQTT, SignalR — không liên quan.

## 6. Rủi ro / điểm cần lưu ý khi implement
- Cần xác nhận `PartOpDto` (FE) đã có field mã OpType (`opTypeId`/`opTypeName`) — hiện có `opTypeName` nhưng **không có `opTypeCode`**. Cần thêm `OpTypeCode` vào `PartOpDto` (backend) để FE nhận diện OP INS qua `code === "INS"` (không dùng `opTypeName` vì tên hiển thị có thể đổi, code mới là khoá ổn định).
- So sánh `OpNumberSort` khi null: dùng pattern đã có trong codebase (`?? 9999` hoặc parse `OpNumber`) — không tạo pattern mới.
- `FaiSheetDto`/`DimensionDto` thêm field optional `opNumber` — đảm bảo không phá vỡ các consumer khác đang dùng `DimensionDto` (Dimension Sheet, Part & Routing) — field optional, default null, không ảnh hưởng.
