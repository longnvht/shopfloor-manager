# FAI — QC Inline Flow & MeasureStage Fix (Desktop) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix a bug where Desktop FAI never sends `MeasureStage` to the API (so "FAI Final" entries are mis-recorded as `InprocessFAI`), and add a new **QC Inline** flow (QC Inspector spot-checks a completed product, stage = `QCInline`) backed by a configurable inspection rate (factory-wide default + Job/PartOp override) managed from a new Web Master Data tab.

**Architecture:** Backend already has full `MeasureStage` support (enum, `SaveMeasureCommand`, `GetFaiSheetQuery` grouping by stage) — no backend change needed for the bug fix itself, only Desktop needs to start sending the field. New backend work is limited to the `QcInlineRate` config entity/API. Desktop's `FaiViewModel`/`FaiPage` is extended (bool `IsFinalMode` → `FaiMode` enum) to host the third mode instead of duplicating the page. Web reuses the existing single-dialog Master Data pattern (`MasterItemDialog` handles every kind via a switch) by adding a new `kind`.

**Tech Stack:** ASP.NET Core 9 / MediatR / FluentValidation / EF Core 9 (Npgsql) — Next.js 16 / TypeScript / react-hook-form / Zod — WPF .NET 9 / CommunityToolkit.Mvvm.

## Global Constraints

- `DECIMAL(14,4)` is required only for **measurement/dimension values** — `RatePercent` (0–100) is a config percentage, not a measurement value, so it uses a plain `decimal` without explicit precision (matches other non-measurement decimals in this codebase, which declare no explicit `HasPrecision`).
- No stored procedures, no DB triggers — all logic in C# (MediatR handlers).
- `snake_case` table/column names (handled automatically by `EFCore.NamingConventions`, already configured — do not add manual `HasColumnName` calls).
- Master Data catalog entries are **never hard-deleted** in this codebase — they use `is_active = false` to hide from dropdowns (see the banner text in `clients/web/app/(main)/master/page.tsx:184`). `QcInlineRate` follows the same convention: add `IsActive`, no DELETE endpoint.
- `clients/web/app/(main)/master/page.tsx` currently has **zero i18n** (hardcoded Vietnamese strings, no `useTranslations`) — the new tab follows the same hardcoded-Vietnamese convention; do not introduce i18n keys here.
- Desktop project (`ShopfloorManager.Desktop`) has **no project reference to `ShopfloorManager.Domain`** — never reference `ShopfloorManager.Domain.Enums.MeasureStage` from Desktop code; use a local Desktop-side enum/int instead.
- Application layer has **no test project for Desktop** (WPF) — only `ShopfloorManager.Application.Tests` (xUnit + EF InMemory) exists. Desktop changes are verified by build + manual run only.
- New MediatR handlers/validators in `ShopfloorManager.Application` are auto-registered via assembly scan (`AddMediatR`/`AddValidatorsFromAssembly` in `ShopfloorManager.Application/DependencyInjection.cs`) — no manual DI wiring needed.
- Backend write commands write `Note`/etc. via constructor positional records, matching the existing `SaveMeasureCommand`/`CreateDimensionCategoryCommand` style — follow the same record + handler + validator shape already used in `FaiCommands.cs` / `DimensionCategoryCommands.cs`.

---

## Part A — Backend: `QcInlineRate` entity, CRUD API, effective-rate lookup

### Task 1: Domain entity + DbContext wiring + EF migration

**Files:**
- Create: `src/ShopfloorManager.Domain/Entities/QcInlineRate.cs`
- Modify: `src/ShopfloorManager.Application/Common/Interfaces/IShopfloorDbContext.cs`
- Modify: `src/ShopfloorManager.Infrastructure/Data/ShopfloorDbContext.cs`
- Migration (generated): `src/ShopfloorManager.Infrastructure/Data/Migrations/{timestamp}_AddQcInlineRates.cs`

**Interfaces:**
- Produces: `QcInlineRate` entity with `Id (int)`, `JobId (int?)`, `PartOpId (int?)`, `RatePercent (decimal)`, `IsActive (bool)`, plus `BaseEntity` audit columns. `IShopfloorDbContext.QcInlineRates : DbSet<QcInlineRate>`.

- [ ] **Step 1: Create the entity**

```csharp
namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Mức kiểm QC Inline (% sản phẩm QC kiểm ngẫu nhiên trên chuyền).
/// JobId=null + PartOpId=null = mặc định toàn nhà máy (luôn tồn tại, không cho ẩn).
/// Độ ưu tiên resolve: (JobId,PartOpId) > (JobId,null) > (null,PartOpId) > (null,null).
/// </summary>
public class QcInlineRate : BaseEntity
{
    public int? JobId { get; set; }
    public int? PartOpId { get; set; }
    public decimal RatePercent { get; set; }
    public bool IsActive { get; set; } = true;
}
```

- [ ] **Step 2: Add the DbSet to the context interface**

In `src/ShopfloorManager.Application/Common/Interfaces/IShopfloorDbContext.cs`, under the `// Phase 3 — Quality` group (after `DbSet<MeasureValue> MeasureValues { get; }`), add:

```csharp
    DbSet<QcInlineRate> QcInlineRates { get; }
```

- [ ] **Step 3: Add the DbSet to the EF Core context**

In `src/ShopfloorManager.Infrastructure/Data/ShopfloorDbContext.cs`, next to `public DbSet<MeasureValue> MeasureValues => Set<MeasureValue>();`, add:

```csharp
    public DbSet<QcInlineRate> QcInlineRates => Set<QcInlineRate>();
```

- [ ] **Step 4: Generate the migration**

Run from repo root:
```bash
dotnet ef migrations add AddQcInlineRates --project src/ShopfloorManager.Infrastructure --startup-project src/ShopfloorManager.API
```
Expected: a new file `src/ShopfloorManager.Infrastructure/Data/Migrations/{timestamp}_AddQcInlineRates.cs` is created containing `migrationBuilder.CreateTable(name: "qc_inline_rates", ...)` with columns `id, job_id, part_op_id, rate_percent, is_active, created_at, updated_at, created_by, updated_by`.

- [ ] **Step 5: Add a data seed for the factory-default row inside the generated migration's `Up()`**

Open the generated migration file and add this line at the end of `Up()` (after the `CreateTable` call), seeding the one mandatory factory-default row:

```csharp
            migrationBuilder.InsertData(
                table: "qc_inline_rates",
                columns: new[] { "job_id", "part_op_id", "rate_percent", "is_active", "created_at", "updated_at" },
                values: new object?[] { null, null, 10m, true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow });
```
(Match the exact column list EF generated for the table — adjust column names if EF named them differently than listed above.)

- [ ] **Step 6: Apply the migration**

```bash
dotnet ef database update --project src/ShopfloorManager.Infrastructure --startup-project src/ShopfloorManager.API
```
Expected: `Done.` with no errors. Verify with `psql` or a DB client that `qc_inline_rates` has exactly one row (`job_id IS NULL AND part_op_id IS NULL`).

- [ ] **Step 7: Build to confirm no compile errors**

