# QC Final — Blind Inspection Correction — Design

Ngày: 2026-06-22

## Bối cảnh

Phiên làm việc trước đó (xem [`2026-06-22-fai-qc-inline-desktop-design.md`](2026-06-22-fai-qc-inline-desktop-design.md)) đã hiểu sai bản chất nghiệp vụ của "QC Final": tưởng rằng QC Final là **re-inspect** — đọc lại các dimension Fail từ InprocessFAI rồi cho QC đo xác nhận lại. User đã chỉnh lại: QC Final là một **công đoạn đo kiểm độc lập hoàn toàn** ("blind inspection"), không đọc/biết kết quả của InprocessFAI hay QCInline, và phải kiểm **100% dimension trên 100% sản phẩm**.

Bổ sung nghiệp vụ quan trọng (lần đầu xuất hiện trong hệ thống): **phòng kỹ thuật và phòng QC có thể có dimsheet khác nhau cho cùng một kích thước.** Ví dụ: theo bản vẽ khách hàng, kích thước 01 là 0.03–0.05; kỹ thuật muốn kiểm soát gắt hơn nên đổi thành 0.04–0.05 cho dimsheet sản xuất (Operator/QC Inline dùng dimsheet này); nhưng QC Final phải bám đúng yêu cầu khách hàng (0.03–0.05) — khác dung sai với dimsheet kỹ thuật.

Để tránh QC phải tạo lại toàn bộ dimsheet từ đầu: **`Dimension.IsFinal = true`** đánh dấu các dimension mà QC **chấp nhận tái sử dụng nguyên dung sai kỹ thuật** cho QC Final (không cần tạo riêng). Với các kích thước QC cần dung sai khác, QC tự tạo dimension mới (cùng `BalloonNumber`, dung sai riêng) và gán nó cho một **OP loại INS** trong routing — OP này đóng vai trò "dimsheet của QC". Mọi OP loại INS trong routing đều dùng chung cơ chế gộp này (không phân biệt OP INS giữa routing hay cuối routing).

## Phạm vi

1. Sửa ý nghĩa & comment của `Dimension.IsFinal` (không đổi field/migration)
2. Sửa `GetFaiSheetQueryHandler` — nhánh gộp dimension cho OP loại INS: gồm dimension riêng của chính OP INS (dung sai QC) + dimension `IsFinal=true` từ các OP trước (dung sai kỹ thuật được QC chấp nhận tái sử dụng), ưu tiên dimension riêng của OP INS khi trùng `BalloonNumber`
3. Desktop `FaiViewModel` — bỏ hoàn toàn lọc "chỉ hiện Fail" và cách đọc state đặc cách cho Final; QC Final giờ đọc/ghi đồng nhất stage `QCFinal`, không tham chiếu InprocessFAI/QCInline, giống cách Basic/QcInline đọc stage của riêng nó
4. Desktop Dashboard — điều kiện hiện shortcut "QC Final": **OP đang chọn là loại INS VÀ sản phẩm đã hoàn thiện gia công** (cả hai, không phải một trong hai)

**Ngoài phạm vi:**
- Nhánh `allOps` (chế độ "Tất cả OP" trên trang Web `/fai`) — không đổi, không filter `IsFinal`
- "QC Inline" (rate sampling, đã làm ở phiên trước) — không đổi, không liên quan tới sửa lần này
- Không đổi tên field `Dimension.IsFinal` hay tạo migration mới

## Backend

### `Dimension.IsFinal` — sửa comment

```csharp
// src/ShopfloorManager.Domain/Entities/Dimension.cs
/// <summary>
/// QC tái sử dụng dimension này (do kỹ thuật tạo) cho QC Final — không cần QC tạo riêng.
/// Dimension nào QC cần dung sai khác bản vẽ kỹ thuật (ví dụ bám sát yêu cầu khách hàng) thì
/// KHÔNG đánh dấu IsFinal — QC tự tạo dimension mới (cùng BalloonNumber) gán cho OP loại INS.
/// </summary>
public bool IsFinal { get; set; }
```

### `GetFaiSheetQueryHandler` — sửa nhánh `isInspectionOp`

Thay đoạn lấy `dims` khi `isInspectionOp == true` (giữ nguyên `allOps == true` không đổi):

