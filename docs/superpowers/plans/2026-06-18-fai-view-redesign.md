# FAI View Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign `/fai` and `/jobs/[id]/fai` (job-list panel, card-based filter/info/stats, balloon-circle matrix with floating tooltip) and fix the OP INS business rule (inspection OPs must show dimensions aggregated from prior OPs, not their own).

**Architecture:** Backend: extend 3 existing MediatR query handlers (`GetJobsQueryHandler`, `GetJobOpsQueryHandler`, `GetFaiSheetQueryHandler`) with new optional DTO fields and an "OP INS aggregation" branch, covered by a new xUnit test project using EF Core InMemory. Frontend: two new components (`FaiJobList`, `FaiOpSelect`), a full rewrite of `FaiMatrix`, and a layout change to `/fai/page.tsx`.

**Tech Stack:** ASP.NET Core 9 / EF Core 9 / MediatR / FluentResults (backend), Next.js 16 + TypeScript + inline VA design tokens (frontend), xUnit + Microsoft.EntityFrameworkCore.InMemory (new test project).

## Global Constraints

- Spec source of truth: `docs/superpowers/specs/2026-06-18-fai-view-redesign-design.md` — read it before starting if any task is unclear.
- `OpType.Code` match for "OP INS" is **exact string `"INS"`, case-insensitive** (`StringComparison.OrdinalIgnoreCase`). No other code values count.
- "OPs before this OP INS" = PartOps in the same Job's routing (`RoutingRevId` template + `ForJobOnly` for this Job) whose effective sort (`OpNumberSort ?? 9999m`) is strictly less than the OP INS's effective sort.
- All new DTO fields are **optional with default values added at the end of existing positional records** — never reorder or remove existing fields, never make a new field required, to avoid breaking other call sites.
- No EF Core migration in this plan — every change is DTO/query logic only.
- No i18n work — `/fai` routes are not translated today (per CLAUDE.md) and this plan does not change that.
- Backend handlers changed in this plan must have failing-then-passing xUnit tests before being considered done (test project created in Task 1).

---

## Task 1: Create the Application test project + shared test DB factory

**Files:**
- Create: `src/ShopfloorManager.Application.Tests/ShopfloorManager.Application.Tests.csproj`
- Create: `src/ShopfloorManager.Application.Tests/TestDbContextFactory.cs`
- Create: `src/ShopfloorManager.Application.Tests/SmokeTests.cs`
- Modify: `src/ShopfloorManager.sln`

**Interfaces:**
- Produces: `TestDbContextFactory.Create()` → returns a fresh `ShopfloorDbContext` backed by a uniquely-named EF Core InMemory database (one instance per call, fully isolated). Used by every later test task.

- [ ] **Step 1: Create the test project file**

```xml
<!-- src/ShopfloorManager.Application.Tests/ShopfloorManager.Application.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.5" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ShopfloorManager.Application\ShopfloorManager.Application.csproj" />
    <ProjectReference Include="..\ShopfloorManager.Infrastructure\ShopfloorManager.Infrastructure.csproj" />
    <ProjectReference Include="..\ShopfloorManager.Domain\ShopfloorManager.Domain.csproj" />
    <ProjectReference Include="..\ShopfloorManager.Shared\ShopfloorManager.Shared.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add the project to the solution**

Run: `cd src && dotnet sln ShopfloorManager.sln add ShopfloorManager.Application.Tests/ShopfloorManager.Application.Tests.csproj`
Expected: `Project ... added to the solution.`

- [ ] **Step 3: Write the shared test DB factory**

```csharp
// src/ShopfloorManager.Application.Tests/TestDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Infrastructure.Data;

namespace ShopfloorManager.Application.Tests;

public static class TestDbContextFactory
{
    public static ShopfloorDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ShopfloorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ShopfloorDbContext(options);
    }
}
```

- [ ] **Step 4: Write a smoke test**

```csharp
// src/ShopfloorManager.Application.Tests/SmokeTests.cs
using ShopfloorManager.Domain.Entities;
using Xunit;

namespace ShopfloorManager.Application.Tests;

public class SmokeTests
{
    [Fact]
    public async Task Can_insert_and_query_a_part()
    {
        using var db = TestDbContextFactory.Create();
        db.Parts.Add(new Part { PartNumber = "SHAFT-50H6", Description = "Trục dẫn động" });
        await db.SaveChangesAsync();

        var part = db.Parts.Single(p => p.PartNumber == "SHAFT-50H6");

        Assert.Equal("Trục dẫn động", part.Description);
    }
}
```

Add `using System.Linq;` is unnecessary (`ImplicitUsings` enabled covers it via `Microsoft.NET.Sdk` default global usings, which include `System.Linq`).

- [ ] **Step 5: Run the test**

Run: `cd src && dotnet test ShopfloorManager.Application.Tests/ShopfloorManager.Application.Tests.csproj`
Expected: `Passed!` with 1 test passed.

- [ ] **Step 6: Commit**

```bash
git add src/ShopfloorManager.Application.Tests src/ShopfloorManager.sln
git commit -m "test: add Application.Tests project with EF Core InMemory smoke test"
```

---

## Task 2: `OpenNcrCount` on `JobDto` (used by the new job-list panel)

**Files:**
- Modify: `src/ShopfloorManager.Application/Production/JobCommands.cs:12-17` (`JobDto` record), `:66-94` (`GetJobsQueryHandler`)
- Test: `src/ShopfloorManager.Application.Tests/GetJobsQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `TestDbContextFactory.Create()` (Task 1).
- Produces: `JobDto.OpenNcrCount` (int, default `0`) — consumed by frontend Task 5 (`api-client.ts` `JobDto` type) and Task 6 (`FaiJobList`).

- [ ] **Step 1: Write the failing test**

```csharp
// src/ShopfloorManager.Application.Tests/GetJobsQueryHandlerTests.cs
using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;
using Xunit;

namespace ShopfloorManager.Application.Tests;

public class GetJobsQueryHandlerTests
{
    private static async Task<(ShopfloorDbContext Db, Job JobWithNcrs, Job JobWithoutNcrs)> SeedAsync()
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

        var jobWithNcrs = new Job { JobNumber = "JB-26-001", PartRev = partRev, RoutingRev = routingRev };
        var jobWithoutNcrs = new Job { JobNumber = "JB-26-002", PartRev = partRev, RoutingRev = routingRev };
        db.Jobs.AddRange(jobWithNcrs, jobWithoutNcrs);
        await db.SaveChangesAsync();

        db.Ncrs.AddRange(
            new Ncr { JobId = jobWithNcrs.Id, Description = "Fail OD", Status = NcrStatus.Open, RaisedBy = 1 },
            new Ncr { JobId = jobWithNcrs.Id, Description = "Fail length", Status = NcrStatus.Open, RaisedBy = 1 },
            new Ncr { JobId = jobWithNcrs.Id, Description = "Closed one", Status = NcrStatus.Closed, RaisedBy = 1 });
        await db.SaveChangesAsync();

        return (db, jobWithNcrs, jobWithoutNcrs);
    }

    [Fact]
    public async Task Counts_only_open_ncrs_per_job()
    {
        var (db, jobWithNcrs, jobWithoutNcrs) = await SeedAsync();
        var handler = new GetJobsQueryHandler(db);

        var result = await handler.Handle(new GetJobsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dtoWithNcrs = result.Value.Items.Single(j => j.Id == jobWithNcrs.Id);
        var dtoWithoutNcrs = result.Value.Items.Single(j => j.Id == jobWithoutNcrs.Id);
        Assert.Equal(2, dtoWithNcrs.OpenNcrCount);
        Assert.Equal(0, dtoWithoutNcrs.OpenNcrCount);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src && dotnet test ShopfloorManager.Application.Tests/ShopfloorManager.Application.Tests.csproj --filter GetJobsQueryHandlerTests`
Expected: FAIL — compile error `'JobDto' does not contain a definition for 'OpenNcrCount'`.

- [ ] **Step 3: Add `OpenNcrCount` to `JobDto`**

In `src/ShopfloorManager.Application/Production/JobCommands.cs`, change:

```csharp
public record JobDto(
    int Id, string JobNumber,
    int PartId, int PartRevId, string PartNumber, string RevCode,
    int RoutingRevId, string RoutingRevCode,
    int? RunQty, int CompletedCount, DateOnly? ShipBy, bool IsComplete,
    DateTimeOffset CreatedAt);
```

to:

```csharp
public record JobDto(
    int Id, string JobNumber,
    int PartId, int PartRevId, string PartNumber, string RevCode,
    int RoutingRevId, string RoutingRevCode,
    int? RunQty, int CompletedCount, DateOnly? ShipBy, bool IsComplete,
    DateTimeOffset CreatedAt,
    int OpenNcrCount = 0);
```

- [ ] **Step 4: Compute `OpenNcrCount` in `GetJobsQueryHandler`**

Replace the handler body (`src/ShopfloorManager.Application/Production/JobCommands.cs:69-93`):

```csharp
public async Task<Result<PagedResult<JobDto>>> Handle(GetJobsQuery req, CancellationToken ct)
{
    var q = db.Jobs
        .Include(j => j.PartRev).ThenInclude(r => r.Part)
        .Include(j => j.RoutingRev)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(req.Search))
        q = q.Where(j => j.JobNumber.Contains(req.Search)
                       || j.PartRev.Part.PartNumber.Contains(req.Search));
    if (req.PartRevId.HasValue)
        q = q.Where(j => j.PartRevId == req.PartRevId.Value);

    var total = await q.CountAsync(ct);
    var page = await q.OrderByDescending(j => j.CreatedAt)
        .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
        .Select(j => new
        {
            j.Id, j.JobNumber,
            j.PartRev.PartId, j.PartRevId,
            PartNumber = j.PartRev.Part.PartNumber, RevCode = j.PartRev.RevCode,
            j.RoutingRevId, RoutingRevCode = j.RoutingRev.RevCode,
            j.RunQty, CompletedCount = j.Products.Count(p => p.IsComplete),
            j.ShipBy, j.IsComplete, j.CreatedAt,
        })
        .ToListAsync(ct);

    var jobIds = page.Select(x => x.Id).ToList();
    var openNcrCounts = await db.Ncrs
        .Where(n => jobIds.Contains(n.JobId) && n.Status == NcrStatus.Open)
        .GroupBy(n => n.JobId)
        .Select(g => new { JobId = g.Key, Count = g.Count() })
        .ToListAsync(ct);
    var ncrCountMap = openNcrCounts.ToDictionary(x => x.JobId, x => x.Count);

    var items = page.Select(x => new JobDto(x.Id, x.JobNumber,
            x.PartId, x.PartRevId, x.PartNumber, x.RevCode,
            x.RoutingRevId, x.RoutingRevCode,
            x.RunQty, x.CompletedCount, x.ShipBy, x.IsComplete, x.CreatedAt,
            ncrCountMap.GetValueOrDefault(x.Id, 0)))
        .ToList();

    return Result.Ok(new PagedResult<JobDto>(items, req.Page, req.PageSize, total));
}
```

`NcrStatus` is already in scope via the existing `using ShopfloorManager.Domain.Enums;` at the top of `JobCommands.cs`.

- [ ] **Step 5: Run test to verify it passes**

Run: `cd src && dotnet test ShopfloorManager.Application.Tests/ShopfloorManager.Application.Tests.csproj --filter GetJobsQueryHandlerTests`
Expected: `Passed!` with 1 test passed.

- [ ] **Step 6: Confirm the other `JobDto` call site still compiles**

Run: `cd src && dotnet build ShopfloorManager.sln`
Expected: `Build succeeded.` (The `new JobDto(...)` call in `CreateJobCommandHandler` at `JobCommands.cs:241` omits the new trailing optional parameter, so it defaults to `0` — correct for a freshly created job.)

- [ ] **Step 7: Commit**

```bash
git add src/ShopfloorManager.Application/Production/JobCommands.cs src/ShopfloorManager.Application.Tests/GetJobsQueryHandlerTests.cs
git commit -m "feat(fai): add OpenNcrCount to JobDto for the FAI job-list panel"
```

---

## Task 3: `OpTypeCode` + real `DimCount` + OP INS aggregation on `GetJobOpsQueryHandler`

**Files:**
- Modify: `src/ShopfloorManager.Application/Production/PartOpCommands.cs:10-17` (`PartOpDto` record), `:27-56` (`GetJobOpsQueryHandler`)
- Test: `src/ShopfloorManager.Application.Tests/GetJobOpsQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `TestDbContextFactory.Create()` (Task 1).
- Produces: `PartOpDto.OpTypeCode` (string?, default `null`), `PartOpDto.DimCount` now real (previously hardcoded `0`) — consumed by frontend Task 5/7 (`FaiOpSelect`).

- [ ] **Step 1: Write the failing test**

```csharp
// src/ShopfloorManager.Application.Tests/GetJobOpsQueryHandlerTests.cs
using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Entities;
using Xunit;

namespace ShopfloorManager.Application.Tests;

public class GetJobOpsQueryHandlerTests
{
    // Mirrors the real legacy routing: ...OP90 -> OP100 STP -> OP110 INS -> OP120 PPG -> OP130 INS
    private static async Task<(ShopfloorDbContext Db, Job Job, PartOp Op60, PartOp Op110Ins, PartOp Op120, PartOp Op130Ins)> SeedRoutingAsync()
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
        var ppgType = new OpType { Code = "PPG", Name = "PHOSPHATING" };
        db.OpTypes.AddRange(insType, mlaType, ppgType);

        var op60 = new PartOp { RoutingRev = routingRev, OpNumber = "60", OpNumberSort = 60m, OpType = mlaType, IsVisible = true };
        var op100 = new PartOp { RoutingRev = routingRev, OpNumber = "100", OpNumberSort = 100m, OpType = mlaType, IsVisible = true };
        var op110Ins = new PartOp { RoutingRev = routingRev, OpNumber = "110", OpNumberSort = 110m, OpType = insType, IsVisible = true };
        var op120 = new PartOp { RoutingRev = routingRev, OpNumber = "120", OpNumberSort = 120m, OpType = ppgType, IsVisible = true };
        var op130Ins = new PartOp { RoutingRev = routingRev, OpNumber = "130", OpNumberSort = 130m, OpType = insType, IsVisible = true };
        db.PartOps.AddRange(op60, op100, op110Ins, op120, op130Ins);

        var job = new Job { JobNumber = "JB-26-031", PartRev = partRev, RoutingRev = routingRev };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        db.Dimensions.AddRange(
            new Dimension { PartOpId = op60.Id, BalloonNumber = "1", NominalValue = 50, TolerancePlus = 0.02m, ToleranceMinus = 0.02m, MaxValue = 50.02m, MinValue = 49.98m, Unit = "mm" },
            new Dimension { PartOpId = op60.Id, BalloonNumber = "2", NominalValue = 80, TolerancePlus = 0.1m, ToleranceMinus = 0.1m, MaxValue = 80.1m, MinValue = 79.9m, Unit = "mm" });
        await db.SaveChangesAsync();