```bash
dotnet build src/ShopfloorManager.sln
```
Expected: `Build succeeded.`

- [ ] **Step 8: Commit**

```bash
git add src/ShopfloorManager.Domain/Entities/QcInlineRate.cs src/ShopfloorManager.Application/Common/Interfaces/IShopfloorDbContext.cs src/ShopfloorManager.Infrastructure/Data/ShopfloorDbContext.cs src/ShopfloorManager.Infrastructure/Data/Migrations/
git commit -m "feat(fai): add QcInlineRate entity and migration"
```

---

### Task 2: Application layer — queries, commands, handlers, tests (TDD)

**Files:**
- Create: `src/ShopfloorManager.Application/Production/QcInlineRateCommands.cs`
- Create: `src/ShopfloorManager.Application.Tests/QcInlineRateCommandsTests.cs`

**Interfaces:**
- Consumes: `IShopfloorDbContext.QcInlineRates` (Task 1), `TestDbContextFactory.Create()` (existing test helper).
- Produces:
  - `record QcInlineRateDto(int Id, int? JobId, string? JobNumber, int? PartOpId, string? OpNumber, decimal RatePercent, bool IsActive)`
  - `record GetQcInlineRatesQuery() : IRequest<Result<List<QcInlineRateDto>>>`
  - `record GetEffectiveQcInlineRateQuery(int JobId, int? PartOpId) : IRequest<Result<decimal>>`
  - `record CreateQcInlineRateCommand(int? JobId, int? PartOpId, decimal RatePercent) : IRequest<Result<QcInlineRateDto>>`
  - `record UpdateQcInlineRateCommand(int Id, decimal RatePercent, bool IsActive) : IRequest<Result<QcInlineRateDto>>`
  - All consumed by Task 3 (API controller) and Task 6 (Web `api-client.ts` mirrors these shapes).

- [ ] **Step 1: Write the failing tests**

Create `src/ShopfloorManager.Application.Tests/QcInlineRateCommandsTests.cs`:

```csharp
using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Infrastructure.Data;
using Xunit;

namespace ShopfloorManager.Application.Tests;

public class QcInlineRateCommandsTests
{
    private static async Task<(ShopfloorDbContext Db, Job Job, PartOp Op)> SeedAsync(decimal factoryDefault = 10m)
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
        var opType = new OpType { Code = "MLA", Name = "Medium Lathe" };
        db.OpTypes.Add(opType);
        var op = new PartOp { RoutingRev = routingRev, OpNumber = "60", OpNumberSort = 60m, OpType = opType, IsVisible = true };
        db.PartOps.Add(op);
        var job = new Job { JobNumber = "JB-26-031", PartRev = partRev, RoutingRev = routingRev };
        db.Jobs.Add(job);

        db.QcInlineRates.Add(new QcInlineRate { JobId = null, PartOpId = null, RatePercent = factoryDefault, IsActive = true });
        await db.SaveChangesAsync();

        return (db, job, op);
    }

    [Fact]
    public async Task Effective_rate_falls_back_to_factory_default_when_no_override_exists()
    {
        var (db, job, op) = await SeedAsync(factoryDefault: 10m);
        var handler = new GetEffectiveQcInlineRateQueryHandler(db);

        var result = await handler.Handle(new GetEffectiveQcInlineRateQuery(job.Id, op.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10m, result.Value);
    }

    [Fact]
    public async Task Job_and_partop_specific_override_wins_over_job_only_and_default()
    {
        var (db, job, op) = await SeedAsync(factoryDefault: 10m);
        db.QcInlineRates.Add(new QcInlineRate { JobId = job.Id, PartOpId = null, RatePercent = 20m, IsActive = true });
        db.QcInlineRates.Add(new QcInlineRate { JobId = job.Id, PartOpId = op.Id, RatePercent = 50m, IsActive = true });
        await db.SaveChangesAsync();
        var handler = new GetEffectiveQcInlineRateQueryHandler(db);

        var result = await handler.Handle(new GetEffectiveQcInlineRateQuery(job.Id, op.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(50m, result.Value);
    }

    [Fact]
    public async Task Job_only_override_wins_over_partop_only_override()
    {
        var (db, job, op) = await SeedAsync(factoryDefault: 10m);
        db.QcInlineRates.Add(new QcInlineRate { JobId = job.Id, PartOpId = null, RatePercent = 20m, IsActive = true });
        db.QcInlineRates.Add(new QcInlineRate { JobId = null, PartOpId = op.Id, RatePercent = 30m, IsActive = true });
        await db.SaveChangesAsync();
        var handler = new GetEffectiveQcInlineRateQueryHandler(db);

        var result = await handler.Handle(new GetEffectiveQcInlineRateQuery(job.Id, op.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(20m, result.Value);
    }

    [Fact]
    public async Task Inactive_override_is_ignored_falls_back_to_default()
    {
        var (db, job, op) = await SeedAsync(factoryDefault: 10m);
        db.QcInlineRates.Add(new QcInlineRate { JobId = job.Id, PartOpId = null, RatePercent = 20m, IsActive = false });
        await db.SaveChangesAsync();
        var handler = new GetEffectiveQcInlineRateQueryHandler(db);

        var result = await handler.Handle(new GetEffectiveQcInlineRateQuery(job.Id, op.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10m, result.Value);
    }

    [Fact]
    public async Task Create_then_update_rate_round_trips_via_handlers()
    {
        var (db, job, op) = await SeedAsync();
        var createHandler = new CreateQcInlineRateCommandHandler(db);
        var updateHandler = new UpdateQcInlineRateCommandHandler(db);

        var created = await createHandler.Handle(new CreateQcInlineRateCommand(job.Id, op.Id, 35m), CancellationToken.None);
        Assert.True(created.IsSuccess);
        Assert.Equal(35m, created.Value.RatePercent);

        var updated = await updateHandler.Handle(new UpdateQcInlineRateCommand(created.Value.Id, 45m, true), CancellationToken.None);
        Assert.True(updated.IsSuccess);
        Assert.Equal(45m, updated.Value.RatePercent);
    }

    [Fact]
    public async Task Update_cannot_change_isactive_to_false_on_the_factory_default_row()
    {
        var db = TestDbContextFactory.Create();
        var defaultRow = new QcInlineRate { JobId = null, PartOpId = null, RatePercent = 10m, IsActive = true };
        db.QcInlineRates.Add(defaultRow);
        await db.SaveChangesAsync();
        var handler = new UpdateQcInlineRateCommandHandler(db);

        var result = await handler.Handle(new UpdateQcInlineRateCommand(defaultRow.Id, 15m, false), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail (handlers don't exist yet)**

```bash
dotnet test src/ShopfloorManager.Application.Tests --filter QcInlineRateCommandsTests
```
Expected: compile error (`GetEffectiveQcInlineRateQueryHandler` etc. not found).

- [ ] **Step 3: Write the implementation**

Create `src/ShopfloorManager.Application/Production/QcInlineRateCommands.cs`:

```csharp
using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Production;

// ── DTOs ─────────────────────────────────────────────────────

