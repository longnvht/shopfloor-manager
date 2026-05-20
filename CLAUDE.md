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

*(cập nhật 2026-05-20)*

| Phase | Status |
|---|---|
| Phase 0 — Foundation (infrastructure, DB schema, .NET scaffold) | ✅ Done |
| Phase 1 — Auth & HR (JWT, users, roles, SignalR) | ✅ Done |
| Phase 2 — Production Core (Jobs, Parts, OPs, Documents) | ✅ Done |
| Phase 3 — Quality (Dimensions, FAI, NCR, SPC) | ✅ Done |
| Phase 4 — Desktop MES (WPF/MAUI, offline, FAI at machine) | 🔄 Tiếp theo |
| Phase 5 — Advanced (Gage, Planning, MQTT pipeline, Dashboard) | ⏳ |

**Phase 1 — đã xong:**
- EF Core `ShopfloorDbContext` + 9 entities (User, Role, Department, UserType, Position, WorkStatus, Menu, RoleMenu, AuditLog)
- Migration `InitialSchema` — seed 6 roles, 4 departments, 3 work statuses
- `DbSeeder` tạo `admin/Admin@123` khi DB trống
- `POST /api/v1/auth/login` → JWT token (8h)
- `POST /api/v1/auth/forgot-password` + `POST /api/v1/auth/reset-password` (MailKit)
- `GET|POST|PUT /api/v1/users` — phân trang, role-based, update, change password
- `GET|POST|PUT /api/v1/roles`, `/api/v1/departments`
- `GET|POST /api/v1/positions`, `/api/v1/user-types`; `GET /api/v1/work-statuses`
- SignalR hub tại `/hub/shopfloor` (auto-join group theo role)
- `ValidationBehavior` MediatR pipeline, `ExceptionMiddleware`, Swagger + JWT

**Phase 1 — ✅ Hoàn tất** (2026-05-20)

**Phase 2 — ✅ Hoàn tất** (2026-05-20)

**Phase 3 — ✅ Hoàn tất** (2026-05-20)
- Entities: Dimension (BIGSERIAL, DECIMAL(14,4)), MeasureValue, Ncr, NcrLog
- Migration: `Phase3_Quality`
- SPC: ISpcService + SpcService (MathNet.Numerics) — Cp, Cpu, Cpl, Cpk
- API: `GET|POST|PUT /api/v1/operations/{opId}/dimensions`
- API: `GET /api/v1/operations/{opId}/dimensions/{id}/spc`
- API: `GET /api/v1/fai?partOpId=&jobId=`, `POST /api/v1/fai/measure`
- API: `GET|POST /api/v1/ncrs`, `GET /api/v1/ncrs/{id}`, `POST /api/v1/ncrs/{id}/actions`
- Web: `/jobs/[id]/fai?opId=` — bảng đo FAI spreadsheet-style (blur/Enter to save)
- Web: `/ncrs` — danh sách NCR, lọc Open/Closed
- Navbar: thêm link NCR
- Entities: Part (SoftDeletable), Job, PartOp, Product, OpType, PoLine, FileType, TechDocument
- Migration: `Phase2_ProductionCore` — seed 6 OpTypes, 5 FileTypes
- API: `GET|POST|PUT /api/v1/parts`, `GET|POST|PUT /api/v1/jobs`
- Nested: `GET /api/v1/jobs/{id}/operations`, `GET|POST /api/v1/jobs/{id}/products/generate`
- `GET|POST /api/v1/operations`
- `GET|POST /api/v1/tech-documents`, `GET /{id}/download-url`, `PUT /{id}/inspect`
- MinIO: pre-signed URL upload/download, `IMinioService` + `MinioService`
- Web: `/jobs` (list + search + tạo mới), `/jobs/[id]` (detail: OPs + serials)
- Navbar: link Jobs, Dashboard

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
