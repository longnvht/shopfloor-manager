# FAI — QC Inline Flow & MeasureStage Fix (Desktop) — Design

Ngày: 2026-06-22

## Bối cảnh

Theo `Project_Documents/06_dimensions_fai.md` và memory `fai_measure_stage_design`, hệ thống FAI có 3 giai đoạn đo độc lập (`MeasureStage`: `InprocessFAI=0`, `QCInline=1`, `QCFinal=2`). Backend đã hỗ trợ đầy đủ (migration `AddMeasureStage`, `SaveMeasureCommand.MeasureStage`, `GetFaiSheet` group theo stage). Desktop hiện chỉ có 2 mode: **FAI Basic** (Operator, mọi role có session) và **FAI Final** (QC Inspector/Admin, re-inspect dims Fail) — cả hai **không gửi `MeasureStage`** trong request lưu đo, nên record luôn bị lưu nhầm là `InprocessFAI` mặc định kể cả khi đang ở FAI Final.

Phase 4b (Desktop role-aware FAI) trong progress log còn thiếu: flow **QC Inline** (QC Inspector kiểm ngẫu nhiên sản phẩm đã hoàn thành OP) và cơ chế **QC Inline Rate** (% gợi ý kiểm tra, factory-wide default + override theo Job/PartOp).

## Phạm vi

1. Backend: entity `QcInlineRate` + migration, API CRUD (Admin/Manager) + API đọc rate hiệu lực (Desktop dùng)
2. Web: tab mới trong Master Data hub để CRUD rate
3. Desktop: sửa bug thiếu `MeasureStage`, đổi `IsFinalMode` (bool) → `FaiMode` enum (`Basic`/`Final`/`QcInline`), thêm flow QC Inline, thêm shortcut + điều kiện truy cập

**Ngoài phạm vi:** auto-suggest sản phẩm cụ thể cần kiểm theo rate (QC tự chọn hoàn toàn, rate chỉ hiển thị tham khảo); thay đổi điều kiện FAI Basic/FAI Final hiện có.

## Backend

### Entity `QcInlineRate`

```csharp
public class QcInlineRate : BaseEntity
{
    public int? JobId { get; set; }
    public int? PartOpId { get; set; }
    public decimal RatePercent { get; set; } // 0–100
}
```

Một row đặc biệt `JobId=null, PartOpId=null` = mặc định toàn nhà máy, luôn tồn tại (seed trong migration), không cho xóa.

Độ ưu tiên resolve effective rate: `(JobId, PartOpId)` cụ thể → `(JobId, null)` → `(null, PartOpId)` → `(null, null)` (factory default).

### API

- `GET /api/v1/qc-inline-rates` — list toàn bộ (Admin, Manager) — phục vụ trang Web CRUD
- `POST /api/v1/qc-inline-rates` — tạo mới (Admin, Manager)
- `PUT /api/v1/qc-inline-rates/{id}` — sửa (Admin, Manager); row factory-default chỉ cho sửa `RatePercent`
- `DELETE /api/v1/qc-inline-rates/{id}` — xóa (Admin, Manager); chặn xóa row factory-default
- `GET /api/v1/fai/qc-inline-rate?jobId={jobId}&partOpId={partOpId}` — trả % hiệu lực theo độ ưu tiên trên, mọi role có quyền vào FAI đều gọi được — Desktop dùng để hiển thị banner thông tin

### Migration

`AddQcInlineRates` — bảng `qc_inline_rates`, cột `job_id` (FK nullable → `jobs.id`), `part_op_id` (FK nullable → `part_ops.id`), `rate_percent DECIMAL(5,2)`, kế thừa cột audit từ `BaseEntity`. Seed 1 row factory-default với `RatePercent` khởi tạo (ví dụ 10%).

## Web

Thêm tab **"Mức kiểm QC Inline"** vào `clients/web/app/(main)/master/page.tsx` (tab-based hub hiện có, không tạo route riêng).