public record QcInlineRateDto(
    int Id, int? JobId, string? JobNumber, int? PartOpId, string? OpNumber,
    decimal RatePercent, bool IsActive);

// ── Queries ──────────────────────────────────────────────────

public record GetQcInlineRatesQuery : IRequest<Result<List<QcInlineRateDto>>>;

public class GetQcInlineRatesQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetQcInlineRatesQuery, Result<List<QcInlineRateDto>>>
{
    public async Task<Result<List<QcInlineRateDto>>> Handle(GetQcInlineRatesQuery req, CancellationToken ct)
    {
        var rates = await db.QcInlineRates
            .OrderByDescending(r => r.JobId.HasValue).ThenByDescending(r => r.PartOpId.HasValue)
            .ToListAsync(ct);

        var jobIds = rates.Where(r => r.JobId.HasValue).Select(r => r.JobId!.Value).Distinct().ToList();
        var jobNumbers = await db.Jobs.Where(j => jobIds.Contains(j.Id))
            .ToDictionaryAsync(j => j.Id, j => j.JobNumber, ct);

        var opIds = rates.Where(r => r.PartOpId.HasValue).Select(r => r.PartOpId!.Value).Distinct().ToList();
        var opNumbers = await db.PartOps.Where(o => opIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.OpNumber, ct);

        var dtos = rates.Select(r => new QcInlineRateDto(
            r.Id, r.JobId, r.JobId.HasValue ? jobNumbers.GetValueOrDefault(r.JobId.Value) : null,
            r.PartOpId, r.PartOpId.HasValue ? opNumbers.GetValueOrDefault(r.PartOpId.Value) : null,
            r.RatePercent, r.IsActive)).ToList();
        return Result.Ok(dtos);
    }
}

public record GetEffectiveQcInlineRateQuery(int JobId, int? PartOpId) : IRequest<Result<decimal>>;

public class GetEffectiveQcInlineRateQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetEffectiveQcInlineRateQuery, Result<decimal>>
{
    public async Task<Result<decimal>> Handle(GetEffectiveQcInlineRateQuery req, CancellationToken ct)
    {
        var candidates = await db.QcInlineRates
            .Where(r => r.IsActive && (
                (r.JobId == req.JobId && r.PartOpId == req.PartOpId) ||
                (r.JobId == req.JobId && r.PartOpId == null) ||
                (r.JobId == null && r.PartOpId == req.PartOpId) ||
                (r.JobId == null && r.PartOpId == null)))
            .ToListAsync(ct);

        var best = candidates
            .OrderByDescending(r => r.JobId == req.JobId && r.PartOpId == req.PartOpId)
            .ThenByDescending(r => r.JobId == req.JobId && r.PartOpId == null)
            .ThenByDescending(r => r.JobId == null && r.PartOpId == req.PartOpId)
            .FirstOrDefault();

        return best is null ? Result.Fail("Chưa cấu hình mức kiểm QC Inline.") : Result.Ok(best.RatePercent);
    }
}

// ── Commands ─────────────────────────────────────────────────

public record CreateQcInlineRateCommand(int? JobId, int? PartOpId, decimal RatePercent)
    : IRequest<Result<QcInlineRateDto>>;

public class CreateQcInlineRateCommandValidator : AbstractValidator<CreateQcInlineRateCommand>
{
    public CreateQcInlineRateCommandValidator()
    {
        RuleFor(x => x.RatePercent).InclusiveBetween(0m, 100m);
    }
}

public class CreateQcInlineRateCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateQcInlineRateCommand, Result<QcInlineRateDto>>
{
    public async Task<Result<QcInlineRateDto>> Handle(CreateQcInlineRateCommand req, CancellationToken ct)
    {
        if (await db.QcInlineRates.AnyAsync(r => r.JobId == req.JobId && r.PartOpId == req.PartOpId, ct))
            return Result.Fail("Đã có mức kiểm cho Job/OP này — hãy sửa dòng hiện có.");

        var rate = new QcInlineRate { JobId = req.JobId, PartOpId = req.PartOpId, RatePercent = req.RatePercent, IsActive = true };
        db.QcInlineRates.Add(rate);
        await db.SaveChangesAsync(ct);
        return Result.Ok(new QcInlineRateDto(rate.Id, rate.JobId, null, rate.PartOpId, null, rate.RatePercent, rate.IsActive));
    }
}

public record UpdateQcInlineRateCommand(int Id, decimal RatePercent, bool IsActive)
    : IRequest<Result<QcInlineRateDto>>;

public class UpdateQcInlineRateCommandValidator : AbstractValidator<UpdateQcInlineRateCommand>
{
    public UpdateQcInlineRateCommandValidator()
    {
        RuleFor(x => x.RatePercent).InclusiveBetween(0m, 100m);
    }
}

public class UpdateQcInlineRateCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdateQcInlineRateCommand, Result<QcInlineRateDto>>
{
    public async Task<Result<QcInlineRateDto>> Handle(UpdateQcInlineRateCommand req, CancellationToken ct)
    {
        var rate = await db.QcInlineRates.FindAsync([req.Id], ct);
        if (rate is null) return Result.Fail($"Không tìm thấy mức kiểm ID {req.Id}.");

        var isFactoryDefault = rate.JobId is null && rate.PartOpId is null;
        if (isFactoryDefault && !req.IsActive)
            return Result.Fail("Không thể ẩn mức kiểm mặc định toàn nhà máy.");

        rate.RatePercent = req.RatePercent;
        rate.IsActive = req.IsActive;
        await db.SaveChangesAsync(ct);
        return Result.Ok(new QcInlineRateDto(rate.Id, rate.JobId, null, rate.PartOpId, null, rate.RatePercent, rate.IsActive));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test src/ShopfloorManager.Application.Tests --filter QcInlineRateCommandsTests
```
Expected: `Passed! - Failed: 0, Passed: 6`

- [ ] **Step 5: Commit**

```bash
git add src/ShopfloorManager.Application/Production/QcInlineRateCommands.cs src/ShopfloorManager.Application.Tests/QcInlineRateCommandsTests.cs
git commit -m "feat(fai): add QcInlineRate queries/commands with effective-rate resolution"
```

---

### Task 3: API endpoints

**Files:**
- Modify: `src/ShopfloorManager.API/Controllers/FaiController.cs`

**Interfaces:**
- Consumes: `GetQcInlineRatesQuery`, `GetEffectiveQcInlineRateQuery`, `CreateQcInlineRateCommand`, `UpdateQcInlineRateCommand` (Task 2).
- Produces: HTTP endpoints consumed by Task 6 (`api-client.ts`) and Task 9 (Desktop `FaiViewModel`).

- [ ] **Step 1: Add the endpoints**

In `src/ShopfloorManager.API/Controllers/FaiController.cs`, add `using ShopfloorManager.Application.Production;` is already present (same namespace as `FaiCommands`). Add this block right after the `ExportPdf` method, before the closing `}` of the class:

```csharp
    // ── QC Inline Rate config ────────────────────────────────