        return (db, job, op60, op110Ins, op120, op130Ins);
    }

    [Fact]
    public async Task Normal_op_reports_its_own_dimension_count_and_op_type_code()
    {
        var (db, job, op60, _, _, _) = await SeedRoutingAsync();
        var handler = new GetJobOpsQueryHandler(db);

        var result = await handler.Handle(new GetJobOpsQuery(job.Id), CancellationToken.None);

        var dto = result.Value.Single(o => o.Id == op60.Id);
        Assert.Equal(2, dto.DimCount);
        Assert.Equal("MLA", dto.OpTypeCode);
    }

    [Fact]
    public async Task Ins_op_aggregates_dimension_count_from_prior_ops_only()
    {
        var (db, job, _, op110Ins, op120, _) = await SeedRoutingAsync();
        var handler = new GetJobOpsQueryHandler(db);

        var result = await handler.Handle(new GetJobOpsQuery(job.Id), CancellationToken.None);

        var op110Dto = result.Value.Single(o => o.Id == op110Ins.Id);
        var op120Dto = result.Value.Single(o => o.Id == op120.Id);
        Assert.Equal(2, op110Dto.DimCount);   // OP110 INS sees OP60's 2 dimensions
        Assert.Equal("INS", op110Dto.OpTypeCode);
        Assert.Equal(0, op120Dto.DimCount);   // OP120 PPG owns no dimension itself
    }

    [Fact]
    public async Task Second_ins_op_after_the_first_sees_the_same_aggregated_set()
    {
        var (db, job, _, _, _, op130Ins) = await SeedRoutingAsync();
        var handler = new GetJobOpsQueryHandler(db);

        var result = await handler.Handle(new GetJobOpsQuery(job.Id), CancellationToken.None);

        var op130Dto = result.Value.Single(o => o.Id == op130Ins.Id);
        Assert.Equal(2, op130Dto.DimCount);   // Still OP60's 2 dimensions — OP110/OP120 contribute none
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src && dotnet test ShopfloorManager.Application.Tests/ShopfloorManager.Application.Tests.csproj --filter GetJobOpsQueryHandlerTests`
Expected: FAIL — compile error `'PartOpDto' does not contain a definition for 'OpTypeCode'`.

- [ ] **Step 3: Add `OpTypeCode` to `PartOpDto`**

In `src/ShopfloorManager.Application/Production/PartOpCommands.cs`, change:

```csharp
public record PartOpDto(
    int Id, int? RoutingRevId, int? JobId, bool ForJobOnly,
    string OpNumber, decimal? OpNumberSort,
    int? OpTypeId, string? OpTypeName,
    string? Description, string? Note,
    decimal? SetupTime, decimal? ProdTime,
    bool IsVisible, bool IsComplete,
    int DimCount, int DocCount);
```

to:

```csharp
public record PartOpDto(
    int Id, int? RoutingRevId, int? JobId, bool ForJobOnly,
    string OpNumber, decimal? OpNumberSort,
    int? OpTypeId, string? OpTypeName,
    string? Description, string? Note,
    decimal? SetupTime, decimal? ProdTime,
    bool IsVisible, bool IsComplete,
    int DimCount, int DocCount,
    string? OpTypeCode = null);
```

- [ ] **Step 4: Implement the aggregation logic in `GetJobOpsQueryHandler`**

Replace the handler body (`src/ShopfloorManager.Application/Production/PartOpCommands.cs:30-55`):

```csharp
public async Task<Result<List<PartOpDto>>> Handle(GetJobOpsQuery req, CancellationToken ct)
{
    var job = await db.Jobs.FindAsync([req.JobId], ct);
    if (job is null) return Result.Fail($"Không tìm thấy Job ID {req.JobId}.");

    // Template OPs từ RoutingRev được snapshot trong Job
    var templateOps = await db.PartOps
        .Include(o => o.OpType)
        .Where(o => o.RoutingRevId == job.RoutingRevId && o.IsVisible)
        .ToListAsync(ct);

    // OPs riêng của Job này (ForJobOnly=true)
    var jobOps = await db.PartOps
        .Include(o => o.OpType)
        .Where(o => o.JobId == req.JobId && o.ForJobOnly && o.IsVisible)
        .ToListAsync(ct);

    var all = templateOps.Concat(jobOps).OrderBy(o => o.OpNumberSort ?? 0).ToList();
    var allIds = all.Select(o => o.Id).ToList();

    var dimCountsByOp = (await db.Dimensions
            .Where(d => allIds.Contains(d.PartOpId))
            .GroupBy(d => d.PartOpId)
            .Select(g => new { PartOpId = g.Key, Count = g.Count() })
            .ToListAsync(ct))
        .ToDictionary(x => x.PartOpId, x => x.Count);

    decimal EffectiveSort(PartOp p) => p.OpNumberSort ?? 9999m;

    var result = all.Select(o =>
    {
        var isInspectionOp = string.Equals(o.OpType?.Code, "INS", StringComparison.OrdinalIgnoreCase);
        int dimCount;
        if (isInspectionOp)
        {
            var priorOpIds = all.Where(p => EffectiveSort(p) < EffectiveSort(o)).Select(p => p.Id).ToHashSet();
            dimCount = dimCountsByOp.Where(kv => priorOpIds.Contains(kv.Key)).Sum(kv => kv.Value);
        }
        else
        {
            dimCount = dimCountsByOp.GetValueOrDefault(o.Id, 0);
        }

        return new PartOpDto(o.Id, o.RoutingRevId, o.JobId, o.ForJobOnly,
            o.OpNumber, o.OpNumberSort, o.OpTypeId, o.OpType?.Name,
            o.Description, o.Note, o.SetupTime, o.ProdTime, o.IsVisible, o.IsComplete,
            dimCount, 0, o.OpType?.Code);
    }).ToList();

    return Result.Ok(result);
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `cd src && dotnet test ShopfloorManager.Application.Tests/ShopfloorManager.Application.Tests.csproj --filter GetJobOpsQueryHandlerTests`
Expected: `Passed!` with 3 tests passed.

- [ ] **Step 6: Confirm full solution still builds**

Run: `cd src && dotnet build ShopfloorManager.sln`
Expected: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
git add src/ShopfloorManager.Application/Production/PartOpCommands.cs src/ShopfloorManager.Application.Tests/GetJobOpsQueryHandlerTests.cs
git commit -m "fix(fai): compute real DimCount and OpTypeCode, aggregate OP INS dimension count from prior ops"
```

---

## Task 4: `OpNumber` on `DimensionDto` + OP INS aggregation in `GetFaiSheetQueryHandler`

**Files:**
- Modify: `src/ShopfloorManager.Application/Production/FaiCommands.cs:13-21` (`DimensionDto` record), `:48-123` (`GetFaiSheetQueryHandler`)
- Test: `src/ShopfloorManager.Application.Tests/GetFaiSheetQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `TestDbContextFactory.Create()` (Task 1).
- Produces: `DimensionDto.OpNumber` (string?, default `null`, set only when the FAI sheet is for an OP INS) — consumed by frontend Task 8 (`FaiMatrix` balloon header label).

- [ ] **Step 1: Write the failing test**

```csharp
// src/ShopfloorManager.Application.Tests/GetFaiSheetQueryHandlerTests.cs
using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;
using Xunit;

namespace ShopfloorManager.Application.Tests;

public class GetFaiSheetQueryHandlerTests
{
    private static async Task<(ShopfloorDbContext Db, Job Job, PartOp Op60, PartOp Op110Ins, PartOp Op130Ins, Product Product)> SeedAsync()
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
        var ppgType = new OpType { Code = "PPG", Name = "PHOSPHATING" };
        db.OpTypes.AddRange(insType, mlaType, ppgType);

        var op60 = new PartOp { RoutingRev = routingRev, OpNumber = "60", OpNumberSort = 60m, OpType = mlaType, IsVisible = true };
        var op110Ins = new PartOp { RoutingRev = routingRev, OpNumber = "110", OpNumberSort = 110m, OpType = insType, IsVisible = true };
        var op120 = new PartOp { RoutingRev = routingRev, OpNumber = "120", OpNumberSort = 120m, OpType = ppgType, IsVisible = true };
        var op130Ins = new PartOp { RoutingRev = routingRev, OpNumber = "130", OpNumberSort = 130m, OpType = insType, IsVisible = true };
        db.PartOps.AddRange(op60, op110Ins, op120, op130Ins);

        var job = new Job { JobNumber = "JB-26-031", PartRev = partRev, RoutingRev = routingRev };
        db.Jobs.Add(job);
        var product = new Product { Job = job, SerialNumber = "001" };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        db.Dimensions.AddRange(
            new Dimension { PartOpId = op60.Id, BalloonNumber = "1", NominalValue = 50, TolerancePlus = 0.02m, ToleranceMinus = 0.02m, MaxValue = 50.02m, MinValue = 49.98m, Unit = "mm" },
            new Dimension { PartOpId = op60.Id, BalloonNumber = "2", NominalValue = 80, TolerancePlus = 0.1m, ToleranceMinus = 0.1m, MaxValue = 80.1m, MinValue = 79.9m, Unit = "mm" });
        await db.SaveChangesAsync();

        return (db, job, op60, op110Ins, op130Ins, product);
    }

    [Fact]
    public async Task Normal_op_returns_only_its_own_dimensions_with_null_op_number()
    {
        var (db, job, op60, _, _, _) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, op60.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Dimensions.Count);
        Assert.All(result.Value.Dimensions, d => Assert.Null(d.OpNumber));
    }

    [Fact]
    public async Task Ins_op_aggregates_dimensions_from_prior_ops_and_tags_op_number()
    {
        var (db, job, op60, op110Ins, _, _) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, op110Ins.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Dimensions.Count);
        Assert.All(result.Value.Dimensions, d => Assert.Equal("60", d.OpNumber));
    }

    [Fact]
    public async Task Second_ins_op_sees_the_same_aggregated_dimensions_as_the_first()
    {
        var (db, job, _, _, op130Ins, _) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, op130Ins.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Dimensions.Count);
        Assert.All(result.Value.Dimensions, d => Assert.Equal("60", d.OpNumber));
    }

    [Fact]
    public async Task Rows_have_one_cell_per_dimension_for_every_product()
    {
        var (db, job, op60, _, _, product) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, op60.Id), CancellationToken.None);

        var row = result.Value.Rows.Single(r => r.ProductId == product.Id);
        Assert.Equal(2, row.Cells.Count);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src && dotnet test ShopfloorManager.Application.Tests/ShopfloorManager.Application.Tests.csproj --filter GetFaiSheetQueryHandlerTests`
Expected: FAIL — compile error `'DimensionDto' does not contain a definition for 'OpNumber'`.

- [ ] **Step 3: Add `OpNumber` to `DimensionDto`**

In `src/ShopfloorManager.Application/Production/FaiCommands.cs`, change:

```csharp
public record DimensionDto(
    long Id, int PartOpId,
    string BalloonNumber, decimal? BalloonSort, string? Code, string? Description,
    decimal? NominalValue, decimal? TolerancePlus, decimal? ToleranceMinus,
    decimal? MaxValue, decimal? MinValue, string Unit,
    bool IsTextType, string? NominalText,
    string? CategoryCode, bool IsCritical, bool IsFinal, int SortOrder,
    // Approval workflow
    string Status = "Approved", int? ReviewedBy = null, DateTimeOffset? ReviewedAt = null, string? ReviewNote = null);
```

to:

```csharp
public record DimensionDto(
    long Id, int PartOpId,
    string BalloonNumber, decimal? BalloonSort, string? Code, string? Description,
    decimal? NominalValue, decimal? TolerancePlus, decimal? ToleranceMinus,
    decimal? MaxValue, decimal? MinValue, string Unit,
    bool IsTextType, string? NominalText,
    string? CategoryCode, bool IsCritical, bool IsFinal, int SortOrder,
    // Approval workflow
    string Status = "Approved", int? ReviewedBy = null, DateTimeOffset? ReviewedAt = null, string? ReviewNote = null,
    // OP gốc sở hữu dimension — chỉ set khi xem qua OP INS (xem GetFaiSheetQueryHandler)
    string? OpNumber = null);
```

- [ ] **Step 4: Implement the OP INS branch in `GetFaiSheetQueryHandler`**

Replace lines `src/ShopfloorManager.Application/Production/FaiCommands.cs:51-65` (the `op` lookup through the `dims` query) with:

```csharp
var op = await db.PartOps.Include(o => o.OpType).FirstOrDefaultAsync(o => o.Id == req.PartOpId, ct);
if (op is null) return Result.Fail("PartOp không tồn tại.");

var job = await db.Jobs
    .Include(j => j.PartRev).ThenInclude(pr => pr.Part)
    .FirstOrDefaultAsync(j => j.Id == req.JobId, ct);
if (job is null) return Result.Fail("Job không tồn tại.");

var isInspectionOp = string.Equals(op.OpType?.Code, "INS", StringComparison.OrdinalIgnoreCase);

List<Dimension> dims;
if (isInspectionOp)
{
    var routingOps = await db.PartOps
        .Where(p => (p.RoutingRevId == job.RoutingRevId && !p.ForJobOnly) || (p.ForJobOnly && p.JobId == job.Id))
        .ToListAsync(ct);
    decimal EffectiveSort(PartOp p) => p.OpNumberSort ?? 9999m;
    var priorOpIds = routingOps.Where(p => EffectiveSort(p) < EffectiveSort(op)).Select(p => p.Id).ToList();

    dims = await db.Dimensions
        .Include(d => d.Category).Include(d => d.PartOp)
        .Where(d => priorOpIds.Contains(d.PartOpId))
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

This removes the old standalone `op` lookup (`db.PartOps.FindAsync`) and the old standalone `dims` query — both are now part of the block above. Leave everything from `var products = await db.Products...` (`FaiCommands.cs:67` in the original file) onward untouched, **except** the `dimDtos` projection a few lines below, which must change next.

- [ ] **Step 5: Tag `OpNumber` in the `dimDtos` projection**

Replace (originally `FaiCommands.cs:94-98`):

```csharp
var dimDtos = dims.Select(d => new DimensionDto(
    d.Id, d.PartOpId, d.BalloonNumber, d.BalloonSort, d.Code, d.Description,
    d.NominalValue, d.TolerancePlus, d.ToleranceMinus, d.MaxValue, d.MinValue, d.Unit,
    d.IsTextType, d.NominalText, d.Category?.Code, d.IsCritical, d.IsFinal, d.SortOrder))
    .ToList();
```

with:

```csharp
var dimDtos = dims.Select(d => new DimensionDto(
    d.Id, d.PartOpId, d.BalloonNumber, d.BalloonSort, d.Code, d.Description,
    d.NominalValue, d.TolerancePlus, d.ToleranceMinus, d.MaxValue, d.MinValue, d.Unit,
    d.IsTextType, d.NominalText, d.Category?.Code, d.IsCritical, d.IsFinal, d.SortOrder,
    OpNumber: isInspectionOp ? d.PartOp.OpNumber : null))
    .ToList();
```

(`d.PartOp` is safe to dereference here only inside the `isInspectionOp` branch — the conditional operator short-circuits, and `d.PartOp` was `.Include()`d only in that branch's query. In the non-INS branch the operand is never evaluated.)

- [ ] **Step 6: Run tests to verify they pass**

Run: `cd src && dotnet test ShopfloorManager.Application.Tests/ShopfloorManager.Application.Tests.csproj --filter GetFaiSheetQueryHandlerTests`
Expected: `Passed!` with 4 tests passed.

- [ ] **Step 7: Run the full test suite + full build**

Run: `cd src && dotnet test ShopfloorManager.Application.Tests/ShopfloorManager.Application.Tests.csproj`
Expected: `Passed!` with 9 tests passed total (1 smoke + 1 jobs + 3 ops + 4 fai sheet).

Run: `cd src && dotnet build ShopfloorManager.sln`
Expected: `Build succeeded.`

- [ ] **Step 8: Commit**

```bash
git add src/ShopfloorManager.Application/Production/FaiCommands.cs src/ShopfloorManager.Application.Tests/GetFaiSheetQueryHandlerTests.cs
git commit -m "feat(fai): aggregate dimensions from prior ops when viewing an OP INS sheet"
```

---

## Task 5: Frontend `api-client.ts` type updates

**Files:**
- Modify: `clients/web/lib/api-client.ts:75-80` (`JobDto`), `:82-89` (`PartOpDto`), `:105-113` (`DimensionDto`)

**Interfaces:**
- Consumes: backend fields from Tasks 2–4 (`openNcrCount`, `opTypeCode`, `opNumber`).
- Produces: updated TS types consumed by Task 6 (`FaiJobList`), Task 7 (`FaiOpSelect`), Task 8 (`FaiMatrix`).

- [ ] **Step 1: Add `openNcrCount` to `JobDto`**

In `clients/web/lib/api-client.ts`, change:

```typescript
export type JobDto = {
  id: number; jobNumber: string
  partId: number; partRevId: number; partNumber: string; revCode: string
  routingRevId: number; routingRevCode: string
  runQty: number | null; completedCount: number; shipBy: string | null; isComplete: boolean; createdAt: string
}
```

to:

```typescript
export type JobDto = {
  id: number; jobNumber: string
  partId: number; partRevId: number; partNumber: string; revCode: string
  routingRevId: number; routingRevCode: string
  runQty: number | null; completedCount: number; shipBy: string | null; isComplete: boolean; createdAt: string
  openNcrCount: number
}
```

- [ ] **Step 2: Add `opTypeCode` to `PartOpDto`**

Change:

```typescript
export type PartOpDto = {
  id: number; routingRevId: number | null; jobId: number | null; forJobOnly: boolean
  opNumber: string; opNumberSort: number | null
  opTypeId: number | null; opTypeName: string | null
  description: string | null; note: string | null
  setupTime: number | null; prodTime: number | null; isVisible: boolean; isComplete: boolean
  dimCount: number; docCount: number
}
```

to:

```typescript
export type PartOpDto = {
  id: number; routingRevId: number | null; jobId: number | null; forJobOnly: boolean
  opNumber: string; opNumberSort: number | null
  opTypeId: number | null; opTypeName: string | null
  description: string | null; note: string | null
  setupTime: number | null; prodTime: number | null; isVisible: boolean; isComplete: boolean
  dimCount: number; docCount: number
  opTypeCode: string | null
}
```

- [ ] **Step 3: Add `opNumber` to `DimensionDto`**

Change:

```typescript
export type DimensionDto = {
  id: number; partOpId: number
  balloonNumber: string; balloonSort: number | null; code: string | null; description: string | null
  nominalValue: number | null; tolerancePlus: number | null; toleranceMinus: number | null
  maxValue: number | null; minValue: number | null; unit: string
  isTextType: boolean; nominalText: string | null
  categoryCode: string | null; isCritical: boolean; isFinal: boolean; sortOrder: number
  status: string; reviewedBy: number | null; reviewedAt: string | null; reviewNote: string | null
}
```

to:

```typescript
export type DimensionDto = {
  id: number; partOpId: number
  balloonNumber: string; balloonSort: number | null; code: string | null; description: string | null
  nominalValue: number | null; tolerancePlus: number | null; toleranceMinus: number | null
  maxValue: number | null; minValue: number | null; unit: string
  isTextType: boolean; nominalText: string | null
  categoryCode: string | null; isCritical: boolean; isFinal: boolean; sortOrder: number
  status: string; reviewedBy: number | null; reviewedAt: string | null; reviewNote: string | null
  opNumber: string | null
}
```

- [ ] **Step 4: Type-check the web app**

Run: `cd clients/web && npx tsc --noEmit`
Expected: no new errors (pre-existing unrelated errors, if any, are out of scope — compare against a baseline run before this task if unsure).

- [ ] **Step 5: Commit**

```bash
git add clients/web/lib/api-client.ts
git commit -m "feat(fai): add openNcrCount, opTypeCode, opNumber to FAI-related API types"
```

---

## Task 6: `FaiJobList` component (job-list panel for `/fai`)

**Files:**
- Create: `clients/web/components/fai/fai-job-list.tsx`

**Interfaces:**
- Consumes: `api.jobs.list(page, search)` → `JobDto[]` (existing, `lib/api-client.ts:245-246`), `JobDto.openNcrCount` (Task 5), `va` tokens (`lib/va-tokens.ts`), `VABadge` (`components/va`).
- Produces: `FaiJobList({ selectedJobId, onSelect }: { selectedJobId: number | null; onSelect: (jobId: number, job: JobDto) => void })` — default export named `FaiJobList`, used by Task 9 (`/fai/page.tsx`).

- [ ] **Step 1: Write the component**

```tsx
// clients/web/components/fai/fai-job-list.tsx
'use client'

import { useEffect, useState } from 'react'
import { api, type JobDto } from '@/lib/api-client'
import { VABadge } from '@/components/va'
import { va } from '@/lib/va-tokens'

type Props = {
  selectedJobId: number | null
  onSelect: (jobId: number, job: JobDto) => void
}

type JobStatus = 'on-track' | 'at-risk' | 'complete' | 'overdue'

function deriveStatus(job: JobDto): JobStatus {
  if (job.isComplete) return 'complete'
  if (!job.shipBy) return 'on-track'
  const today = new Date(); today.setHours(0, 0, 0, 0)
  const shipBy = new Date(job.shipBy); shipBy.setHours(0, 0, 0, 0)
  const daysLeft = Math.round((shipBy.getTime() - today.getTime()) / 86_400_000)
  if (daysLeft < 0) return 'overdue'
  if (daysLeft <= 3) return 'at-risk'
  return 'on-track'
}

const STATUS_BADGE: Record<JobStatus, { kind: 'ok' | 'warn' | 'err' | 'neutral'; label: string }> = {
  'on-track': { kind: 'ok', label: 'Đúng hạn' },
  'at-risk': { kind: 'warn', label: 'Rủi ro' },
  complete: { kind: 'neutral', label: 'Xong' },
  overdue: { kind: 'err', label: 'Trễ' },
}

function completion(job: JobDto): number {
  if (!job.runQty || job.runQty <= 0) return job.completedCount > 0 ? 100 : 0
  return Math.round((job.completedCount / job.runQty) * 100)
}

export function FaiJobList({ selectedJobId, onSelect }: Props) {
  const [search, setSearch] = useState('')
  const [jobs, setJobs] = useState<JobDto[]>([])

  useEffect(() => {
    const t = setTimeout(() => {
      api.jobs.list(1, search || undefined).then(res => {
        if (res.success && res.data) setJobs(res.data)
      })
    }, 250)
    return () => clearTimeout(t)
  }, [search])

  return (
    <div style={{ width: 268, flexShrink: 0, background: va.surface, borderRight: `1px solid ${va.border}`, display: 'flex', flexDirection: 'column', height: '100%' }}>
      <div style={{ padding: '15px 16px 12px', borderBottom: `1px solid ${va.separator}`, flexShrink: 0 }}>
        <div style={{ display: 'flex', alignItems: 'baseline', gap: 8, marginBottom: 11 }}>
          <span style={{ fontSize: 13, fontWeight: 700, color: va.text }}>Lệnh sản xuất</span>
          <span style={{ fontFamily: va.mono, fontSize: 11, color: va.text3 }}>{jobs.length}</span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, height: 34, background: va.bg, border: `1px solid ${va.border}`, borderRadius: 7, padding: '0 10px' }}>
          <span style={{ color: va.text3, fontSize: 13 }}>⌕</span>
          <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Tìm job, part…"
            style={{ flex: 1, border: 'none', outline: 'none', background: 'transparent', fontSize: 12.5, color: va.text, fontFamily: va.font }} />
        </div>
      </div>
      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 8 }}>
        {jobs.length === 0 && (
          <div style={{ padding: 24, textAlign: 'center', color: va.text3, fontSize: 12 }}>Không có job khớp tìm kiếm.</div>
        )}
        {jobs.map(j => {
          const on = j.id === selectedJobId
          const status = STATUS_BADGE[deriveStatus(j)]
          const pct = completion(j)
          return (
            <div key={j.id} className="va-clickable" onClick={() => onSelect(j.id, j)}
              style={{ padding: '10px 11px', borderRadius: 8, marginBottom: 3, background: on ? va.accentBg : 'transparent', boxShadow: on ? `inset 0 0 0 1px ${va.accentLt}` : 'none' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 7, marginBottom: 4 }}>
                <span style={{ fontFamily: va.mono, fontSize: 12.5, fontWeight: 700, color: va.primary }}>{j.jobNumber}</span>
                <span style={{ marginLeft: 'auto' }}><VABadge kind={status.kind}>{status.label}</VABadge></span>
              </div>
              <div style={{ fontSize: 12, color: va.text2, marginBottom: 7 }}>
                <span style={{ fontWeight: 600, color: va.text }}>{j.partNumber}</span> · Rev {j.revCode}
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <div style={{ flex: 1, height: 4, background: va.surface3, borderRadius: 3, overflow: 'hidden' }}>
                  <div style={{ width: `${pct}%`, height: '100%', background: va.ok }} />
                </div>
                <span style={{ fontFamily: va.mono, fontSize: 10, color: va.text3 }}>{pct}%</span>
                {j.openNcrCount > 0 && <span style={{ fontFamily: va.mono, fontSize: 9.5, fontWeight: 700, color: va.err }}>⚑{j.openNcrCount}</span>}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
```

`va.surface3` already exists in `lib/va-tokens.ts` (`'#F6EADb'`).

- [ ] **Step 2: Type-check**

Run: `cd clients/web && npx tsc --noEmit`
Expected: no new errors.

- [ ] **Step 3: Commit**

```bash
git add clients/web/components/fai/fai-job-list.tsx
git commit -m "feat(fai): add FaiJobList component for the /fai job-list panel"
```

---

## Task 7: `FaiOpSelect` component (Operation dropdown with OP INS indicator)

**Files:**
- Create: `clients/web/components/fai/fai-op-select.tsx`

**Interfaces:**
- Consumes: `PartOpDto` (Task 5 — needs `dimCount`, `opTypeCode`, `opNumber`, `description`), `va` tokens.
- Produces: `FaiOpSelect({ ops, value, onChange }: { ops: PartOpDto[]; value: number | null; onChange: (opId: number) => void })` — used by Task 9 (`/fai/page.tsx`).

- [ ] **Step 1: Write the component**

```tsx
// clients/web/components/fai/fai-op-select.tsx
'use client'

import { useState } from 'react'
import type { PartOpDto } from '@/lib/api-client'
import { va } from '@/lib/va-tokens'

type Props = {
  ops: PartOpDto[]
  value: number | null
  onChange: (opId: number) => void
}

function isInspectionOp(op: PartOpDto): boolean {
  return (op.opTypeCode ?? '').toUpperCase() === 'INS'
}

export function FaiOpSelect({ ops, value, onChange }: Props) {
  const [open, setOpen] = useState(false)
  const cur = ops.find(o => o.id === value) ?? null

  return (
    <div style={{ position: 'relative' }}>
      <button type="button" className="va-clickable" onClick={() => setOpen(o => !o)}
        style={{ height: 34, minWidth: 220, padding: '0 12px', borderRadius: 7, border: `1px solid ${va.border}`, background: va.surface, color: va.text, fontSize: 12.5, fontWeight: 600, fontFamily: va.font, display: 'inline-flex', alignItems: 'center', gap: 8, cursor: 'pointer' }}>
        <span style={{ flex: 1, textAlign: 'left' }}>
          {cur ? `OP${cur.opNumber}${cur.description ? ` · ${cur.description}` : ''}` : '— Chọn Operation —'}
        </span>
        {cur && isInspectionOp(cur) && <span title="OP kiểm tra">🔍</span>}
        <span style={{ color: va.text3, fontSize: 11 }}>▾</span>
      </button>
      {open && (
        <>
          <div onClick={() => setOpen(false)} style={{ position: 'fixed', inset: 0, zIndex: 50 }} />
          <div style={{ position: 'absolute', top: 38, left: 0, zIndex: 51, minWidth: 260, background: va.surface, border: `1px solid ${va.border}`, borderRadius: 9, boxShadow: va.shadowLg, padding: 5, display: 'flex', flexDirection: 'column', gap: 1, maxHeight: 320, overflow: 'auto' }}>
            {ops.map(o => {
              const on = o.id === value
              const hasSheet = o.dimCount > 0
              return (
                <div key={o.id} className="va-clickable" onClick={() => { onChange(o.id); setOpen(false) }}
                  style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '7px 10px', borderRadius: 6, background: on ? va.accentBg : 'transparent', fontSize: 12.5, fontWeight: on ? 600 : 400, color: va.text }}>
                  <span style={{ width: 7, height: 7, borderRadius: '50%', background: hasSheet ? va.ok : va.borderStr, flexShrink: 0 }} />
                  <span style={{ flex: 1 }}>OP{o.opNumber}{o.description ? ` · ${o.description}` : ''}</span>
                  {isInspectionOp(o) && <span title="OP kiểm tra" style={{ fontSize: 12 }}>🔍</span>}
                  <span style={{ fontSize: 9.5, color: hasSheet ? va.ok : va.text3, fontWeight: 600 }}>
                    {hasSheet ? '● sheet' : 'chưa đo'}
                  </span>
                </div>
              )
            })}
          </div>
        </>
      )}
    </div>
  )
}
```

- [ ] **Step 2: Type-check**

Run: `cd clients/web && npx tsc --noEmit`
Expected: no new errors.

- [ ] **Step 3: Commit**

```bash
git add clients/web/components/fai/fai-op-select.tsx
git commit -m "feat(fai): add FaiOpSelect component with OP INS indicator"
```

---

## Task 8: Redesign `FaiMatrix`

**Files:**
- Modify: `clients/web/components/fai/fai-matrix.tsx` (full rewrite of the JSX body; imports and props/exported function name unchanged)

**Interfaces:**
- Consumes: `FaiSheetDto` (existing, now with `dimensions[].opNumber`), `MEASURE_STAGE_LABELS`, `downloadBlob`, `VABadge`, `VASeg`, `VABtn`, `VACard` (`components/va`), `va` tokens.
- Produces: `FaiMatrix({ sheet }: { sheet: FaiSheetDto })` — same signature as before, so Task 9 and `/jobs/[id]/fai/page.tsx` need no prop changes.

- [ ] **Step 1: Rewrite the component**

```tsx
// clients/web/components/fai/fai-matrix.tsx
'use client'

import { useMemo, useState } from 'react'
import type { MouseEvent } from 'react'
import Link from 'next/link'
import { api, type FaiSheetDto, MEASURE_STAGE_LABELS } from '@/lib/api-client'
import { VABadge, VASeg, VABtn, VACard } from '@/components/va'
import { va } from '@/lib/va-tokens'
import { downloadBlob } from '@/lib/doc-format'

type Props = {
  sheet: FaiSheetDto
}

const STAGE_OPTIONS = [
  { id: 'all', label: 'Tất cả' },
  { id: '0', label: MEASURE_STAGE_LABELS[0] },
  { id: '1', label: MEASURE_STAGE_LABELS[1] },
  { id: '2', label: MEASURE_STAGE_LABELS[2] },
]

const CATEGORY_COLOR: Record<string, string> = {
  LIN: va.primary, ANG: va.primaryLt, THD: va.accent, GEO: '#5D4037', SFC: va.text2,
}

type TooltipState = { x: number; y: number; lines: [string, string][] } | null

export function FaiMatrix({ sheet }: Props) {
  const { dimensions: dims, rows } = sheet
  const [stageFilter, setStageFilter] = useState('all')
  const [exporting, setExporting] = useState<'excel' | 'pdf' | null>(null)
  const [tip, setTip] = useState<TooltipState>(null)

  const allCells = useMemo(() => rows.flatMap(r => r.cells), [rows])
  const totalCells = dims.length * rows.length

  const scopedCells = stageFilter === 'all'
    ? allCells
    : allCells.filter(c => c.measureStage === Number(stageFilter))

  const filledCells = scopedCells.filter(c => c.value != null).length
  const passCells = scopedCells.filter(c => c.result === 'Pass').length
  const failCells = scopedCells.filter(c => c.result === 'Fail').length
  const pendingCells = totalCells - filledCells
  const passRate = filledCells > 0 ? Math.round((passCells / filledCells) * 100) : null

  const lastMeasured = allCells
    .filter(c => c.measuredAt)
    .sort((a, b) => new Date(b.measuredAt!).getTime() - new Date(a.measuredAt!).getTime())[0]

  async function handleExport(kind: 'excel' | 'pdf') {
    setExporting(kind)
    try {
      const stage = stageFilter === 'all' ? undefined : Number(stageFilter)
      const blob = kind === 'excel'
        ? await api.fai.exportExcel(sheet.partOpId, sheet.jobId, stage)
        : await api.fai.exportPdf(sheet.partOpId, sheet.jobId, stage)
      downloadBlob(blob, `FAI_OP${sheet.opNumber}.${kind === 'excel' ? 'xlsx' : 'pdf'}`)
    } finally {
      setExporting(null)
    }
  }

  function showTip(e: MouseEvent, stageValue: { value: number | null; measuredByName: string | null; measureStage?: number | null; gageNo: string | null; hasNcr: boolean; ncrCode: string | null; measuredAt: string | null } | null, dim: { unit: string }) {
    if (!stageValue?.value && stageValue?.value !== 0) return
    setTip({
      x: e.clientX, y: e.clientY,
      lines: [
        ['Giá trị', `${stageValue.value}${dim.unit ? ' ' + dim.unit : ''}`],
        ['Người đo', stageValue.measuredByName ?? '—'],
        ['Stage', stageValue.measureStage != null ? MEASURE_STAGE_LABELS[stageValue.measureStage] : '—'],
        ['Gage', stageValue.gageNo ?? '—'],
        ...(stageValue.hasNcr ? [['NCR', stageValue.ncrCode ?? '⚑'] as [string, string]] : []),
        ['Lúc', stageValue.measuredAt ? new Date(stageValue.measuredAt).toLocaleString('vi-VN') : '—'],
      ],
    })
  }

  if (dims.length === 0) {
    return (
      <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>
        Operation này chưa có dimension. Cần thêm dimensions trước.
      </div>
    )
  }

  return (
    <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 14 }}>
      {/* Info bar */}
      <div style={{ background: va.surface, border: `1px solid ${va.border}`, borderRadius: 11, padding: '14px 18px', display: 'flex', alignItems: 'center', boxShadow: va.shadow, flexWrap: 'wrap' }}>
        {([
          ['Part number', sheet.partNumber, true],
          ['Mô tả', sheet.partDescription, false],
          ['Rev', sheet.revCode, false],
          ['Job', sheet.jobNumber, false],
          ['Operation', `OP${sheet.opNumber}`, false],
        ] as [string, string, boolean][]).map(([k, v, mono], i) => (
          <div key={k} style={{ display: 'flex', alignItems: 'center' }}>
            {i > 0 && <div style={{ height: 34, width: 1, background: va.separator, margin: '0 18px' }} />}
            <div>
              <div style={{ fontSize: 10, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.6, fontWeight: 600, marginBottom: 3 }}>{k}</div>
              <div style={{ fontSize: 14, fontWeight: 600, fontFamily: mono ? va.mono : va.font, color: va.text }}>{v}</div>
            </div>
          </div>
        ))}
        {lastMeasured && (
          <div style={{ marginLeft: 'auto', textAlign: 'right' }}>
            <div style={{ fontSize: 10, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.6, fontWeight: 600, marginBottom: 3 }}>Đo gần nhất</div>
            <div style={{ fontSize: 13, fontWeight: 600 }}>{lastMeasured.measuredByName ?? '—'}</div>
            <div style={{ fontSize: 10.5, color: va.text3 }}>{lastMeasured.measuredAt ? new Date(lastMeasured.measuredAt).toLocaleString('vi-VN') : '—'}</div>
          </div>
        )}
      </div>

      {/* Stats strip */}
      <div style={{ background: va.surface, border: `1px solid ${va.border}`, borderRadius: 11, padding: '15px 18px', display: 'flex', alignItems: 'center', gap: 24, boxShadow: va.shadow, flexWrap: 'wrap' }}>
        {([
          ['Tổng ô', totalCells, va.text],
          ['Đã đo', filledCells, va.primary],
          ['Pass', passCells, va.ok],
          ['Fail · NCR', failCells, va.err],
          ['Pending', pendingCells, va.text2],
          ['Pass rate', passRate != null ? `${passRate}%` : '—', va.accent],
        ] as [string, string | number, string][]).map(([label, value, color]) => (
          <div key={label}>
            <div style={{ fontFamily: va.mono, fontSize: 22, fontWeight: 600, color, lineHeight: 1 }}>{value}</div>
            <div style={{ fontSize: 10, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.5, fontWeight: 600, marginTop: 5 }}>{label}</div>
          </div>
        ))}
        <VASeg options={STAGE_OPTIONS} value={stageFilter} onChange={setStageFilter} />
        <div style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: 14 }}>
          <span style={{ fontSize: 11, color: va.text3 }}>
            Đang xem: <b style={{ color: va.text2 }}>{stageFilter === 'all' ? 'Tất cả' : MEASURE_STAGE_LABELS[Number(stageFilter)]}</b>
          </span>
          <div style={{ display: 'flex', gap: 8 }}>
            <VABtn kind="ghost" onClick={() => handleExport('excel')} disabled={exporting !== null}>
              {exporting === 'excel' ? 'Đang xuất…' : '⤓ Excel'}
            </VABtn>
            <VABtn kind="primary" onClick={() => handleExport('pdf')} disabled={exporting !== null}>
              {exporting === 'pdf' ? 'Đang xuất…' : '⤓ Xuất FAI PDF'}
            </VABtn>
          </div>
        </div>
      </div>

      {/* Matrix card */}
      <VACard
        title="Serial × Dimension"
        sub={`${rows.length} serial × ${dims.length} balloon`}
        pad={false}
        style={{ minHeight: 0, flex: 1 }}
        right={<div style={{ display: 'flex', gap: 12, fontSize: 11, color: va.text2 }}>
          <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}><span style={{ width: 9, height: 9, background: va.ok, borderRadius: 2 }} />Pass</span>
          <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}><span style={{ width: 9, height: 9, background: va.err, borderRadius: 2 }} />Fail</span>
          <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}><span style={{ width: 9, height: 9, background: va.borderStr, borderRadius: 2 }} />Chưa đo</span>
          <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}><span style={{ color: va.err, fontSize: 12 }}>⚑</span>NCR</span>
        </div>}
      >
        <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
          <table style={{ borderCollapse: 'separate', borderSpacing: 0, fontSize: 12, width: '100%' }}>
            <thead>
              <tr>
                <th style={{ position: 'sticky', left: 0, top: 0, background: va.surface2, padding: '8px 14px', textAlign: 'left', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderRight: `2px solid ${va.borderStr}`, borderBottom: `1px solid ${va.border}`, zIndex: 4, minWidth: 92 }}>Serial</th>
                {dims.map(d => {
                  const color = d.categoryCode ? CATEGORY_COLOR[d.categoryCode] ?? va.text2 : va.text2
                  return (
                    <th key={d.id} style={{ position: 'sticky', top: 0, background: va.surface2, padding: '8px 8px 9px', textAlign: 'center', borderBottom: `1px solid ${va.border}`, borderRight: `1px solid ${va.separator}`, minWidth: 96, verticalAlign: 'top', zIndex: 2 }}>
                      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4 }}>
                        <div style={{ width: 26, height: 26, borderRadius: '50%', border: `2px solid ${d.isCritical ? va.err : color}`, color: d.isCritical ? va.err : color, display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: va.mono, fontWeight: 700, fontSize: 11 }}>
                          {d.balloonNumber}
                        </div>
                        <div style={{ fontFamily: va.mono, fontSize: 10.5, fontWeight: 600, color: va.text, lineHeight: 1.3 }}>
                          {d.isTextType ? d.nominalText : `${d.nominalValue ?? ''} +${d.tolerancePlus ?? 0}/-${d.toleranceMinus ?? 0}`}
                        </div>
                        <div style={{ fontFamily: va.mono, fontSize: 8.5, color: va.text3, lineHeight: 1.25 }}>
                          {d.categoryCode ?? d.unit}
                        </div>
                        {d.opNumber && (
                          <div style={{ fontFamily: va.mono, fontSize: 8, color: va.text3, opacity: 0.7 }}>OP{d.opNumber}</div>
                        )}
                      </div>
                    </th>
                  )
                })}
                <th style={{ position: 'sticky', top: 0, right: 0, background: va.surface2, padding: '8px 14px', textAlign: 'center', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, borderLeft: `2px solid ${va.borderStr}`, zIndex: 3, minWidth: 88 }}>Kết quả</th>
              </tr>
            </thead>
            <tbody>
              {rows.map(row => {
                const rowAllPass = stageFilter === 'all'
                  ? row.allPass
                  : row.cells.every(c => {
                      const sv = c.byStage[stageFilter]
                      return !sv || sv.result === 'Pass'
                    })
                return (
                  <tr key={row.productId} className="va-row">
                    <td style={{ position: 'sticky', left: 0, background: va.surface, padding: '0 14px', height: 46, borderRight: `2px solid ${va.borderStr}`, borderBottom: `1px solid ${va.separator}`, zIndex: 1 }}>
                      <Link href={`/fai/product/${row.productId}`} title="Xem toàn bộ dimension của Serial này qua mọi OP" style={{ color: va.primary, textDecoration: 'none', fontFamily: va.mono, fontWeight: 600 }}>
                        {row.serialNumber}
                      </Link>
                    </td>
                    {row.cells.map((cell, i) => {
                      const dim = dims[i]
                      const stageValue = stageFilter === 'all' ? cell : cell.byStage[stageFilter]
                      const value = stageValue?.value ?? null
                      const result = stageValue?.result ?? null
                      const hasData = stageFilter === 'all' || !!cell.byStage[stageFilter]
                      const bg = result === 'Pass' ? va.okBg : result === 'Fail' ? va.errBg : va.surface
                      return (
                        <td key={dim.id}
                          onMouseEnter={e => showTip(e, stageValue ? { ...stageValue, measureStage: cell.measureStage } : null, dim)}
                          onMouseMove={e => setTip(t => t ? { ...t, x: e.clientX, y: e.clientY } : t)}
                          onMouseLeave={() => setTip(null)}
                          style={{ position: 'relative', padding: 0, borderBottom: `1px solid ${va.separator}`, borderRight: `1px solid ${va.separator}`, background: bg, opacity: hasData ? 1 : 0.35 }}>
                          <div style={{ height: 46, padding: '0 10px', display: 'flex', alignItems: 'center', justifyContent: 'center', borderLeft: result === 'Fail' ? `2px solid ${va.err}` : '2px solid transparent' }}>
                            <span style={{ fontFamily: va.mono, fontSize: 12.5, fontWeight: result === 'Fail' ? 700 : 500, color: result === 'Fail' ? va.err : result === 'Pass' ? va.text : va.text3 }}>
                              {value ?? '—'}
                            </span>
                          </div>
                          {stageValue?.hasNcr && (
                            <span style={{ position: 'absolute', top: 2, right: 3, fontSize: 11, color: va.err, lineHeight: 1 }}>⚑</span>
                          )}
                        </td>
                      )
                    })}
                    <td style={{ position: 'sticky', right: 0, background: va.surface, padding: '0 14px', borderBottom: `1px solid ${va.separator}`, borderLeft: `2px solid ${va.borderStr}`, textAlign: 'center' }}>
                      <VABadge kind={rowAllPass ? 'ok' : 'err'} dot>{rowAllPass ? 'PASS' : 'FAIL'}</VABadge>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      </VACard>

      <div style={{ fontSize: 11, color: va.text3, display: 'flex', alignItems: 'center', gap: 6 }}>
        <span style={{ color: va.text2 }}>ⓘ</span>
        View read-only — dữ liệu đo nhập từ Desktop app. Click số <b style={{ color: va.primary }}>SN</b> để mở Serial Measure Sheet (toàn bộ dimension của serial qua mọi OP).
      </div>

      {tip && (
        <div style={{ position: 'fixed', left: Math.min(tip.x + 14, window.innerWidth - 230), top: tip.y + 14, zIndex: 9999, pointerEvents: 'none', background: va.text, color: '#fff', borderRadius: 8, padding: '9px 11px', boxShadow: va.shadowLg, minWidth: 180, fontSize: 11.5 }}>
          {tip.lines.map(([k, val]) => (
            <div key={k} style={{ display: 'flex', justifyContent: 'space-between', gap: 14, padding: '1.5px 0' }}>
              <span style={{ color: 'rgba(255,255,255,0.6)' }}>{k}</span>
              <span style={{ fontFamily: va.mono, fontWeight: 500, color: k === 'NCR' ? '#FFB4A8' : '#fff' }}>{val}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
```

`VACard` must be exported from `clients/web/components/va/index.ts` (or wherever the barrel file lives) — verify with a quick grep before this step; if it isn't exported yet, add `export { VACard } from './card'` to the barrel file.

- [ ] **Step 2: Verify `VACard` is exported from the `components/va` barrel**

Run: `cd clients/web && grep -rn "VACard" components/va/index.ts`
Expected: a line exporting `VACard`. If missing, add it (`export { VACard } from './card'`) to `clients/web/components/va/index.ts`.

- [ ] **Step 3: Type-check**

Run: `cd clients/web && npx tsc --noEmit`
Expected: no new errors.

- [ ] **Step 4: Commit**

```bash
git add clients/web/components/fai/fai-matrix.tsx clients/web/components/va/index.ts
git commit -m "feat(fai): redesign FaiMatrix with card layout, balloon circles, and floating tooltip"
```

---

## Task 9: `/fai` page — 2-column layout with `FaiJobList` + `FaiOpSelect`

**Files:**
- Modify: `clients/web/app/(main)/fai/page.tsx` (full rewrite)

**Interfaces:**
- Consumes: `FaiJobList` (Task 6), `FaiOpSelect` (Task 7), `FaiMatrix` (Task 8), `api.jobs.operations`, `api.fai.sheet` (existing).

- [ ] **Step 1: Rewrite the page**

```tsx
// clients/web/app/(main)/fai/page.tsx
'use client'

import { useState, useEffect, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import { api, type PartOpDto, type FaiSheetDto, type JobDto } from '@/lib/api-client'
import { VATopbar, VABtn } from '@/components/va'
import { va } from '@/lib/va-tokens'
import { FaiMatrix } from '@/components/fai/fai-matrix'
import { FaiJobList } from '@/components/fai/fai-job-list'
import { FaiOpSelect } from '@/components/fai/fai-op-select'

export default function FaiPage() {
  const router = useRouter()

  const [jobId, setJobId] = useState<number | null>(null)
  const [ops, setOps] = useState<PartOpDto[]>([])
  const [opId, setOpId] = useState<number | null>(null)

  const [sheet, setSheet] = useState<FaiSheetDto | null>(null)
  const [loading, setLoading] = useState(false)

  const onSelectJob = useCallback((id: number, _job: JobDto) => {
    setJobId(id)
    setOpId(null)
    setSheet(null)
  }, [])

  useEffect(() => {
    if (!jobId) { setOps([]); return }
    api.jobs.operations(jobId).then(res => {
      if (res.success && res.data) {
        setOps(res.data)
        const firstWithSheet = res.data.find(o => o.dimCount > 0)
        setOpId(firstWithSheet ? firstWithSheet.id : null)
      }
    })
  }, [jobId])

  const loadSheet = useCallback(() => {
    if (!jobId || !opId) { setSheet(null); return }
    setLoading(true)
    api.fai.sheet(opId, jobId).then(res => {
      if (res.success) setSheet(res.data)
      setLoading(false)
    })
  }, [jobId, opId])

  useEffect(() => { loadSheet() }, [loadSheet])

  return (
    <div style={{ flex: 1, display: 'flex', minWidth: 0, minHeight: 0, background: va.bg }}>
      <FaiJobList selectedJobId={jobId} onSelect={onSelectJob} />

      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0 }}>
        <VATopbar title="FAI · Ma trận đo kiểm" breadcrumb="Chất lượng › FAI & Đo kiểm"
          right={jobId ? <VABtn kind="ghost" onClick={() => router.push(`/jobs/${jobId}`)}>→ Job</VABtn> : undefined} />

        {!jobId ? (
          <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 12 }}>
            <div style={{ fontSize: 28, color: va.text3 }}>◉</div>
            <div style={{ fontSize: 14, color: va.text2, fontWeight: 600 }}>Chọn một Job</div>
            <div style={{ fontSize: 12.5, color: va.text3, textAlign: 'center', maxWidth: 320 }}>
              Chọn Job ở panel bên trái để xem bảng đo FAI.
            </div>
          </div>
        ) : (
          <>
            <div style={{ padding: '10px 22px', background: va.surface, borderBottom: `1px solid ${va.border}` }}>
              <FaiOpSelect ops={ops} value={opId} onChange={setOpId} />
            </div>

            {!opId ? (
              <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 12 }}>
                <div style={{ fontSize: 14, color: va.text2, fontWeight: 600 }}>Chọn Operation</div>
                <div style={{ fontSize: 12.5, color: va.text3, textAlign: 'center', maxWidth: 320 }}>
                  Job này chưa có Operation nào — hoặc chưa chọn Operation cần xem.
                </div>
              </div>
            ) : loading ? (
              <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>Đang tải FAI sheet…</div>
            ) : !sheet ? (
              <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.err, fontSize: 13 }}>Không tải được FAI sheet.</div>
            ) : (
              <FaiMatrix sheet={sheet} />
            )}
          </>
        )}
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Type-check**

Run: `cd clients/web && npx tsc --noEmit`
Expected: no new errors.

- [ ] **Step 3: Commit**

```bash
git add clients/web/app/\(main\)/fai/page.tsx
git commit -m "feat(fai): rebuild /fai page as 2-column layout with job-list panel"
```

---

## Task 10: Build + manual browser verification

**Files:** none (verification only)

- [ ] **Step 1: Full backend build + test**

Run: `cd src && dotnet build ShopfloorManager.sln && dotnet test ShopfloorManager.Application.Tests/ShopfloorManager.Application.Tests.csproj`
Expected: `Build succeeded.` and `Passed!` (9 tests).

- [ ] **Step 2: Full frontend type-check + build**

Run: `cd clients/web && npx tsc --noEmit && npm run build`
Expected: both succeed with no errors.

- [ ] **Step 3: Start infrastructure + API + web**

Run (3 separate terminals, or background):
```bash
docker compose -f docker-compose.dev.yml up -d
cd src && dotnet run --project ShopfloorManager.API
cd clients/web && npm run dev
```
Expected: API on `http://localhost:5066`, web on `http://localhost:3000`.

- [ ] **Step 4: Manual check — `/fai` page**

In a browser, navigate to `http://localhost:3000/fai` (logged in as a user with `QC Inspector`/`Manager`/`Administrator` role per CLAUDE.md permission rules):
- Confirm the left job-list panel renders with search, status badges, progress bars.
- Click a job → confirm the Operation dropdown (`FaiOpSelect`) populates and shows "● sheet" / "chưa đo" per OP.
- If the seeded dev DB has an OP with `OpType.Code = "INS"`, select it and confirm the matrix shows dimensions aggregated from prior OPs (balloon header shows the small "OP{n}" tag). If no such OP type exists in the dev DB yet, create one via `/master` → OP Types (code `INS`) and assign it to a `PartOp` to verify end-to-end — this is exploratory verification, not a required code change.
- Confirm the matrix shows balloon circles colored by category, floating tooltip on hover, sticky Serial/Kết quả columns, and the legend in the card header.

- [ ] **Step 5: Manual check — `/jobs/[id]/fai` page**

Navigate to a Job Detail page, pick an Operation, click into its FAI sheet (`/jobs/[id]/fai?opId=...`). Confirm: no job-list panel present, breadcrumb and "← Job" button unchanged, and the matrix below renders with the same new card-based design as `/fai`.

- [ ] **Step 6: Update progress log**

Append an entry to `Project_Documents/20_progress_log.md` describing this change (per CLAUDE.md's mandated workflow step "Cập nhật `20_progress_log.md`" after each phase change) — read the file's existing entry format first and match its style.

- [ ] **Step 7: Final commit (if Step 6 produced changes)**

```bash
git add Project_Documents/20_progress_log.md
git commit -m "docs: log FAI view redesign + OP INS fix in progress log"
```
