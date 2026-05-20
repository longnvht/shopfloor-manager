# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Shopfloor Manager is an open-source factory management system for CNC machining shops, replacing a legacy WinForms (DevExpress) system. Solo project — 1 developer. Prioritize **simple and maintainable** over clever.

---

## Dev Commands

```bash
# 1. Start infrastructure (PostgreSQL + MinIO + Mosquitto — Docker only)
docker compose -f docker-compose.dev.yml up -d

# 2. Run the API (from repo root or src/)
cd src
dotnet run --project ShopfloorManager.API

# API:          http://localhost:5066
# Swagger UI:   http://localhost:5066/swagger  (once configured)
# MinIO:        http://localhost:9001  (minioadmin / minioadmin123)
# PostgreSQL:   localhost:5432  (shopfloor / dev_password / shopfloor_dev)
# MQTT:         localhost:1883

# Build solution
dotnet build src/ShopfloorManager.sln

# Run tests
dotnet test src/ShopfloorManager.sln

# EF Core migrations (run from src/ — required after any entity change)
dotnet ef migrations add {MigrationName} --project ShopfloorManager.Infrastructure --startup-project ShopfloorManager.API
dotnet ef database update --project ShopfloorManager.Infrastructure --startup-project ShopfloorManager.API
```

> The dev compose has **no auth** — PostgreSQL credentials are hardcoded (`shopfloor` / `dev_password`). Production uses `.env` (copy from `.env.example`).

---

## Architecture

Clean Architecture with 4 layers. **Dependency direction: API → Application → Domain ← Infrastructure**.

```
ShopfloorManager.API            # Controllers, middleware, Program.cs, DI composition
ShopfloorManager.Application   # MediatR commands/queries, FluentValidation, DTOs, interfaces
ShopfloorManager.Domain        # Entities, enums — no framework dependencies
ShopfloorManager.Infrastructure # EF Core DbContext, MinIO, MQTT, MailKit, repositories
ShopfloorManager.Shared        # PagedResult<T>, AppConstants, enums shared across boundaries
```

**Dependency rules enforced by .csproj references:**
- `Domain` → `Shared` only
- `Application` → `Domain` + `Shared`
- `Infrastructure` → `Application` + `Domain` (implements Application interfaces)
- `API` → `Application` + `Infrastructure` + `Shared` (composition root only)

### Request flow

```
HTTP Request
  → Controller (thin — only calls IMediator.Send)
  → MediatR Handler (in Application layer — all business logic lives here)
  → Repository/Service interfaces (defined in Application, implemented in Infrastructure)
  → EF Core / MinIO / MQTT
```

No logic in controllers. No stored procedures or DB triggers — business logic 100% in Application handlers.

### Base types (Domain layer)

```csharp
// All tables use surrogate int PK + audit fields
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}

// Soft-delete entities add:
public abstract class SoftDeletableEntity : BaseEntity
{
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
}
```

### Standard API response shape

```json
{ "success": true, "data": {}, "error": null,
  "pagination": { "page": 1, "pageSize": 20, "total": 100 } }
```

`PagedResult<T>` is defined in `ShopfloorManager.Shared/Pagination/`.

---

## Key Design Decisions

**Database:**
- PostgreSQL only, no MySQL stored procedures — all logic in C#
- `DECIMAL(14,4)` for all measurement values — never store as VARCHAR
- `snake_case` for all table/column names
- Soft delete via `deleted_at TIMESTAMPTZ` on main entities
- Full schema lives in `docker/postgres/init.sql` — EF Core mirrors this via migrations

**Domain enums** (match PostgreSQL ENUM values):
```csharp
FileStatus:        Pending=0, Approved=1, Rejected=2
NcrAction:         Pending=0, Approve=1, Rework=2, Reject=3
NcrStatus:         Open=0, Closed=1
MeasureResult:     Pass=1, Fail=2       // Note: 1-indexed (legacy)
BorrowStatus:      Active=0, Returned=1, Cancelled=2
CalibRequestStatus:Pending=0, Approved=1, Completed=2, Cancelled=3
```

**Roles** (from `AppConstants.Roles`):
`Administrator`, `Manager`, `Engineer`, `QC Inspector`, `Operator`, `Planner`

**MinIO:** all files stored in bucket `shopfloor-storage`. Upload via pre-signed URL — client uploads direct, API only handles metadata.

**MQTT topics:** `factory/cnc/#` (all CNC data), `factory/cnc/{machineCode}/status` per machine.

---

## Project Status

| Phase | Status |
|---|---|
| Phase 0 — Foundation (infrastructure, DB schema, .NET scaffold) | ✅ Done |
| Phase 1 — Auth & HR (JWT, users, roles, SignalR) | 🔄 Next |
| Phase 2 — Production Core (Jobs, Parts, OPs, Documents) | ⏳ |
| Phase 3 — Quality (Dimensions, FAI, NCR, SPC) | ⏳ |
| Phase 4 — Desktop MES (WPF/MAUI, offline, FAI at machine) | ⏳ |
| Phase 5 — Advanced (Gage, Planning, MQTT pipeline, Dashboard) | ⏳ |

**Current state:** `Program.cs` is a placeholder (`/weatherforecast` endpoint). Application/Infrastructure/Domain projects have stub `Class1.cs` files. EF Core DbContext and migrations have not been created yet.

**Phase 1 starting point:**
1. Create EF Core `DbContext` + entity configurations in Infrastructure
2. `dotnet ef migrations add InitialSchema`
3. Auth: JWT login endpoint, refresh token, middleware
4. CRUD for Users + Roles

---

## Rules for Claude

**Always ask before:**
- Changing DB schema (EF Core migrations are hard to rollback cleanly)
- Adding a NuGet package (must be MIT/Apache 2.0, must have a clear reason)
- Restructuring directories

**When adding a new feature:**
1. Define entity in `Domain` extending `BaseEntity` or `SoftDeletableEntity`
2. Define command/query + handler in `Application` (MediatR)
3. Define repository interface in `Application`, implement in `Infrastructure`
4. Add thin controller in `API` — only `_mediator.Send(request)`
5. Add EF migration: `dotnet ef migrations add {Name} ...`
6. Add OpenAPI/Swagger annotation to all new endpoints

**Don't:**
- Put business logic in controllers or EF entities
- Add Python (Phase 0–5 are C# only)
- Hardcode credentials, URLs, or ports — use `appsettings.json` / env vars
- Copy logic from old WinForms source — use it only to understand business rules