    /// <summary>List toàn bộ mức kiểm QC Inline — phục vụ trang Master Data (Web).</summary>
    [HttpGet("/api/v1/qc-inline-rates")]
    public async Task<IActionResult> GetQcInlineRates()
    {
        var result = await mediator.Send(new GetQcInlineRatesQuery());
        return Ok(ApiResponse<List<QcInlineRateDto>>.Ok(result.Value));
    }

    /// <summary>Mức kiểm hiệu lực cho 1 Job/OP — Desktop dùng để hiển thị banner ở màn QC Inline.</summary>
    [HttpGet("/api/v1/fai/qc-inline-rate")]
    public async Task<IActionResult> GetEffectiveQcInlineRate([FromQuery] int jobId, [FromQuery] int? partOpId)
    {
        var result = await mediator.Send(new GetEffectiveQcInlineRateQuery(jobId, partOpId));
        return result.IsSuccess
            ? Ok(ApiResponse<decimal>.Ok(result.Value))
            : Ok(ApiResponse<decimal>.Ok(0));
    }

    [HttpPost("/api/v1/qc-inline-rates")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> CreateQcInlineRate([FromBody] CreateQcInlineRateCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? StatusCode(201, ApiResponse<QcInlineRateDto>.Ok(result.Value))
            : BadRequest(ApiResponse<QcInlineRateDto>.Fail(result.Errors));
    }

    [HttpPut("/api/v1/qc-inline-rates/{id:int}")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> UpdateQcInlineRate(int id, [FromBody] UpdateQcInlineRateCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse<QcInlineRateDto>.Fail("ID không khớp."));
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? Ok(ApiResponse<QcInlineRateDto>.Ok(result.Value))
            : BadRequest(ApiResponse<QcInlineRateDto>.Fail(result.Errors));
    }
```

Note: `GetEffectiveQcInlineRate` returns `Ok(0)` on failure (no config found) rather than `BadRequest`, since the Desktop banner should degrade gracefully to "0%" rather than show an error for a missing config — this is informational-only per the design.

- [ ] **Step 2: Build**

```bash
dotnet build src/ShopfloorManager.sln
```
Expected: `Build succeeded.`

- [ ] **Step 3: Manual smoke test via Swagger**

Run `dotnet run --project src/ShopfloorManager.API`, open `http://localhost:5066/swagger`, log in via `/api/v1/auth/login` to get a token, then call `GET /api/v1/qc-inline-rates` with the bearer token. Expected: 200 with a JSON array containing the one factory-default row (`jobId: null, partOpId: null, ratePercent: 10`).

- [ ] **Step 4: Commit**

```bash
git add src/ShopfloorManager.API/Controllers/FaiController.cs
git commit -m "feat(fai): expose QcInlineRate CRUD and effective-rate endpoints"
```

---

## Part B — Web: Master Data tab for QC Inline Rate

### Task 4: `api-client.ts` — types and `api.qcInlineRates`

**Files:**
- Modify: `clients/web/lib/api-client.ts`

**Interfaces:**
- Consumes: endpoints from Task 3.
- Produces: `QcInlineRateDto` type, `api.qcInlineRates.{list, create, update}` consumed by Task 5.

- [ ] **Step 1: Add the type and API methods**

In `clients/web/lib/api-client.ts`, add near the other DTO type exports (alongside `JobDto`, `PartOpDto` — search the file for `export type JobDto` to find the right location and follow the same export style):

```typescript
export type QcInlineRateDto = {
  id: number
  jobId: number | null
  jobNumber: string | null
  partOpId: number | null
  opNumber: string | null
  ratePercent: number
  isActive: boolean
}
```

Then add a new entry to the `api` object (next to `machineGroups:` — same indentation level, after its closing `},`):

```typescript
  qcInlineRates: {
    list: () => request<QcInlineRateDto[]>('/api/v1/qc-inline-rates'),
    create: (body: { jobId: number | null; partOpId: number | null; ratePercent: number }) =>
      request<QcInlineRateDto>('/api/v1/qc-inline-rates', { method: 'POST', body: JSON.stringify(body) }),
    update: (id: number, body: { id: number; ratePercent: number; isActive: boolean }) =>
      request<QcInlineRateDto>(`/api/v1/qc-inline-rates/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  },
```

- [ ] **Step 2: Type-check**

```bash
cd clients/web && npx tsc --noEmit
```
Expected: no new errors referencing `qcInlineRates` or `QcInlineRateDto`.

- [ ] **Step 3: Commit**

```bash
git add clients/web/lib/api-client.ts
git commit -m "feat(web): add qcInlineRates API client methods"
```

---

### Task 5: Master Data tab — table + dialog

**Files:**
- Modify: `clients/web/app/(main)/master/page.tsx`
- Modify: `clients/web/components/master/master-item-dialog.tsx`

**Interfaces:**
- Consumes: `api.qcInlineRates.{list, create, update}` (Task 4), `api.jobs.list(page, search)` (existing), `api.jobs.operations(jobId)` (existing, returns `PartOpDto[]` with `id`/`opNumber`).
- Produces: a working 6th tab in the Master Data hub; no other task depends on this one.

- [ ] **Step 1: Add the 6th tab to `master/page.tsx`**

In `clients/web/app/(main)/master/page.tsx`:

Change the imports (line 4) to add the new type and import the dialog's new exports:
```typescript
import { api, type MachineDto, type MachineGroupDto, type OpTypeDto, type DimensionCategoryDto, type FileTypeDto, type QcInlineRateDto } from '@/lib/api-client'
```

Change `TABS` and `TAB_KINDS` (lines 9-10):
```typescript
const TABS = ['Máy móc', 'Nhóm máy', 'Loại OP', 'Dimension Category', 'Loại tài liệu', 'Mức kiểm QC Inline']
const TAB_KINDS: MasterKind[] = ['machine', 'machineGroup', 'opType', 'dimCategory', 'fileType', 'qcInlineRate']
```

Add state (after `const [fileTypes, ...]` on line 27):
```typescript
  const [qcRates,    setQcRates]    = useState<QcInlineRateDto[]>([])
```

Add it to the `load()` `Promise.all` (lines 35-48):
```typescript
  const load = useCallback(() => {
    setLoading(true)
    Promise.all([
      api.machines.list(false),
      api.opTypes.list(),
      api.dimCategories.list(),
      api.fileTypes2.list(),
      api.machineGroups.list(),
      api.qcInlineRates.list(),
    ]).then(([mRes, otRes, dcRes, ftRes, mgRes, qrRes]) => {
      if (mRes.success  && mRes.data)  setMachines(mRes.data)
      if (otRes.success && otRes.data) setOpTypes(otRes.data)
      if (dcRes.success && dcRes.data) setDimCats(dcRes.data)
      if (ftRes.success && ftRes.data) setFileTypes(ftRes.data)
      if (mgRes.success && mgRes.data) setGroups(mgRes.data)
      if (qrRes.success && qrRes.data) setQcRates(qrRes.data)
      setLoading(false)
    })
  }, [])
```

Add a 6th table to the `tables` array (after the File Types table, before the closing `]`):
```typescript
    // QC Inline Rates
    <table key="qcrates" style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead><tr style={{ background: va.surface2 }}>
        {['Job', 'OP', 'Mức kiểm (%)', 'Trạng thái'].map((h, i) => <th key={i} style={thStyle()}>{h}</th>)}
      </tr></thead>
      <tbody>
        {loading ? <tr><td colSpan={4} style={tdStyle}><span style={{ color: va.text3 }}>Đang tải…</span></td></tr>
          : qcRates.map(r => (
          <tr key={r.id} className="va-row va-clickable" onClick={() => openEdit(r)}>
            <td style={tdStyle}>{r.jobNumber ?? <span style={{ color: va.text3 }}>— Tất cả Job —</span>}</td>
            <td style={tdStyle}>{r.opNumber ?? <span style={{ color: va.text3 }}>— Tất cả OP —</span>}</td>
            <td style={{ ...tdStyle, fontFamily: va.mono, fontWeight: 600 }}>{r.ratePercent}%</td>
            <td style={tdStyle}><ActiveBadge active={r.isActive} /></td>
          </tr>
        ))}
      </tbody>
    </table>,