```csharp
List<Dimension> dims;
if (allOps)
{
    // KHÔNG ĐỔI — giữ nguyên logic hiện có (gồm toàn bộ dimension mọi OP, không filter IsFinal)
    var routingOps = await db.PartOps
        .Where(p => (p.RoutingRevId == job.RoutingRevId && !p.ForJobOnly) || (p.ForJobOnly && p.JobId == job.Id))
        .ToListAsync(ct);
    var scopedOpIds = routingOps.Select(p => p.Id).ToList();
    dims = await db.Dimensions
        .Include(d => d.Category).Include(d => d.PartOp)
        .Where(d => scopedOpIds.Contains(d.PartOpId))
        .OrderBy(d => d.PartOp.OpNumberSort ?? 9999).ThenBy(d => d.BalloonSort ?? 9999).ThenBy(d => d.BalloonNumber)
        .ToListAsync(ct);
}
else if (isInspectionOp)
{
    var routingOps = await db.PartOps
        .Where(p => (p.RoutingRevId == job.RoutingRevId && !p.ForJobOnly) || (p.ForJobOnly && p.JobId == job.Id))
        .ToListAsync(ct);
    decimal EffectiveSort(PartOp p) => p.OpNumberSort ?? 9999m;
    var priorOpIds = routingOps.Where(p => EffectiveSort(p) < EffectiveSort(op!)).Select(p => p.Id).ToList();

    // Dimension từ OP trước — CHỈ những dim QC chấp nhận tái sử dụng nguyên dung sai kỹ thuật
    var priorFinalDims = await db.Dimensions
        .Include(d => d.Category).Include(d => d.PartOp)
        .Where(d => priorOpIds.Contains(d.PartOpId) && d.IsFinal)
        .ToListAsync(ct);

    // Dimension QC tự tạo trực tiếp trên OP INS này (dung sai riêng của QC)
    var ownDims = await db.Dimensions
        .Include(d => d.Category).Include(d => d.PartOp)
        .Where(d => d.PartOpId == op!.Id)
        .ToListAsync(ct);

    // Trùng BalloonNumber → dimension QC tự tạo trên OP INS THẮNG (loại bản tái sử dụng)
    var ownBalloons = ownDims.Select(d => d.BalloonNumber).ToHashSet();
    dims = ownDims
        .Concat(priorFinalDims.Where(d => !ownBalloons.Contains(d.BalloonNumber)))
        .OrderBy(d => d.PartOp.OpNumberSort ?? 9999).ThenBy(d => d.BalloonSort ?? 9999).ThenBy(d => d.BalloonNumber)
        .ToList();
}
else
{
    // KHÔNG ĐỔI — OP thường, chỉ lấy dimension của chính OP đó
    dims = await db.Dimensions
        .Include(d => d.Category)
        .Where(d => d.PartOpId == req.PartOpId)
        .OrderBy(d => d.BalloonSort ?? 9999).ThenBy(d => d.BalloonNumber)
        .ToListAsync(ct);
}
```

`OpNumber` tag trên `DimensionDto` (dùng để hiển thị "dim này gộp từ OP nào"): chỉ gắn cho dim trong `priorFinalDims` (như cách `tagOpNumber` hiện có đang làm) — dim trong `ownDims` (thuộc chính OP INS) để `OpNumber = null` vì nó không phải "mượn từ OP khác". Cụ thể: giữ `tagOpNumber = allOps || isInspectionOp` như cũ, nhưng khi build `dimDtos`, chỉ set `OpNumber: d.PartOp.OpNumber` nếu `d.PartOpId != op?.Id` (tức không phải dim của chính OP INS đang xem).

## Desktop — `FaiViewModel`

### Bỏ đặc cách Final trong `LoadAsync`

- Xóa block lọc "chỉ giữ Dimensions có `State == Fail`" sau khi build `Dimensions`.
- Đọc state đồng nhất cho **cả 3 mode** bằng đúng stage tương ứng — không còn fallback giữa các stage:
  ```csharp
  var stageCell = cell?.ByStage?.GetValueOrDefault(ToServerStage(Mode));
  ```
  (Final → đọc đúng stage QCFinal(2) — không đọc/tham chiếu InprocessFAI(0) hay QCInline(1) nữa, đúng tinh thần "blind inspection".)
- `ErrorMessage` khi rỗng: dùng chung 1 câu cho mọi mode — `"OP này chưa có kích thước nào được định nghĩa."` (xóa câu riêng "Không có kích thước nào ở trạng thái FAIL để re-inspect.")
- `SelectedDimension` ban đầu: `Dimensions.FirstOrDefault(d => !d.IsMeasured)` cho cả Basic và Final (auto-advance gợi ý dim chưa đo tiếp theo); QC Inline giữ `null` (không tự chọn).

### `IsInputLocked` — bỏ switch theo Mode

```csharp
// Một rule chung cho cả 3 mode: khóa sau khi đã đo (bất kể Pass/Fail)
public bool IsInputLocked => SelectedDimension?.IsMeasured == true;
```
(Xóa nhánh riêng `FaiMode.Final => State == Pass`.)

### Sau khi lưu đo (`SaveAsync`) — bỏ logic tìm Fail tiếp theo

```csharp
SelectedDimension = Mode == FaiMode.QcInline
    ? null  // QC tự chọn balloon tiếp theo
    : Dimensions.FirstOrDefault(d => !d.IsMeasured);  // Basic & Final: auto-advance
```

## Desktop — `PartOpDto` + Dashboard shortcut

`src/ShopfloorManager.Desktop/Models/PartOpDto.cs` thêm field `OpTypeCode` (server đã trả `opTypeCode` trong JSON nhưng Desktop chưa khai báo nên bị bỏ qua khi deserialize):

```csharp
public record PartOpDto(
    int Id, string OpNumber, int? OpTypeId, string? OpTypeName, string? OpTypeCode, ...);
```

`DashboardViewModel.RefreshShortcuts` — điều kiện shortcut "QC Final" cần **cả hai**:

```csharp
bool canQcFinal = !_work.IsViewMode && hasProd
    && _work.CurrentOp?.OpTypeCode == "INS"
    && _work.CurrentProduct?.StatusCode == "complete";

if (role is "QC Inspector" or "Administrator")
{
    Add("FAI Final", "ClipboardCheckOutline", "fai-final", when: canQcFinal);
    Add("QC Inline", "Magnify",               "qc-inline", when: canQcInline);  // không đổi
}
```

## Testing

- Backend: unit test `GetFaiSheetQueryHandler` cho nhánh `isInspectionOp` — case (a) chỉ dim `IsFinal=true` từ OP trước được gộp, dim `IsFinal=false` bị loại; case (b) dim riêng của OP INS luôn được gồm; case (c) trùng `BalloonNumber` giữa dim riêng OP INS và dim `IsFinal=true` từ OP trước → dim riêng OP INS thắng, không bị duplicate; case (d) `OpNumber` tag đúng — null cho dim riêng OP INS, có giá trị cho dim tái sử dụng.
- Desktop: build + verify thủ công (không có test framework cho WPF, như đã xác nhận ở phiên trước).
