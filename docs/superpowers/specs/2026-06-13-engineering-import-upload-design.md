# Thiết kế: Hoàn thiện Import/Upload — nhóm "Kỹ thuật"

**Ngày:** 2026-06-13
**Phạm vi:** `clients/web/app/(main)/documents`, `parts`, `parts/[id]/operations`, `dimsheet` + backend `TechDocumentsController`/`OperationsController`

## Mục tiêu

Hai cải tiến độc lập cho luồng Import/Upload tài liệu kỹ thuật:

- **Phase A** — nút "Tải template Excel" cho 2 dialog import hiện có (`ImportOpsDialog`, `ImportDimensionsDialog`).
- **Phase B** — "Tải lên hàng loạt" (bulk upload) trong `/documents`: chọn nhiều file cùng lúc, hệ thống tự nhận diện Part/Rev/RoutingRev/OP (hoặc Job/OP) + loại tài liệu từ **tên file** theo quy ước đặt tên mới, phát hiện trùng lặp/segment thiếu/file không hợp lệ, hiển thị bảng xem trước trước khi upload, và log kết quả sau khi xong.

Không thay đổi form upload đơn lẻ hiện có trong `/documents` (vẫn giữ nguyên, dùng cho trường hợp 1 file).

---

## Phase A — Tải template Excel cho Import OPs / Import Dimensions

### Backend

Thêm helper mới `src/ShopfloorManager.API/Common/ExcelTemplateBuilder.cs` (cùng vị trí với `ExcelImportReader.cs`, dùng ClosedXML — không cần MediatR vì không truy vấn DB, chỉ build file tĩnh):

```csharp
public static class ExcelTemplateBuilder
{
    // Header: OpNumber, OpType, Description, SetupTime, ProdTime + 1 dòng ví dụ
    public static byte[] BuildOpsTemplate();

    // Header: BalloonNumber, Code, Description, Nominal, TolPlus, TolMinus, Unit, Category + 1 dòng ví dụ
    public static byte[] BuildDimensionsTemplate();
}
```

Thêm 2 endpoint vào `OperationsController` (đặt cạnh `import` endpoints hiện có):

- `GET /api/v1/operations/import/template` → `BuildOpsTemplate()`
- `GET /api/v1/operations/dimensions/import/template` → `BuildDimensionsTemplate()` (không cần `opId` vì cấu trúc cột giống nhau cho mọi OP)