```

- [ ] **Step 2: Extend `master-item-dialog.tsx` with the `qcInlineRate` kind**

In `clients/web/components/master/master-item-dialog.tsx`:

Add the import and union types (lines 7, 13-14):
```typescript
import { api, type MachineDto, type MachineGroupDto, type OpTypeDto, type DimensionCategoryDto, type FileTypeDto, type QcInlineRateDto, type JobDto, type PartOpDto } from '@/lib/api-client'

export type MasterKind = 'machine' | 'machineGroup' | 'opType' | 'dimCategory' | 'fileType' | 'qcInlineRate'
export type MasterItem = MachineDto | MachineGroupDto | OpTypeDto | DimensionCategoryDto | FileTypeDto | QcInlineRateDto
```

Add the title (line 16-22):
```typescript
const TITLES: Record<MasterKind, string> = {
  machine: 'Máy',
  machineGroup: 'Nhóm máy',
  opType: 'Loại OP',
  dimCategory: 'Dimension Category',
  fileType: 'Loại tài liệu',
  qcInlineRate: 'Mức kiểm QC Inline',
}
```

`qcInlineRate` needs its own local state for the Job/PartOp pickers (these are not simple text fields, so they're managed outside the shared `react-hook-form` schema). Add inside the component body, after the `useForm` line (line 47):
```typescript
  const [qrJobSearch, setQrJobSearch] = useState('')
  const [qrJobOptions, setQrJobOptions] = useState<JobDto[]>([])
  const [qrJobId, setQrJobId] = useState<number | null>(null)
  const [qrOpOptions, setQrOpOptions] = useState<PartOpDto[]>([])
  const [qrOpId, setQrOpId] = useState<number | null>(null)
  const [qrRatePercent, setQrRatePercent] = useState('10')
```

Add to the `useEffect` that resets state when the dialog opens (after the `switch (kind)` block ends, i.e. right before the closing `}, [open, item, kind, reset])`) a branch that initializes the QC Inline Rate local state — restructure the `useEffect` body to add this case inside the existing `switch (kind)` (after the `fileType` case, before the closing brace of the switch):
```typescript
      case 'qcInlineRate': {
        const r = item as QcInlineRateDto
        setQrJobId(r.jobId)
        setQrOpId(r.partOpId)
        setQrRatePercent(String(r.ratePercent))
        setQrJobSearch(r.jobNumber ?? '')
        break
      }
```
And when `!item` (create mode, the early-return branch at line 52-57), also reset the QC Inline Rate local state — add these three lines right before the `return` on line 57:
```typescript
      setQrJobId(null); setQrOpId(null); setQrRatePercent('10'); setQrJobSearch(''); setQrJobOptions([]); setQrOpOptions([])
```

Add a debounced job search effect, right after the state declarations added above:
```typescript
  useEffect(() => {
    if (kind !== 'qcInlineRate' || !open) return
    const t = setTimeout(() => {
      api.jobs.list(1, qrJobSearch || undefined).then(res => { if (res.success && res.data) setQrJobOptions(res.data) })
    }, 250)
    return () => clearTimeout(t)
  }, [kind, open, qrJobSearch])

  useEffect(() => {
    if (kind !== 'qcInlineRate' || !qrJobId) { setQrOpOptions([]); return }
    api.jobs.operations(qrJobId).then(res => { if (res.success && res.data) setQrOpOptions(res.data) })
  }, [kind, qrJobId])
```

Add the submit branch inside the `onSubmit` switch (after the `fileType` case, before the closing brace of the switch on line 150):
```typescript
      case 'qcInlineRate': {
        const ratePercent = Number(qrRatePercent)
        if (!Number.isFinite(ratePercent) || ratePercent < 0 || ratePercent > 100) { setError('Mức kiểm phải từ 0 đến 100'); return }
        const r = item as QcInlineRateDto | null
        const res = r
          ? await api.qcInlineRates.update(r.id, { id: r.id, ratePercent, isActive: data.isActive ?? true })
          : await api.qcInlineRates.create({ jobId: qrJobId, partOpId: qrOpId, ratePercent })
        if (res.success) { onClose(); onSaved() } else setError(res.error ?? 'Lỗi lưu mức kiểm QC Inline')
        break
      }
```

Add the render branch in the JSX, right after the `{kind === 'fileType' && (...)}` block (before the `isActive` checkbox at line 222) — and **suppress the generic Code/Name fields for this kind** by wrapping the existing first `<div className="grid grid-cols-2 gap-3">` (lines 161-171, the Code/Name fields) with a condition. Change line 161 from:
```typescript
            <div className="grid grid-cols-2 gap-3">