- Table: cột Job (tên hoặc "— Tất cả Job —"), PartOp/OP (hoặc "— Tất cả OP —"), Rate %, action Sửa/Xóa
- Dialog form riêng (không dùng `MasterItemDialog` generic — schema khác: 2 combobox optional + số) — `VACombobox` chọn Job (optional), `VACombobox` chọn PartOp (optional, lọc theo Job đã chọn nếu có), input Rate %
- Row factory-default: không có nút Xóa, chỉ Sửa %
- Hiển thị/truy cập tab: chỉ Admin, Manager (theo `useAuthStore` role)
- i18n: thêm key vào `messages/vi.json` + `messages/en.json` namespace `master` (hoặc namespace mới `qcInlineRates` nếu tách riêng cho rõ)

## Desktop

### `FaiMode` enum

Thay `bool IsFinalMode` trong `FaiViewModel` bằng:
```csharp
public enum FaiMode { Basic, Final, QcInline }
public FaiMode Mode { get; set; }
```
Cập nhật mọi nơi tham chiếu `IsFinalMode` (PageTitle, IsInputLocked, LoadAsync filter, SaveAsync) sang switch theo `Mode`.

### Fix bug MeasureStage khi lưu đo

`SaveAsync` thêm field `MeasureStage` vào request gửi `/api/v1/fai/measure`:
```csharp
MeasureStage = Mode switch
{
    FaiMode.Final    => MeasureStage.QCFinal,
    FaiMode.QcInline => MeasureStage.QCInline,
    _                => MeasureStage.InprocessFAI
}
```
(`IsFinal` field giữ nguyên `Mode == FaiMode.Final` để tương thích các chỗ khác đang đọc field này — theo memory, `IsFinal`/`FinalOpId` là legacy, không dùng thêm logic mới dựa trên chúng.)

### Dimension filter theo mode trong `LoadAsync`

- `Basic`: như hiện tại — tất cả dims, lock sau khi đo 1 lần
- `Final`: như hiện tại — chỉ dims đang Fail, lock khi Pass
- `QcInline`: tất cả dims hiển thị; QC tự chọn balloon muốn đo. Dim đã có `MeasureValue` ở stage `QCInline` cho `(DimensionId, ProductId)` → hiển thị giá trị cũ và lock (ràng buộc 1 lần/stage ở backend); dim chưa có → cho đo. Không bắt buộc đo hết toàn bộ (không có "auto-advance" ép tuần tự như Basic).

### Banner thông tin rate

Khi vào `QcInline` mode, gọi `GET /api/v1/fai/qc-inline-rate?jobId=&partOpId=`, hiển thị dòng phụ trong `PageTitle`/header: `"Mức kiểm đề xuất: {rate}%"`. Chỉ mang tính thông tin, không chặn hành vi đo.

### Shortcut & điều kiện truy cập (`DashboardViewModel.RefreshShortcuts`)

```csharp
bool canQcInline = !_work.IsViewMode && hasProd
    && _work.CurrentProduct?.StatusCode == "complete";

if (role is "QC Inspector" or "Administrator")
{
    Add("FAI Final", "ClipboardCheckOutline", "fai-final", when: canFai);
    Add("QC Inline",  "MagnifyQuestion",       "qc-inline", when: canQcInline);
}
```
`canQcInline` khác `canFai`: yêu cầu session **đã hoàn thành** (`StatusCode == "complete"`, bất kể Operator nào chạy), không yêu cầu session đang active của chính máy hiện tại — đúng rule "OP đó đã có session completed của Operator" (memory `fai_measure_stage_design`).

## Testing

- Backend: unit test `SaveMeasureCommandHandler` xác nhận `MeasureStage` được lưu đúng theo request; unit test resolve effective rate theo đúng thứ tự ưu tiên 4 cấp; test chặn xóa row factory-default; test phân quyền CRUD (chỉ Admin/Manager).
- Web: không yêu cầu test tự động theo pattern hiện có của Master Data hub (manual verify qua browser).
- Desktop: không có test framework hiện hữu cho WPF ViewModel trong repo (theo khảo sát) — verify thủ công qua build + chạy app, theo skill `superpowers:verification-before-completion`.