Cả 2 trả `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, `[Authorize]` (không cần ràng buộc role — chỉ đọc template tĩnh).

Cột mẫu (theo đúng cột mà `OperationsController` import đang đọc, case-insensitive/bỏ space):

| Endpoint | Header | Dòng ví dụ |
|---|---|---|
| ops template | `OpNumber, OpType, Description, SetupTime, ProdTime` | `10, CNC, Phay mặt đầu, 30, 5` |
| dimensions template | `BalloonNumber, Code, Description, Nominal, TolPlus, TolMinus, Unit, Category` | `Ø1, D1, Đường kính ngoài, 25.4, 0.05, 0.05, mm, LIN` |

### Frontend

`api-client.ts` thêm 2 hàm trả `Blob`:

```ts
operations: {
  ...
  importOpsTemplate: () => requestBlob('/api/v1/operations/import/template'),
  importDimsTemplate: () => requestBlob('/api/v1/operations/dimensions/import/template'),
}
```

(`requestBlob` là helper mới tương tự `request` nhưng trả `res.blob()` thay vì JSON — cần thêm vào `api-client.ts`.)

`ImportOpsDialog` và `ImportDimensionsDialog`: thêm link "⬇ Tải template" phía trên input chọn file — gọi hàm tương ứng, tạo `URL.createObjectURL(blob)` + `<a download="...">` rồi revoke URL.

i18n: thêm key `parts.importOps.template` / `parts.importDims.template` (vi+en).

---

## Phase B — Bulk Upload với tự nhận diện theo tên file

### Quy ước đặt tên file (naming convention)

| Loại | FileType.Code | Gắn vào | Pattern | Ví dụ |
|---|---|---|---|---|
| Part-level | `DRW`, `CAD` | PartRev | `{PartNumber}-{PartRevCode}-{FileTypeCode}.ext` | `SHAFT-50H6-A-DRW.pdf` |
| Standard OP | `GCD`, `THD`, `TLS`, `CAM` | PartOp (qua RoutingRev) | `{PartNumber}-{PartRevCode}-{RoutingRevCode}-{OPNumber}-{FileTypeCode}.ext` | `SHAFT-50H6-A-R1-20-GCD.nc` |
| Segment (GCD nhiều phần) | `GCD` (IsSegment=true) | PartOp | `{PartNumber}-{PartRevCode}-{RoutingRevCode}-{OPNumber}-{FileTypeCode}-{i}_{n}.ext` | `SHAFT-50H6-A-R1-20-GCD-1_3.nc` |
| ForJobOnly OP | `RTC`, `FXT` | PartOp (gắn Job) | `{JobNumber}-{OPNumber}-{FileTypeCode}.ext` | `J2026-001-80-RTC.pdf` |

Separator cố định `-` (hyphen).

### Thuật toán parse tên file (phải sang trái)

`PartNumber`/`JobNumber` trong thực tế có thể chứa dấu `-` (vd `SHAFT-50H6`, `J2026-001`), nên không thể split cố định từ trái. Parser xử lý **từ phải sang trái**, dựa vào các token có hình dạng cố định:

1. Tách phần mở rộng file (`.ext`).
2. Split phần còn lại theo `-` → `tokens[]`.
3. Nếu `tokens[last]` khớp `^\d+_\d+$` (vd `1_3`) → đây là segment suffix, lưu lại `(segmentIndex, segmentTotal)`, pop khỏi `tokens`.
4. `tokens[last]` (sau khi pop segment nếu có) phải khớp (case-insensitive) một `FileType.Code` đang `IsActive` → đây là `fileTypeCode`. Nếu không khớp → **Invalid: "Không nhận diện được loại file"**.
5. Dựa vào flags của FileType đã match (`isPartNumber`, `isOpNumber`, `isJobNumber`):
   - **RTC/FXT** (`isJobNumber && isOpNumber && !isPartNumber`): cần ≥2 token còn lại. `tokens[last-1]` = `opNumber`, phần còn lại (join lại bằng `-`) = `jobNumber`.
   - **GCD/THD/TLS/CAM** (`isPartNumber && isOpNumber`): cần ≥3 token còn lại. `tokens[last-1]` = `opNumber`, `tokens[last-2]` = `routingRevCode`, `tokens[last-3]` = `partRevCode`, phần còn lại = `partNumber`.
   - **DRW/CAD** (`isPartNumber && !isOpNumber`): cần ≥1 token còn lại. `tokens[last-1]` = `partRevCode`, phần còn lại = `partNumber`.
   - Nếu segment suffix tồn tại nhưng `fileType.isSegment == false` → **Invalid: "Loại file này không hỗ trợ segment"**.
   - Nếu không đủ token → **Invalid: "Tên file thiếu thông tin (Part/Rev/Routing/OP)"**.

Kết quả parse thành công → object `ParsedToken { fileName, fileTypeCode, partNumber?, partRevCode?, routingRevCode?, opNumber?, jobNumber?, segmentIndex?, segmentTotal?, fileSizeBytes }`.

### Backend — endpoint resolve-batch

Thêm vào `TechDocumentsController`:

```csharp
[HttpPost("resolve-batch")]
[Authorize(Roles = "Administrator,Manager,Engineer")]
public async Task<IActionResult> ResolveBatch([FromBody] List<ResolveBatchItem> items)
```

Logic chuyển vào Application layer — `ResolveBulkUploadQuery(List<ResolveBatchItem> Items, int UserId)` / handler trong `TechDocumentQueries.cs`, business logic:

Với mỗi item, theo `fileTypeCode`:

1. Tìm `FileType` theo `Code` (case-insensitive, `IsActive=true`) → nếu không có → `Invalid: "Loại file không hợp lệ"`.
2. Resolve entity theo flags:
   - **RTC/FXT**: `Job` theo `JobNumber` → `PartOp` where `JobId == job.Id && OpNumber == opNumber && ForJobOnly == true` → không có Job: `Invalid: "Không tìm thấy Job {jobNumber}"`; không có OP: `Invalid: "Không tìm thấy OP riêng {opNumber} trong Job {jobNumber}"`.
   - **GCD/THD/TLS/CAM**: `Part` theo `PartNumber` → `PartRev` theo `RevCode` (của Part đó) → `RoutingRev` theo `RevCode` (thuộc `Routing` của `PartRev` đó, **không yêu cầu `IsActive`** — để vẫn nhận diện được RoutingRev cũ) → `PartOp` where `RoutingRevId == routingRev.Id && OpNumber == opNumber`. Lỗi cụ thể theo từng bước: `"Không tìm thấy Part {partNumber}"` / `"Không tìm thấy Revision {partRevCode}"` / `"Không tìm thấy Routing Rev {routingRevCode}"` / `"Không tìm thấy OP {opNumber}"`.
   - **DRW/CAD**: `Part` theo `PartNumber` → `PartRev` theo `RevCode`. Lỗi: `"Không tìm thấy Part {partNumber}"` / `"Không tìm thấy Revision {partRevCode}"`.
3. Nếu resolve thành công → tìm `TechDocument` hiện có cùng `(PartRevId/PartOpId/JobId tương ứng, FileTypeId, Segment == "{i}_{n}" hoặc null)`:
   - `Status == Approved` → `Invalid: "File đã được approve — không thể ghi đè"`.
   - `Status == Pending && CreatedBy != UserId` → `Invalid: "Đang chờ duyệt bởi người khác"`.
   - `Status == Pending && CreatedBy == UserId` → `Ready` (ghi đè khi upload, giống flow hiện tại).
   - `Status == Rejected` → `Ready` (note: `"Sẽ thay thế file đã bị từ chối"`).
   - Không có existing → `Ready`.
4. Nếu `fileType.IsSegment` và `segmentTotal.HasValue`: query toàn bộ `TechDocument` cùng target+`FileTypeId` có `Segment` dạng `"{x}_{segmentTotal}"` (không phân biệt trạng thái, trừ đã xoá) → trả về `existingSegments: string[]` (các index `x` đã có trên server).

Response — `ResolveBatchResultDto` (theo đúng thứ tự input):

```csharp
record ResolveBatchResultDto(
    string FileName,
    string Status,        // "Ready" | "Invalid"
    string? Reason,
    int? FileTypeId,
    int? PartRevId, int? PartOpId, int? JobId,
    string? ResolvedPartNumber, string? ResolvedRevCode,
    string? ResolvedRoutingRevCode, string? ResolvedOpNumber, string? ResolvedJobNumber,
    List<string>? ExistingSegments);