```
to:
```typescript
            {kind !== 'qcInlineRate' && (
            <div className="grid grid-cols-2 gap-3">
```
and close it right after its existing closing `</div>` (the one matching line 171) by adding `)}` right after that `</div>`.

Then add the QC Inline Rate fields block, after the `fileType` block (before the `isActive` checkbox):
```typescript
            {kind === 'qcInlineRate' && (
              <>
                <div className="space-y-1.5">
                  <Label>Job (để trống = áp dụng mọi Job)</Label>
                  <Input value={qrJobSearch} onChange={e => { setQrJobSearch(e.target.value); setQrJobId(null) }} placeholder="Tìm theo số Job..." disabled={!!item} />
                  {!item && qrJobOptions.length > 0 && (
                    <div className="border rounded-md max-h-32 overflow-y-auto">
                      {qrJobOptions.map(j => (
                        <div key={j.id} className={`px-2 py-1 text-sm cursor-pointer hover:bg-accent ${qrJobId === j.id ? 'bg-accent' : ''}`}
                          onClick={() => { setQrJobId(j.id); setQrJobSearch(j.jobNumber) }}>
                          {j.jobNumber}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
                <div className="space-y-1.5">
                  <Label>OP (để trống = áp dụng mọi OP của Job trên)</Label>
                  <select className="w-full h-9 rounded-md border px-2 text-sm" disabled={!!item || !qrJobId}
                    value={qrOpId ?? ''} onChange={e => setQrOpId(e.target.value ? Number(e.target.value) : null)}>
                    <option value="">— Tất cả OP —</option>
                    {qrOpOptions.map(o => <option key={o.id} value={o.id}>{o.opNumber}</option>)}
                  </select>
                </div>
                <div className="space-y-1.5">
                  <Label>Mức kiểm (%) *</Label>
                  <Input type="number" min={0} max={100} value={qrRatePercent} onChange={e => setQrRatePercent(e.target.value)} />
                </div>
              </>
            )}
```

Finally, hide the generic `isActive` checkbox label text mismatch for this kind is fine as-is ("Đang hoạt động (hiện trong dropdown)" reads slightly off but is acceptable — for the factory-default row, the checkbox should be disabled). Find the `isActive` checkbox block (lines 222-225) and make it conditionally disabled:
```typescript
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" {...register('isActive')} className="h-4 w-4"
                disabled={kind === 'qcInlineRate' && !!item && (item as QcInlineRateDto).jobId == null && (item as QcInlineRateDto).partOpId == null} />
              Đang hoạt động (hiện trong dropdown)
            </label>
```

- [ ] **Step 2: Type-check**

```bash
cd clients/web && npx tsc --noEmit
```
Expected: no errors.

- [ ] **Step 3: Manual verification in browser**

Start the stack (`docker compose -f docker-compose.dev.yml up -d`, `dotnet run --project src/ShopfloorManager.API`, `cd clients/web && npm run dev`). Log in as Administrator, go to `/master`, click the "Mức kiểm QC Inline" tab. Verify: the factory-default row shows "— Tất cả Job —" / "— Tất cả OP —" / the seeded %. Click "+ Thêm mục" while on this tab, search and pick a Job, optionally pick an OP, set a rate, save — verify the new row appears. Click the factory-default row to edit — verify the "Đang hoạt động" checkbox is disabled and saving with a new % works.

- [ ] **Step 4: Commit**

```bash
git add clients/web/app/\(main\)/master/page.tsx clients/web/components/master/master-item-dialog.tsx
git commit -m "feat(web): add QC Inline Rate tab to Master Data hub"
```

---

## Part C — Desktop: `FaiMode`, MeasureStage fix, QC Inline flow

### Task 6: `FaiModels.cs` — `FaiMode` enum + per-stage cell data

**Files:**
- Modify: `src/ShopfloorManager.Desktop/Models/FaiModels.cs`

**Interfaces:**
- Produces: `enum FaiMode { Basic, Final, QcInline }`, `record FaiStageCellData(decimal? Value, string? Result)`, `FaiCellData` extended with `Dictionary<int, FaiStageCellData>? ByStage`. Consumed by Task 7 (`FaiViewModel`).

- [ ] **Step 1: Add the enum and extend `FaiCellData`**

In `src/ShopfloorManager.Desktop/Models/FaiModels.cs`, replace:
```csharp
public record FaiCellData(
    long? MeasureValueId, string BalloonNumber,
    decimal? Value, string? Result);
```
with:
```csharp
public record FaiStageCellData(decimal? Value, string? Result);

public record FaiCellData(
    long? MeasureValueId, string BalloonNumber,
    decimal? Value, string? Result,
    Dictionary<int, FaiStageCellData>? ByStage = null);

/// <summary>
/// 3 chế độ FAI trên Desktop — KHÔNG tham chiếu ShopfloorManager.Domain.Enums.MeasureStage
/// (Desktop không có project reference tới Domain). Giá trị int khớp thủ công với
/// ShopfloorManager.Domain.Enums.MeasureStage: InprocessFAI=0, QCInline=1, QCFinal=2.
/// </summary>
public enum FaiMode { Basic, Final, QcInline }
```

No extra response record is needed for the rate banner call — `IApiClient.GetAsync<TResponse>` already wraps the response in `ApiResponse<TResponse>`, so `GetAsync<decimal>(...)` deserializes straight into `ApiResponse<decimal>.Data` (same pattern as every other typed `GetAsync<T>` call in this codebase, e.g. `GetAsync<FaiSheetResponse>` above).

- [ ] **Step 2: Build**

```bash
dotnet build src/ShopfloorManager.sln
```
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/ShopfloorManager.Desktop/Models/FaiModels.cs
git commit -m "feat(desktop): add FaiMode enum and per-stage cell data to FAI models"
```

---

### Task 7: `FaiViewModel` — `FaiMode`, MeasureStage fix, QC Inline filtering, rate banner

**Files:**
- Modify: `src/ShopfloorManager.Desktop/ViewModels/FaiViewModel.cs`

**Interfaces:**
- Consumes: `FaiMode`, `FaiCellData.ByStage` (Task 6); `GET /api/v1/fai/qc-inline-rate` (Task 3).
- Produces: `public FaiMode Mode { get; set; }` (replaces `IsFinalMode`), consumed by Task 8 (XAML) and Task 9 (`MainViewModel`).

- [ ] **Step 1: Replace `IsFinalMode` with `Mode`, add rate banner properties**

Replace:
```csharp
    /// <summary>
    /// true = FAI Final mode: re-inspect sau rework, chỉ hiển thị Fail dims,
    /// chỉ QC Inspector, lưu với IsFinal=true.
    /// </summary>
    public bool IsFinalMode { get; set; }

    public string PageTitle => IsFinalMode
        ? "NHẬP KẾT QUẢ ĐO (FAI FINAL)"
        : "NHẬP KẾT QUẢ ĐO (FAI)";
```
with:
```csharp
    /// <summary>Basic = Operator (InprocessFAI). Final = re-inspect sau rework (QCFinal), chỉ Fail dims.
    /// QcInline = QC Inspector kiểm ngẫu nhiên (QCInline), không bắt buộc đo hết.</summary>
    public FaiMode Mode { get; set; } = FaiMode.Basic;

    /// <summary>Binding read-only cho XAML — giữ tương thích với trigger màu title bar hiện có.</summary>
    public bool IsFinalMode => Mode == FaiMode.Final;
    public bool IsQcInlineMode => Mode == FaiMode.QcInline;

    public string PageTitle => Mode switch
    {
        FaiMode.Final    => "NHẬP KẾT QUẢ ĐO (FAI FINAL)",
        FaiMode.QcInline => "NHẬP KẾT QUẢ ĐO (QC INLINE)",
        _                => "NHẬP KẾT QUẢ ĐO (FAI)",
    };

    [ObservableProperty]
    private string? _rateInfoText;
```

- [ ] **Step 2: Replace the `IsInputLocked` logic to use per-stage lookups for all three modes**

Replace:
```csharp
    // Normal FAI: lock sau bất kỳ lần đo nào.
    // FAI Final: chỉ lock khi Pass (Fail dims vẫn cho phép đo lại).
    public bool IsInputLocked    => IsFinalMode
        ? SelectedDimension?.State == MeasureState.Pass
        : SelectedDimension?.IsMeasured == true;
```
with:
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
(`QcInline`'s "already measured" state is computed per-stage in `LoadAsync`, below — by the time it reaches `DimensionCardVm.State`/`IsMeasured`, the per-stage resolution already happened, so this switch only needs to special-case `Final`.)

- [ ] **Step 3: Resolve per-stage cell value in `LoadAsync` instead of the cross-stage "latest" field**

Replace the `foreach (var dim in resp.Data.Dimensions ?? [])` loop body in `LoadAsync`:
```csharp
            foreach (var dim in resp.Data.Dimensions ?? [])
            {
                var cell = row?.Cells?.FirstOrDefault(c => c.BalloonNumber == dim.BalloonNumber);
                var state = cell?.Result switch
                {
                    "Pass" => MeasureState.Pass,
                    "Fail" => MeasureState.Fail,
                    _      => MeasureState.Unmeasured
                };
                Dimensions.Add(new DimensionCardVm
                {
                    Id            = dim.Id,
                    BalloonNumber = dim.BalloonNumber,
                    NominalValue  = dim.NominalValue,
                    TolerancePlus = dim.TolerancePlus,
                    ToleranceMinus = dim.ToleranceMinus,
                    MaxValue      = dim.MaxValue,
                    MinValue      = dim.MinValue,
                    Unit          = dim.Unit ?? "",
                    IsTextType    = dim.IsTextType,
                    NominalText   = dim.NominalText,
                    IsFinal       = dim.IsFinal,
                    IsCritical    = dim.IsCritical,
                    State         = state,
                    MeasuredValue = cell?.Value
                });
            }
```
with:
```csharp
            var stageKey = (int)ToServerStage(Mode);
            foreach (var dim in resp.Data.Dimensions ?? [])
            {
                var cell = row?.Cells?.FirstOrDefault(c => c.BalloonNumber == dim.BalloonNumber);
                // Đọc giá trị riêng của stage hiện tại — KHÔNG dùng cell.Result/Value (đó là "mới nhất
                // qua mọi stage", có thể lẫn dữ liệu của stage khác cho cùng dimension/product).
                var stageCell = cell?.ByStage?.GetValueOrDefault(stageKey);
                var state = stageCell?.Result switch
                {
                    "Pass" => MeasureState.Pass,
                    "Fail" => MeasureState.Fail,
                    _      => MeasureState.Unmeasured
                };
                Dimensions.Add(new DimensionCardVm
                {
                    Id            = dim.Id,
                    BalloonNumber = dim.BalloonNumber,
                    NominalValue  = dim.NominalValue,
                    TolerancePlus = dim.TolerancePlus,
                    ToleranceMinus = dim.ToleranceMinus,
                    MaxValue      = dim.MaxValue,
                    MinValue      = dim.MinValue,
                    Unit          = dim.Unit ?? "",
                    IsTextType    = dim.IsTextType,
                    NominalText   = dim.NominalText,
                    IsFinal       = dim.IsFinal,
                    IsCritical    = dim.IsCritical,
                    State         = state,
                    MeasuredValue = stageCell?.Value
                });
            }
```

Replace the `IsFinalMode` checks further down in `LoadAsync`:
```csharp
            // FAI Final: chỉ giữ lại dims có trạng thái Fail để re-inspect
            if (IsFinalMode)
            {
                var failDims = Dimensions.Where(d => d.State == MeasureState.Fail).ToList();
                Dimensions.Clear();
                foreach (var d in failDims) Dimensions.Add(d);
            }

            RefreshProgress();

            if (!Dimensions.Any())
                ErrorMessage = IsFinalMode
                    ? "Không có kích thước nào ở trạng thái FAIL để re-inspect."
                    : "OP này chưa có kích thước nào được định nghĩa.";
            else
                SelectedDimension = Dimensions.FirstOrDefault(d =>
                    IsFinalMode ? d.State == MeasureState.Fail : !d.IsMeasured);
```
with:
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

            if (Mode == FaiMode.QcInline)
                _ = LoadRateInfoAsync();
```

- [ ] **Step 4: Add the stage-mapping helper and rate banner loader, and fix `SaveAsync` to send `MeasureStage`**

Add this private method anywhere in the class (e.g. right before `LoadAsync`):
```csharp
    /// <summary>Khớp thủ công với ShopfloorManager.Domain.Enums.MeasureStage trên server (int): 0/1/2.</summary>
    private static int ToServerStage(FaiMode mode) => mode switch
    {
        FaiMode.Final    => 2, // QCFinal
        FaiMode.QcInline => 1, // QCInline
        _                => 0, // InprocessFAI
    };

    private async Task LoadRateInfoAsync()
    {
        try
        {
            var resp = await _api.GetAsync<decimal>($"/api/v1/fai/qc-inline-rate?jobId={Job!.Id}&partOpId={Op!.Id}");
            RateInfoText = resp?.Data is decimal rate ? $"Mức kiểm đề xuất: {rate:0.#}%" : null;
        }
        catch { RateInfoText = null; }
    }
```

Replace the request object inside `SaveAsync`:
```csharp
            var req = new
            {
                DimensionId  = SelectedDimension.Id,
                ProductId    = Product!.ProductId,
                Value        = value,
                ManualResult = manualResult,
                IsFinal      = IsFinalMode,
                Note         = (string?)null
            };
```
with:
```csharp
            var req = new
            {
                DimensionId  = SelectedDimension.Id,
                ProductId    = Product!.ProductId,
                Value        = value,
                ManualResult = manualResult,
                IsFinal      = Mode == FaiMode.Final,
                Note         = (string?)null,
                MeasureStage = ToServerStage(Mode)
            };
```

Replace the post-save dimension selection logic (uses `IsFinalMode`):
```csharp
            InputValue        = "";
            SelectedDimension = IsFinalMode
                ? Dimensions.FirstOrDefault(d => d.State == MeasureState.Fail)
                : Dimensions.FirstOrDefault(d => !d.IsMeasured);
            RefreshProgress();
```
with:
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

- [ ] **Step 5: Build**

```bash
dotnet build src/ShopfloorManager.sln
```
Expected: `Build succeeded.` (fix any remaining references to the now-removed settable `IsFinalMode` — Task 9 updates `MainViewModel`'s assignment.)

- [ ] **Step 6: Commit**

```bash
git add src/ShopfloorManager.Desktop/ViewModels/FaiViewModel.cs
git commit -m "fix(desktop): send MeasureStage on save; add QcInline mode with per-stage state"
```

---

### Task 8: `FaiPage.xaml` — rate banner

**Files:**
- Modify: `src/ShopfloorManager.Desktop/Views/Pages/FaiPage.xaml`

**Interfaces:**
- Consumes: `RateInfoText` (Task 7).

- [ ] **Step 1: Add the banner TextBlock**

In the title bar `StackPanel` (inside `<Border Grid.Row="0" ...>`), right after the `TitleContext` `TextBlock` (after line 73's closing `/>`), add:
```xml
                    <TextBlock Text="{Binding RateInfoText, Mode=OneWay}"
                               Foreground="{StaticResource BrandAccentLight}"
                               FontSize="11" FontWeight="Medium" Margin="0,1,0,0"
                               Visibility="{Binding RateInfoText, Converter={StaticResource StringToVisibilityConverter}}"/>
```
(`StringToVisibilityConverter` is already a registered resource — it's used at line 182 of this same file for `ErrorMessage`.)

- [ ] **Step 2: Build**

```bash
dotnet build src/ShopfloorManager.sln
```
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/ShopfloorManager.Desktop/Views/Pages/FaiPage.xaml
git commit -m "feat(desktop): show QC Inline rate banner on FAI page"
```

---

### Task 9: `MainViewModel` + `DashboardViewModel` — QC Inline navigation and shortcut

**Files:**
- Modify: `src/ShopfloorManager.Desktop/ViewModels/MainViewModel.cs`
- Modify: `src/ShopfloorManager.Desktop/ViewModels/DashboardViewModel.cs`

**Interfaces:**
- Consumes: `FaiViewModel.Mode` (Task 7), `WorkContext.CurrentProduct.StatusCode` (existing).
- Produces: `NavigateToQcInline()`, `"qc-inline"` navigation target, `"QC Inline"` shortcut.

- [ ] **Step 1: Fix `NavigateToFaiFinal`'s now-broken assignment and add `NavigateToQcInline`**

In `src/ShopfloorManager.Desktop/ViewModels/MainViewModel.cs`, replace:
```csharp
        var vm = _sp.GetRequiredService<FaiViewModel>();
        vm.IsFinalMode = true;
        vm.OnBack = NavigateToDashboard;
        vm.OnDimensionFail = ShowNcrDialog;
        SetPage(vm);
        _ = vm.InitializeAsync();
    }

    // ===== Document Viewer =====
```
with:
```csharp
        var vm = _sp.GetRequiredService<FaiViewModel>();
        vm.Mode = FaiMode.Final;
        vm.OnBack = NavigateToDashboard;
        vm.OnDimensionFail = ShowNcrDialog;
        SetPage(vm);
        _ = vm.InitializeAsync();
    }

    // ===== QC Inline (QC kiểm ngẫu nhiên trên sản phẩm đã hoàn thành OP) =====

    public void NavigateToQcInline()
    {
        _keyboard.Hide();
        if (_work.CurrentJob is null || _work.CurrentOp is null || _work.CurrentProduct is null
            || _work.CurrentProduct.StatusCode != "complete")
        {
            NavigateToDashboard();
            return;
        }
        var vm = _sp.GetRequiredService<FaiViewModel>();
        vm.Mode = FaiMode.QcInline;
        vm.OnBack = NavigateToDashboard;
        vm.OnDimensionFail = ShowNcrDialog;
        SetPage(vm);
        _ = vm.InitializeAsync();
    }

    // ===== Document Viewer =====
```

Add a `using ShopfloorManager.Desktop.Models;` at the top of the file if not already present (check the existing `using` block — `FaiMode` lives in `ShopfloorManager.Desktop.Models`, same namespace as other models already referenced by this file such as `JobSummaryDto`).

Add the new case to the navigation switch (next to the existing `case "fai-final":`):
```csharp
            case "fai-final": NavigateToFaiFinal();    break;
            case "qc-inline": NavigateToQcInline();    break;
```

- [ ] **Step 2: Add the shortcut**

In `src/ShopfloorManager.Desktop/ViewModels/DashboardViewModel.cs`, in `RefreshShortcuts`, replace:
```csharp
        if (role is "QC Inspector" or "Administrator")
        {
            Add("FAI Final",  "ClipboardCheckOutline","fai-final", when: canFai);
        }
```
with:
```csharp
        bool canQcInline = !_work.IsViewMode && hasProd
            && _work.CurrentProduct?.StatusCode == "complete";
        if (role is "QC Inspector" or "Administrator")
        {
            Add("FAI Final",  "ClipboardCheckOutline","fai-final",  when: canFai);
            Add("QC Inline",  "Magnify",               "qc-inline", when: canQcInline);
        }
```

- [ ] **Step 3: Build**

```bash
dotnet build src/ShopfloorManager.sln
```
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/ShopfloorManager.Desktop/ViewModels/MainViewModel.cs src/ShopfloorManager.Desktop/ViewModels/DashboardViewModel.cs
git commit -m "feat(desktop): wire QC Inline navigation and dashboard shortcut"
```

---

### Task 10: Manual end-to-end verification on Desktop

**Files:** none (verification only).

- [ ] **Step 1: Start the full stack**

```bash
docker compose -f docker-compose.dev.yml up -d
dotnet run --project src/ShopfloorManager.API
```
In a separate terminal, run the Desktop app from Visual Studio or:
```bash
dotnet run --project src/ShopfloorManager.Desktop
```

- [ ] **Step 2: Verify the MeasureStage bug fix**

Log in as an Operator, claim a product, start the session, go to "Bảng đo" (FAI Basic), measure one numeric dimension. Query the DB (`psql` or any client): `SELECT measure_stage FROM measure_values ORDER BY id DESC LIMIT 1;` — expect `0`. Log in as QC Inspector/Administrator on the same product (session still active), open "FAI Final", measure a Fail dimension if any exists (or force one). Re-check the DB — the new row's `measure_stage` should be `2`, not `0` (this was the bug — previously it would have been `0` here too).

- [ ] **Step 3: Verify QC Inline flow**

Complete a session for some product (`StatusCode == "complete"`). Log in as QC Inspector, navigate to that Job/OP/Product — the "QC Inline" shortcut should appear on the dashboard (and should NOT appear for an in-progress, non-completed product). Open it — verify the title bar shows "NHẬP KẾT QUẢ ĐO (QC INLINE)" with the rate banner ("Mức kiểm đề xuất: X%") under it, all dimensions are shown (not just Fail), measuring a dimension does not auto-advance to the next one, and re-opening the page shows the previously-entered QC Inline value locked while leaving dimensions never measured under stage `QCInline` open for entry even if they already have an `InprocessFAI` value from the Operator.

- [ ] **Step 4: Confirm no regression in FAI Basic / FAI Final**

Repeat the standard FAI Basic flow (auto-advance, lock after one entry) and FAI Final flow (only Fail dims shown, locks on Pass) — confirm both behave exactly as before this change.

- [ ] **Step 5: Report results**

No commit for this task — it's verification only. If any step fails, return to the relevant task and fix before proceeding.

---

## Self-Review Notes (already applied above)

- Spec coverage: MeasureStage bug fix (Task 7 Step 4), QC Inline flow (Tasks 6-9), QC Inline Rate backend (Tasks 1-3), QC Inline Rate Web CRUD (Tasks 4-5), shortcut/access condition (Task 9 Step 2) — all covered.
- Deviated from the original spec text in one place, called out explicitly: replaced "DELETE endpoint" with `IsActive` toggle to match this codebase's established no-hard-delete Master Data convention; replaced "add i18n keys" with hardcoded Vietnamese strings since `/master` has no i18n at all today.
- Confirmed real type names/signatures by reading the actual source (`FaiCellDto.ByStage`, `IApiClient.GetAsync<T>`, `ProductWithSessionDto.StatusCode`, `ShortcutItem` constructor, `MasterItemDialog` switch pattern) rather than assuming — no placeholder code.
