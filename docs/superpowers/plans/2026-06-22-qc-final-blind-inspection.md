# QC Final — Blind Inspection Correction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Correct "QC Final" from a wrong "re-inspect Fail dims" implementation to the real business process — an independent, blind 100% inspection that aggregates Engineering's reusable dimensions (`IsFinal=true`) with QC's own dimensions defined directly on the routing's OP-INS, and fix the Desktop shortcut condition accordingly.

**Architecture:** Backend change is isolated to `GetFaiSheetQueryHandler`'s OP-INS aggregation branch (new merge-by-balloon-number logic, `IsFinal` filter). Desktop change removes all Final-specific special-casing from `FaiViewModel` so Final behaves identically to Basic (read/write only its own `MeasureStage`, lock after measuring, auto-advance) — the only remaining differences are role restriction and the shortcut's visibility condition in `DashboardViewModel`.

**Tech Stack:** ASP.NET Core 9 / MediatR / EF Core 9 (Npgsql, InMemory for tests) — WPF .NET 9 / CommunityToolkit.Mvvm.

## Global Constraints

- Do NOT change the `allOps` branch (`partOpId` query param omitted, "Tất cả OP" on web `/fai`) — it must keep showing every dimension from every OP, unfiltered by `IsFinal`.
- Do NOT change the "QC Inline" feature (rate-sampling, built in the prior session) — out of scope, untouched.
- Do NOT rename the `Dimension.IsFinal` field or add a migration — only its doc comment and the code that interprets it change.
- `ShopfloorManager.Desktop` has no project reference to `ShopfloorManager.Domain` — never reference `ShopfloorManager.Domain.Enums.MeasureStage` from Desktop code (already true; nothing new to avoid here).
- Backend tests live in `ShopfloorManager.Application.Tests` (xUnit + EF Core InMemory via `TestDbContextFactory.Create()`) — no test project exists for the Desktop (WPF) project; Desktop changes are verified by build + manual run only.
- `IApiClient.GetAsync<TResponse>`/`PostAsync<TRequest,TResponse>` already deserialize into `ApiResponse<TResponse>` — no new response wrapper types needed for any task below.

---

### Task 1: Backend — fix OP-INS dimension aggregation + `Dimension.IsFinal` doc comment

**Files:**
- Modify: `src/ShopfloorManager.Domain/Entities/Dimension.cs:48`
- Modify: `src/ShopfloorManager.Application/Production/FaiCommands.cs:70-131` (inside `GetFaiSheetQueryHandler.Handle`)
- Create: `src/ShopfloorManager.Application.Tests/QcFinalDimensionAggregationTests.cs`

**Interfaces:**
- Consumes: `IShopfloorDbContext.Dimensions`/`PartOps`/`Jobs`/`Routings` (existing), `TestDbContextFactory.Create()` (existing test helper).
- Produces: corrected `GetFaiSheetQueryHandler.Handle` behavior consumed by Task 2 (Desktop `FaiViewModel.LoadAsync` reads the resulting `FaiSheetDto.Dimensions`/`FaiCellDto.ByStage` — types are unchanged, only which dimensions get returned changes).

- [ ] **Step 1: Write the failing tests**

Create `src/ShopfloorManager.Application.Tests/QcFinalDimensionAggregationTests.cs`:

```csharp
using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Infrastructure.Data;
using Xunit;

namespace ShopfloorManager.Application.Tests;

public class QcFinalDimensionAggregationTests
{
    private static async Task<(ShopfloorDbContext Db, Job Job, PartOp Op60, PartOp InsOp)> SeedAsync()
    {
        var db = TestDbContextFactory.Create();

        var part = new Part { PartNumber = "SHAFT-50H6", Description = "Trục dẫn động" };
        db.Parts.Add(part);
        var partRev = new PartRev { Part = part, RevCode = "B" };
        db.PartRevs.Add(partRev);
        var routing = new Routing { PartRev = partRev };
        db.Routings.Add(routing);
        var routingRev = new RoutingRev { Routing = routing, RevCode = "R1" };
        db.RoutingRevs.Add(routingRev);

        var insType = new OpType { Code = "INS", Name = "INSPECTION" };
        var mlaType = new OpType { Code = "MLA", Name = "Medium Lathe" };
        db.OpTypes.AddRange(insType, mlaType);

        var op60 = new PartOp { RoutingRev = routingRev, OpNumber = "60", OpNumberSort = 60m, OpType = mlaType, IsVisible = true };
        var insOp = new PartOp { RoutingRev = routingRev, OpNumber = "110", OpNumberSort = 110m, OpType = insType, IsVisible = true };
        db.PartOps.AddRange(op60, insOp);

        var job = new Job { JobNumber = "JB-26-040", PartRev = partRev, RoutingRev = routingRev };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        // op60 (kỹ thuật): balloon "1" IsFinal=true (QC tái sử dụng), balloon "2" IsFinal=false
        // (chỉ kỹ thuật dùng, không liên quan QC Final), balloon "4" IsFinal=true (QC tái sử dụng,
        // không bị override bởi insOp).
        db.Dimensions.AddRange(
            new Dimension { PartOpId = op60.Id, BalloonNumber = "1", NominalValue = 50, TolerancePlus = 0.01m, ToleranceMinus = 0.01m, MaxValue = 50.01m, MinValue = 49.99m, Unit = "mm", IsFinal = true },
            new Dimension { PartOpId = op60.Id, BalloonNumber = "2", NominalValue = 10, TolerancePlus = 0.1m, ToleranceMinus = 0.1m, MaxValue = 10.1m, MinValue = 9.9m, Unit = "mm", IsFinal = false },
            new Dimension { PartOpId = op60.Id, BalloonNumber = "4", NominalValue = 30, TolerancePlus = 0.05m, ToleranceMinus = 0.05m, MaxValue = 30.05m, MinValue = 29.95m, Unit = "mm", IsFinal = true });

        // insOp (QC tự tạo): balloon "1" với dung sai khác (theo yêu cầu khách hàng) — phải override
        // bản balloon "1" của op60 dù bản đó IsFinal=true; balloon "3" — dimension chỉ QC kiểm tra,
        // không tồn tại ở OP nào khác trong routing.
        db.Dimensions.AddRange(
            new Dimension { PartOpId = insOp.Id, BalloonNumber = "1", NominalValue = 50, TolerancePlus = 0.03m, ToleranceMinus = 0.03m, MaxValue = 50.03m, MinValue = 49.97m, Unit = "mm" },
            new Dimension { PartOpId = insOp.Id, BalloonNumber = "3", NominalValue = 5, TolerancePlus = 0.02m, ToleranceMinus = 0.02m, MaxValue = 5.02m, MinValue = 4.98m, Unit = "mm" });
        await db.SaveChangesAsync();

        return (db, job, op60, insOp);
    }

    [Fact]
    public async Task Ins_op_returns_exactly_balloons_1_3_4_not_2()
    {
        var (db, job, _, insOp) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, insOp.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var balloons = result.Value.Dimensions.Select(d => d.BalloonNumber).OrderBy(b => b).ToList();
        Assert.Equal(new[] { "1", "3", "4" }, balloons);
    }

    [Fact]
    public async Task Ins_op_own_dimension_overrides_prior_final_dimension_with_same_balloon()
    {
        var (db, job, op60, insOp) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, insOp.Id), CancellationToken.None);

        var balloon1 = result.Value.Dimensions.Single(d => d.BalloonNumber == "1");
        Assert.Equal(insOp.Id, balloon1.PartOpId);
        Assert.Equal(50.03m, balloon1.MaxValue);
        Assert.Null(balloon1.OpNumber);
    }

    [Fact]
    public async Task Ins_op_includes_its_own_dimension_with_no_op_number_tag()
    {
        var (db, job, _, insOp) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, insOp.Id), CancellationToken.None);

        var balloon3 = result.Value.Dimensions.Single(d => d.BalloonNumber == "3");
        Assert.Equal(insOp.Id, balloon3.PartOpId);
        Assert.Null(balloon3.OpNumber);
    }

    [Fact]
    public async Task Ins_op_includes_prior_final_dimension_tagged_with_its_op_number()
    {
        var (db, job, op60, insOp) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, insOp.Id), CancellationToken.None);

        var balloon4 = result.Value.Dimensions.Single(d => d.BalloonNumber == "4");
        Assert.Equal(op60.Id, balloon4.PartOpId);
        Assert.Equal("60", balloon4.OpNumber);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test src/ShopfloorManager.Application.Tests --filter QcFinalDimensionAggregationTests
```
Expected: `Ins_op_returns_exactly_balloons_1_3_4_not_2` fails — current code returns 4 dimensions (balloons "1" from op60, "2", "4" — all of op60's dims, unfiltered by `IsFinal`) and never includes insOp's own dimensions "1"/"3", so balloon "1" comes back with `PartOpId == op60.Id` and `MaxValue == 50.01m`, not insOp's override.

- [ ] **Step 3: Fix the `Dimension.IsFinal` doc comment**

In `src/ShopfloorManager.Domain/Entities/Dimension.cs`, replace:
```csharp
    public bool IsFinal { get; set; }              // Kích thước kiểm tra lần cuối sau rework
```
with:
```csharp
    /// <summary>
    /// QC tái sử dụng dimension này (do kỹ thuật tạo) cho QC Final — không cần QC tạo riêng.
    /// Dimension nào QC cần dung sai khác bản vẽ kỹ thuật (ví dụ bám sát yêu cầu khách hàng) thì
    /// KHÔNG đánh dấu IsFinal — QC tự tạo dimension mới (cùng BalloonNumber) gán cho OP loại INS.
    /// </summary>
    public bool IsFinal { get; set; }
```

- [ ] **Step 4: Fix the dimension-gathering logic in `GetFaiSheetQueryHandler`**

In `src/ShopfloorManager.Application/Production/FaiCommands.cs`, replace the block (currently lines 70-97):
```csharp
        // Gom dimension từ nhiều OP — khi chọn "Tất cả OP" (gom toàn bộ routing) hoặc khi xem qua OP INS
        // (gom các OP có OpNumberSort nhỏ hơn OP INS đang xét — xem CLAUDE.md / 06_dimensions_fai.md §4.3)
        var tagOpNumber = allOps || isInspectionOp;
        List<Dimension> dims;
        if (allOps || isInspectionOp)
        {
            var routingOps = await db.PartOps
                .Where(p => (p.RoutingRevId == job.RoutingRevId && !p.ForJobOnly) || (p.ForJobOnly && p.JobId == job.Id))
                .ToListAsync(ct);
            decimal EffectiveSort(PartOp p) => p.OpNumberSort ?? 9999m;
            var scopedOpIds = allOps
                ? routingOps.Select(p => p.Id).ToList()
                : routingOps.Where(p => EffectiveSort(p) < EffectiveSort(op!)).Select(p => p.Id).ToList();

            dims = await db.Dimensions
                .Include(d => d.Category).Include(d => d.PartOp)
                .Where(d => scopedOpIds.Contains(d.PartOpId))
                .OrderBy(d => d.PartOp.OpNumberSort ?? 9999).ThenBy(d => d.BalloonSort ?? 9999).ThenBy(d => d.BalloonNumber)
                .ToListAsync(ct);
        }
        else
        {
            dims = await db.Dimensions
                .Include(d => d.Category)
                .Where(d => d.PartOpId == req.PartOpId)
                .OrderBy(d => d.BalloonSort ?? 9999).ThenBy(d => d.BalloonNumber)
                .ToListAsync(ct);
        }
```
with:
```csharp
        // Gom dimension: "Tất cả OP" (gom toàn bộ routing, KHÔNG filter IsFinal) hoặc OP INS
        // (= dimension riêng của chính OP INS — dung sai QC ∪ dimension IsFinal=true từ các OP
        // trước — dung sai kỹ thuật QC chấp nhận tái sử dụng; trùng BalloonNumber thì dimension
        // riêng của OP INS thắng). Xem CLAUDE.md / 06_dimensions_fai.md §4.3.
        var tagOpNumber = allOps || isInspectionOp;
        List<Dimension> dims;
        if (allOps)
        {
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

            var priorFinalDims = await db.Dimensions
                .Include(d => d.Category).Include(d => d.PartOp)
                .Where(d => priorOpIds.Contains(d.PartOpId) && d.IsFinal)
                .ToListAsync(ct);

            var ownDims = await db.Dimensions
                .Include(d => d.Category).Include(d => d.PartOp)
                .Where(d => d.PartOpId == op!.Id)
                .ToListAsync(ct);

            var ownBalloons = ownDims.Select(d => d.BalloonNumber).ToHashSet();
            dims = ownDims
                .Concat(priorFinalDims.Where(d => !ownBalloons.Contains(d.BalloonNumber)))
                .OrderBy(d => d.PartOp.OpNumberSort ?? 9999).ThenBy(d => d.BalloonSort ?? 9999).ThenBy(d => d.BalloonNumber)
                .ToList();
        }
        else
        {
            dims = await db.Dimensions
                .Include(d => d.Category)
                .Where(d => d.PartOpId == req.PartOpId)
                .OrderBy(d => d.BalloonSort ?? 9999).ThenBy(d => d.BalloonNumber)
                .ToListAsync(ct);
        }
```

- [ ] **Step 5: Fix the `OpNumber` tagging so an OP-INS's own dimensions are not tagged with their own OP number**

In the same file, replace (currently around line 126-131):
```csharp
        var dimDtos = dims.Select(d => new DimensionDto(
            d.Id, d.PartOpId, d.BalloonNumber, d.BalloonSort, d.Code, d.Description,
            d.NominalValue, d.TolerancePlus, d.ToleranceMinus, d.MaxValue, d.MinValue, d.Unit,
            d.IsTextType, d.NominalText, d.Category?.Code, d.IsCritical, d.IsFinal, d.SortOrder,
            OpNumber: tagOpNumber ? d.PartOp.OpNumber : null))
            .ToList();
```
with:
```csharp
        var dimDtos = dims.Select(d => new DimensionDto(
            d.Id, d.PartOpId, d.BalloonNumber, d.BalloonSort, d.Code, d.Description,
            d.NominalValue, d.TolerancePlus, d.ToleranceMinus, d.MaxValue, d.MinValue, d.Unit,
            d.IsTextType, d.NominalText, d.Category?.Code, d.IsCritical, d.IsFinal, d.SortOrder,
            OpNumber: tagOpNumber && d.PartOpId != (op?.Id ?? -1) ? d.PartOp.OpNumber : null))
            .ToList();
```
(For `allOps`, `op` is `null` so `op?.Id ?? -1` evaluates to `-1`, which never equals a real `PartOpId` — every dimension keeps its tag, matching the unchanged `allOps` behavior. For `isInspectionOp`, dimensions owned by the INS op itself get `OpNumber = null`; reused dimensions from prior ops keep their tag.)

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test src/ShopfloorManager.Application.Tests --filter QcFinalDimensionAggregationTests
```
Expected: `Passed! - Failed: 0, Passed: 4`

- [ ] **Step 7: Run the full Application test suite to confirm no regression**

```bash
dotnet test src/ShopfloorManager.Application.Tests
```
Expected: all tests pass, including the pre-existing `GetFaiSheetQueryHandlerTests` (which use `isInspectionOp` ops that have no `IsFinal` dimensions and no dimensions of their own — verify their assertions about dimension *count* still hold now that the query no longer includes non-`IsFinal` dimensions from prior ops in that fixture. If `Ins_op_aggregates_dimensions_from_prior_ops_and_tags_op_number` or `Second_ins_op_sees_the_same_aggregated_dimensions_as_the_first` fail because their seeded dimensions don't set `IsFinal = true`, this is expected — those dimensions are the OLD test fixture's "Engineering dims" that were never meant to model a QC-reuse flag; if they fail, that confirms this task's behavior change is real and working. If they fail, this is a **pre-existing test that encoded the old, wrong behavior** — update them in this same step by setting `IsFinal = true` on the two dimensions seeded in `GetFaiSheetQueryHandlerTests.SeedAsync` (`src/ShopfloorManager.Application.Tests/GetFaiSheetQueryHandlerTests.cs:42-43`), since those tests exist to verify "OP INS aggregates from prior ops," which now requires `IsFinal=true` to reuse.)

- [ ] **Step 8: Build the whole solution**

```bash
dotnet build src/ShopfloorManager.sln
```
Expected: `Build succeeded.`

- [ ] **Step 9: Commit**

```bash
git add src/ShopfloorManager.Domain/Entities/Dimension.cs src/ShopfloorManager.Application/Production/FaiCommands.cs src/ShopfloorManager.Application.Tests/QcFinalDimensionAggregationTests.cs src/ShopfloorManager.Application.Tests/GetFaiSheetQueryHandlerTests.cs
git commit -m "fix(fai): QC Final aggregates IsFinal dims + own OP-INS dims, not all prior dims"
```

---

### Task 2: Desktop — remove Final-specific special-casing from `FaiViewModel`

**Files:**
- Modify: `src/ShopfloorManager.Desktop/ViewModels/FaiViewModel.cs`

**Interfaces:**
- Consumes: `FaiCellDto.ByStage` (Task 1's corrected `GetFaiSheetQueryHandler` output, unchanged shape).
- Produces: `FaiViewModel.Mode`, `IsInputLocked`, `LoadAsync`, `SaveAsync` — same public signatures as before this task; only internal behavior changes. Consumed by Task 3 (`DashboardViewModel`/`MainViewModel` already call these correctly; no signature changes needed there).

- [ ] **Step 1: Update the `Mode` doc comment**

Replace:
```csharp
    /// <summary>Basic = Operator (InprocessFAI). Final = re-inspect sau rework (QCFinal), chỉ Fail dims.
    /// QcInline = QC Inspector kiểm ngẫu nhiên (QCInline), không bắt buộc đo hết.</summary>
    public FaiMode Mode { get; set; } = FaiMode.Basic;
```
with:
```csharp
    /// <summary>Basic = Operator (InprocessFAI), đo theo thứ tự, khóa sau khi đo.
    /// Final = QC Final (QCFinal) — kiểm tra độc lập 100% dimension, KHÔNG đọc/tham chiếu kết quả
    /// InprocessFAI hay QCInline ("blind inspection"), khóa sau khi đo, giống hành vi của Basic.
    /// QcInline = QC Inspector kiểm ngẫu nhiên (QCInline), không bắt buộc đo hết, không tự chọn dimension tiếp theo.</summary>
    public FaiMode Mode { get; set; } = FaiMode.Basic;
```

- [ ] **Step 2: Simplify `IsInputLocked` to one rule for all three modes**

Replace:
```csharp
    // Basic: lock sau bất kỳ lần đo nào (ở stage InprocessFAI).
    // Final: chỉ lock khi Pass ở stage QCFinal (Fail dims vẫn cho phép đo lại).
    // QcInline: lock nếu đã có record ở stage QCInline cho dim này (không ép tuần tự/đo hết).
    public bool IsInputLocked    => Mode switch
    {
        FaiMode.Final => SelectedDimension?.State == MeasureState.Pass,
        _             => SelectedDimension?.IsMeasured == true,
    };
```
with:
```csharp
    // Một rule chung cho cả 3 mode: khóa sau khi đã đo (bất kể Pass/Fail) — mỗi mode chỉ đọc/ghi
    // đúng MeasureStage của riêng nó (xem ToServerStage), không có ngoại lệ.
    public bool IsInputLocked    => SelectedDimension?.IsMeasured == true;
```

- [ ] **Step 3: Simplify the per-dimension stage lookup in `LoadAsync` (remove the Final fallback)**

Replace:
```csharp
            foreach (var dim in resp.Data.Dimensions ?? [])
            {
                var cell = row?.Cells?.FirstOrDefault(c => c.BalloonNumber == dim.BalloonNumber);
                // Đọc giá trị riêng của stage hiện tại — KHÔNG dùng cell.Result/Value (đó là "mới nhất
                // qua mọi stage", có thể lẫn dữ liệu của stage khác cho cùng dimension/product).
                // FAI Final là ngoại lệ: cần đọc Fail từ InprocessFAI (Operator) để biết cần re-inspect gì,
                // nhưng ưu tiên QCFinal nếu QC đã đo lại — ghi luôn vào QCFinal (xem SaveAsync).
                var stageCell = Mode == FaiMode.Final
                    ? cell?.ByStage?.GetValueOrDefault(2) ?? cell?.ByStage?.GetValueOrDefault(0)
                    : cell?.ByStage?.GetValueOrDefault(ToServerStage(Mode));
```
with:
```csharp
            var stageKey = ToServerStage(Mode);
            foreach (var dim in resp.Data.Dimensions ?? [])
            {
                var cell = row?.Cells?.FirstOrDefault(c => c.BalloonNumber == dim.BalloonNumber);
                // Đọc giá trị riêng của stage hiện tại — KHÔNG dùng cell.Result/Value (đó là "mới nhất
                // qua mọi stage", có thể lẫn dữ liệu của stage khác cho cùng dimension/product). QC Final
                // là "blind inspection" — không đọc/tham chiếu InprocessFAI hay QCInline.
                var stageCell = cell?.ByStage?.GetValueOrDefault(stageKey);
```

- [ ] **Step 4: Remove the Fail-only filter and fix the empty-state message/initial selection**

Replace:
```csharp
            // FAI Final: chỉ giữ lại dims có trạng thái Fail để re-inspect
            if (Mode == FaiMode.Final)
            {
                var failDims = Dimensions.Where(d => d.State == MeasureState.Fail).ToList();
                Dimensions.Clear();
                foreach (var d in failDims) Dimensions.Add(d);
            }

            RefreshProgress();

            if (!Dimensions.Any())
                ErrorMessage = Mode == FaiMode.Final
                    ? "Không có kích thước nào ở trạng thái FAIL để re-inspect."
                    : "OP này chưa có kích thước nào được định nghĩa.";
            else
                SelectedDimension = Dimensions.FirstOrDefault(d =>
                    Mode == FaiMode.Final ? d.State == MeasureState.Fail : !d.IsMeasured);
```
with:
```csharp
            RefreshProgress();

            if (!Dimensions.Any())
                ErrorMessage = "OP này chưa có kích thước nào được định nghĩa.";
            else
                SelectedDimension = Mode == FaiMode.QcInline
                    ? null
                    : Dimensions.FirstOrDefault(d => !d.IsMeasured);
```

- [ ] **Step 5: Fix the post-save dimension selection in `SaveAsync`**

Replace:
```csharp
            InputValue        = "";
            SelectedDimension = Mode switch
            {
                FaiMode.Final    => Dimensions.FirstOrDefault(d => d.State == MeasureState.Fail),
                FaiMode.QcInline => null, // QC tự chọn balloon tiếp theo muốn kiểm, không auto-advance
                _                => Dimensions.FirstOrDefault(d => !d.IsMeasured),
            };
            RefreshProgress();
```
with:
```csharp
            InputValue        = "";
            SelectedDimension = Mode == FaiMode.QcInline
                ? null  // QC tự chọn balloon tiếp theo muốn kiểm, không auto-advance
                : Dimensions.FirstOrDefault(d => !d.IsMeasured);  // Basic & Final: auto-advance
            RefreshProgress();
```

- [ ] **Step 6: Build**

```bash
dotnet build src/ShopfloorManager.Desktop
```
Expected: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
git add src/ShopfloorManager.Desktop/ViewModels/FaiViewModel.cs
git commit -m "fix(desktop): QC Final is a blind 100% inspection, not a Fail re-inspect filter"
```

---

### Task 3: Desktop — `OpTypeCode` on `PartOpDto` + dual-condition QC Final shortcut

**Files:**
- Modify: `src/ShopfloorManager.Desktop/Models/PartOpDto.cs`
- Modify: `src/ShopfloorManager.Desktop/ViewModels/LoginViewModel.cs:111-116`
- Modify: `src/ShopfloorManager.Desktop/ViewModels/DashboardViewModel.cs:479-485`

**Interfaces:**
- Consumes: server's `opTypeCode` JSON field (already returned by `/api/v1/jobs/{id}/operations` and other PartOp-listing endpoints — confirmed present in the API response, just not declared on the Desktop DTO yet).
- Produces: `PartOpDto.OpTypeCode` (string?) consumed by `DashboardViewModel.RefreshShortcuts`.

- [ ] **Step 1: Add `OpTypeCode` to the Desktop `PartOpDto` record**

In `src/ShopfloorManager.Desktop/Models/PartOpDto.cs`, replace:
```csharp
public record PartOpDto(
    int Id,
    int? RoutingRevId,
    int? JobId,
    bool ForJobOnly,
    string OpNumber,
    decimal? OpNumberSort,
    int? OpTypeId,
    string? OpTypeName,
    string? Description,
    string? Note,
    decimal? SetupTime,
    decimal? ProdTime,
    bool IsVisible,
    bool IsComplete)
```
with:
```csharp
public record PartOpDto(
    int Id,
    int? RoutingRevId,
    int? JobId,
    bool ForJobOnly,
    string OpNumber,
    decimal? OpNumberSort,
    int? OpTypeId,
    string? OpTypeName,
    string? OpTypeCode,
    string? Description,
    string? Note,
    decimal? SetupTime,
    decimal? ProdTime,
    bool IsVisible,
    bool IsComplete)
```

- [ ] **Step 2: Update the one manual construction site**

In `src/ShopfloorManager.Desktop/ViewModels/LoginViewModel.cs`, replace:
```csharp
        var op = new PartOpDto(
            active.PartOpId, RoutingRevId: null, JobId: null,
            ForJobOnly: false, active.OpNumber, OpNumberSort: null,
            OpTypeId: null, OpTypeName: null, Description: null,
            Note: null, SetupTime: null, ProdTime: null,
            IsVisible: true, IsComplete: false);
```
with:
```csharp
        var op = new PartOpDto(
            active.PartOpId, RoutingRevId: null, JobId: null,
            ForJobOnly: false, active.OpNumber, OpNumberSort: null,
            OpTypeId: null, OpTypeName: null, OpTypeCode: null, Description: null,
            Note: null, SetupTime: null, ProdTime: null,
            IsVisible: true, IsComplete: false);
```

- [ ] **Step 3: Require both conditions for the "FAI Final" (QC Final) shortcut**

In `src/ShopfloorManager.Desktop/ViewModels/DashboardViewModel.cs`, replace:
```csharp
        bool canQcInline = !_work.IsViewMode && hasProd
            && _work.CurrentProduct?.StatusCode == "complete";
        if (role is "QC Inspector" or "Administrator")
        {
            Add("FAI Final",  "ClipboardCheckOutline","fai-final",  when: canFai);
            Add("QC Inline",  "Magnify",               "qc-inline", when: canQcInline);
        }
```
with:
```csharp
        bool canQcInline = !_work.IsViewMode && hasProd
            && _work.CurrentProduct?.StatusCode == "complete";
        bool canQcFinal = !_work.IsViewMode && hasProd
            && _work.CurrentOp?.OpTypeCode == "INS"
            && _work.CurrentProduct?.StatusCode == "complete";
        if (role is "QC Inspector" or "Administrator")
        {
            Add("FAI Final",  "ClipboardCheckOutline","fai-final",  when: canQcFinal);
            Add("QC Inline",  "Magnify",               "qc-inline", when: canQcInline);
        }
```

- [ ] **Step 4: Build**

```bash
dotnet build src/ShopfloorManager.Desktop
```
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add src/ShopfloorManager.Desktop/Models/PartOpDto.cs src/ShopfloorManager.Desktop/ViewModels/LoginViewModel.cs src/ShopfloorManager.Desktop/ViewModels/DashboardViewModel.cs
git commit -m "fix(desktop): QC Final shortcut requires OP=INS AND product complete"
```

---

### Task 4: Manual end-to-end verification

**Files:** none (verification only).

- [ ] **Step 1: Start the full stack**

```bash
docker compose -f docker-compose.dev.yml up -d
dotnet run --project src/ShopfloorManager.API
```
Run the Desktop app (close any already-running instance first — the build will fail with a file-lock error otherwise):
```bash
dotnet run --project src/ShopfloorManager.Desktop
```

- [ ] **Step 2: Verify the shortcut condition**

Pick a Job whose routing has an OP of type "INS" later in the sequence and at least one earlier OP with an `IsFinal=true` dimension (use the seed/test data set up in Task 1's pattern, or create one via the Web app's Master Data / Dimsheet pages if no such Job exists in the dev DB). Log in as QC Inspector or Administrator.
- Select that Job, select a **non-INS** OP, select a product whose session is "complete" → confirm "FAI Final" shortcut does **NOT** appear.
- Select the **INS** OP instead, with a product whose session is **not yet complete** → confirm "FAI Final" still does **NOT** appear.
- Select the INS OP with a **complete** product → confirm "FAI Final" **DOES** appear.

- [ ] **Step 3: Verify blind, 100%, dimension aggregation**

Open "FAI Final" on that INS OP/product. Confirm:
- All dimensions shown are either (a) ones with `IsFinal=true` from a prior OP, tagged with that OP's number, or (b) ones created directly on the INS OP itself, untagged.
- No dimension with `IsFinal=false` from a prior OP appears.
- Measuring a dimension here does not show/lock based on any InprocessFAI or QCInline value already entered for that dimension/product (open "Bảng đo" on the earlier OP first, measure something, then come back to FAI Final and confirm it shows that dimension as unmeasured if it's not also part of FAI Final's own dimension set, or — if it is part of the set via `IsFinal` — shows it unmeasured until QC enters a value under the QCFinal stage specifically).
- After measuring all dimensions, confirm lock-after-measure behavior matches FAI Basic (auto-advances to the next unmeasured one, locks each entered dimension regardless of Pass/Fail).

- [ ] **Step 4: Confirm no regression in FAI Basic / QC Inline**

Repeat the standard FAI Basic flow (auto-advance, lock after one entry) on a non-INS OP, and the QC Inline flow (free selection, rate banner, no auto-advance) — confirm both behave exactly as they did before this fix.

- [ ] **Step 5: Report results**

No commit for this task — verification only. If any step fails, return to the relevant task and fix before proceeding.

---

## Self-Review Notes

- **Spec coverage:** `IsFinal` comment fix (Task 1 Step 3), OP-INS aggregation merge logic (Task 1 Step 4-5 + tests), removal of Fail-only filter / blind-inspection read-write (Task 2), dual-condition shortcut (Task 3) — all four spec items covered.
- **Placeholder scan:** none found — every step shows complete code or exact commands.
- **Type consistency:** `FaiMode`, `ToServerStage`, `IsInputLocked`, `LoadAsync`, `SaveAsync` names match exactly what exists in the current `FaiViewModel.cs` (verified by reading the file before writing this plan) — no renames introduced. `PartOpDto.OpTypeCode` matches the server JSON field name `opTypeCode` used elsewhere in this codebase (case-insensitive deserialization already configured in `ApiClient.cs`).