```

(`Status` chỉ có `Ready`/`Invalid` ở backend — `Duplicate-trong-lô` và `SegmentIncomplete` được tính ở client sau khi gộp kết quả, vì cần thông tin về các file khác trong cùng lô.)

### Frontend — `BulkUploadDialog`

File mới `clients/web/components/documents/bulk-upload-dialog.tsx`, mở từ nút **"⬆ Tải lên hàng loạt"** (đặt cạnh nút "+ Upload" hiện có trên `/documents`, luôn hiển thị — không phụ thuộc context).

**State machine:** `idle` (chọn file) → `resolving` (đang gọi resolve-batch) → `preview` (bảng xem trước, user review) → `uploading` (đang upload tuần tự) → `done` (tổng kết).

**Bước 1 — Chọn file (idle):**
- `<input type="file" multiple>` — chọn nhiều file (trình duyệt cho phép chọn cả thư mục qua dialog OS trên một số hệ điều hành).
- Thêm input phụ với `webkitdirectory` ("Chọn thư mục") để tiện chọn cả folder hồ sơ.
- Khi có file → tự parse từng tên file (thuật toán trên) → gọi `resolve-batch` cho các file parse thành công → chuyển sang `preview`. File parse lỗi đưa thẳng vào bảng preview với `status="Invalid"` (không gọi API).

**Bước 2 — Bảng xem trước (preview):**

Cột: `☑` | Tên file | Loại (badge màu theo `FILE_TYPE_COLORS` có sẵn) | Phát hiện (`{PartNumber} Rev{Rev} · R{RoutingRev} · OP{Op}` hoặc `Job {JobNumber} · OP{Op}`) | Kích thước | Trạng thái.

Trạng thái hiển thị (tính ở client sau resolve):
- ✅ **Sẵn sàng** — checked mặc định.
- ⚠ **Trùng trong lô** — ≥2 file trong lô resolve về cùng `(PartRevId/PartOpId/JobId, FileTypeId, segment)`; giữ file đầu tiên là Sẵn sàng, các file sau bị đánh dấu Invalid, unchecked, lý do `"Trùng với {fileName đầu tiên}"`.
- ⚠ **Thiếu segment** — với nhóm `(target, FileTypeId)` có `fileType.IsSegment`: hợp tất cả `segmentIndex` trong lô (status Ready) với `existingSegments` từ server; nếu không phủ đủ `1..segmentTotal` → toàn bộ file Ready trong nhóm này chuyển `Invalid`, unchecked, lý do `"Thiếu segment {missing list}/{segmentTotal}"`. Nếu các file trong nhóm khai báo `segmentTotal` khác nhau → `Invalid: "Tổng số segment không khớp giữa các file"`.
- ❌ **Không hợp lệ** — `reason` từ resolve-batch hoặc lỗi parse.

Footer: `"{checked}/{total} file sẵn sàng"` + nút **"Tải lên ({checked})"** (disabled nếu `checked === 0`).

**Bước 3 — Upload (uploading):**
- Tuần tự (một file tại một thời điểm — đơn giản, v1 không cần concurrency) cho từng row đã check: gọi `api.techDocuments.create({...})` (reuse field mapping từ `handleUpload` hiện có, set thêm `code`/`segment` nếu có) → `PUT` file lên `uploadUrl`.
- Mỗi row cập nhật trạng thái real-time: `Đang tải...` → `✅ Thành công` / `❌ Lỗi: {message}`.

**Bước 4 — Tổng kết (done):**
- Banner `"Đã upload {success}/{total} thành công"`. Nếu có lỗi, danh sách file lỗi + lý do vẫn hiển thị trong bảng.
- Nút "Đóng" → đóng dialog + `load()` (refresh danh sách doc chính của `/documents`).

### i18n

Namespace `documents.bulkUpload`: `trigger`, `selectFiles`, `selectFolder`, `resolving`, `table.headers.*` (file/type/detected/size/status), `status.*` (ready/duplicate/invalid/segmentIncomplete), `reason.*` (các message lỗi ở trên — tham số hoá theo `{...}`), `uploadButton {count}`, `summary {success}/{total}`, `close`.

---

## Ngoài phạm vi (out of scope v1)

- Không đổi form upload đơn lẻ hiện có trong `/documents`.
- Không validate extension theo FileType (vd GCD phải `.nc`).
- Không giới hạn số lượng/tổng kích thước file trong 1 lô (để mặc định trình duyệt).
- Không thêm concurrency cho upload tuần tự (có thể tối ưu sau nếu cần).
- Bulk upload chỉ ở `/documents` (global) — không thêm entrypoint riêng ở `/parts`, `/dimsheet`.

## Kiểm tra (verification plan)

1. **Phase A**: mở `ImportOpsDialog`/`ImportDimensionsDialog` → "Tải template" → file `.xlsx` đúng header download được, mở bằng Excel/LibreOffice xem đúng cột.
2. **Phase B**:
   - Chuẩn bị bộ file test theo naming convention cho 1 Part có sẵn (vd `SHAFT-50H6`): `SHAFT-50H6-A-DRW.pdf`, `SHAFT-50H6-A-R1-20-GCD.nc`, `SHAFT-50H6-A-R1-20-GCD-1_3.nc` + `-2_3` + `-3_3`, `SHAFT-50H6-A-R1-20-TLS.pdf`.
   - Test file không hợp lệ: tên sai pattern, FileType code không tồn tại, Part/OP không tồn tại, file trùng lặp trong lô, segment thiếu 1 phần.
   - Verify bảng preview hiển thị đúng trạng thái + lý do cho từng trường hợp.
   - Upload các file "Sẵn sàng" → verify TechDocument được tạo đúng `PartRevId`/`PartOpId`/`FileTypeId`/`Segment`, object key MinIO đúng path convention, log kết quả đúng.
   - Test ForJobOnly: `J2026-001-80-RTC.pdf` với Job/OP có sẵn.
