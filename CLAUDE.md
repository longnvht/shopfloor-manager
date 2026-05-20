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
# Swagger UI:   http://localhost:5066/swagger
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

## Domain Model — Production Core

Đây là mô hình cốt lõi của hệ thống, được xây dựng từ phân tích nghiệp vụ thực tế tại xưởng gia công CNC.

### Sơ đồ tổng quan

```
PartNumber (loại sản phẩm)
  └── PartRev (phiên bản thiết kế: Rev A, B, C...)
        └── Routing (quy trình cho PartRev đó)
              └── RoutingRev (phiên bản quy trình: R1, R2...)
                    └── PartOp (công đoạn: 10, 20, 30...)
                          ├── TechDocument  (RouteCard, FixtureDrawing, ToolList)
                          ├── CNCProgram    (G-code file)
                          └── Dimension     (kích thước cần kiểm tra)
                                └── MeasureValue  (kết quả đo thực tế)

Job (lệnh SX)
  ├── PartRevId    → snapshot PartRev tại thời điểm phát lệnh
  ├── RoutingRevId → snapshot RoutingRev đang dùng (KHÔNG thay đổi dù routing sau cập nhật)
  ├── RunQty, ShipBy, POLine
  └── Product (serial: 001, 002, ..., N)
        └── MeasureValue (giá trị đo cho từng Dimension của từng serial)
```

### Các thực thể và quan hệ

**PartRev** — Phiên bản thiết kế sản phẩm
- Một `PartNumber` có nhiều `PartRev` (Rev A, B, C...)
- Mỗi `PartRev` có thể có nhiều `Routing` (trường hợp có nhiều phương án gia công)
- Thực tế thường chỉ có 1 Routing active per PartRev

**Routing / RoutingRev** — Quy trình gia công
- `Routing` là tập hợp các công đoạn (`PartOp`) để tạo ra một `PartRev`
- `RoutingRev` là phiên bản của Routing: thay đổi thứ tự, thêm/bớt công đoạn → tạo RoutingRev mới
- Chỉ một `RoutingRev` là `IsActive=true` tại một thời điểm per Routing

**PartOp** — Công đoạn gia công
- Thuộc về một `RoutingRev` cụ thể (KHÔNG phải thuộc Part trực tiếp)
- Có thể là `ForJobOnly=true` — OP bổ sung riêng cho một Job nhất định
- Mỗi OP có: `OpNumber` (10, 20...), `OpType` (CNC/GRIND...), `SetupTime`, `ProdTime`

**Dimension** — Kích thước cần kiểm tra
- Thuộc về một `PartOp` cụ thể (kiểm tra sau công đoạn đó)
- `BalloonNumber`: số bóng trên bản vẽ (ví dụ "Ø1", "L2", "Ra3") — tên theo drawing
- `Code`: mã nội bộ (ví dụ "D1", "L1")
- Lưu `Nominal`, `UpperTol`, `LowerTol` dạng DECIMAL(14,4) — không dùng VARCHAR
- `UpperLimit = Nominal + UpperTol`, `LowerLimit = Nominal + LowerTol`

**Job** — Lệnh sản xuất
- Tham chiếu cả `PartRevId` VÀ `RoutingRevId` → đây là **snapshot** tại thời điểm phát lệnh
- Nếu Routing thay đổi sau khi Job đã tạo, Job vẫn giữ nguyên RoutingRev cũ
- Routing của Job = `RoutingRev.PartOps` (template) + `PartOps ForJobOnly=true` (riêng job này)
- **KHÔNG copy PartOp vào Job** — query động từ RoutingRev

**MeasureValue** — Kết quả đo
- Gắn với: `DimensionId` (kích thước nào) + `ProductId` (serial nào) + `PartOpId` (công đoạn nào)
- `Result`: Pass(1) nếu `LowerLimit ≤ Value ≤ UpperLimit`, Fail(2) nếu ngoài dung sai
- Upsert — có thể đo lại, ghi đè giá trị cũ

### Business rules quan trọng

```
1. Tạo PartRev mới:
   → Deactivate PartRev cũ cùng PartNumber (hoặc giữ nguyên tất cả, chỉ mark active)

2. Tạo RoutingRev mới:
   → Deactivate RoutingRev cũ của Routing đó
   → Copy toàn bộ PartOps từ RoutingRev cũ sang RoutingRev mới
   → Người dùng chỉnh sửa trên RoutingRev mới

3. Tạo Job:
   → Chọn PartRev (active) + RoutingRev (active của Routing đó)
   → Lưu snapshot: job.PartRevId + job.RoutingRevId
   → KHÔNG copy PartOps — query từ RoutingRev khi cần

4. Routing của Job (query):
   → PartOps WHERE RoutingRevId = job.RoutingRevId  [template OPs]
   → UNION PartOps WHERE JobId = job.Id             [job-specific OPs]

5. Tạo Product:
   → Generate serials: 001, 002, ..., RunQty
   → Một Product per serial

6. Nhập MeasureValue:
   → Lấy Dimensions từ PartOps của Job (RoutingRev + ForJobOnly)
   → Upsert giá trị đo cho từng (DimensionId, ProductId)
   → Auto-calculate Pass/Fail vs LowerLimit/UpperLimit

7. Upload TechDocument:
   → Check 3 upload rules (xem bên dưới) trước khi accept
   → MinIO object key = {PartNumber}/{RevCode}/{RoutingRevCode}/{OpNumber}/{Folder}/{filename}
      hoặc {JobNumber}/{OpNumber}/{Folder}/{filename} (nếu IsJobNumber)
   → Sau upload thành công → Status = Pending, chờ Inspector duyệt
```

### TechDocument — FileType flags và Upload rules

`FileType` có các flags điều khiển naming convention và path:

```
IsSegment    — G-code có thể chia thành nhiều segment (upload cùng lúc, đủ N files)
IsJobNumber  — tên file/path bắt đầu bằng JobNumber
IsPartNumber — tên file/path bắt đầu bằng PartNumber
IsRevision   — path include RevCode
IsOpNumber   — path/tên file include OpNumber
```

**Naming convention:**
- PartNumber-based: `{PartNumber}-{RevCode}-{OpNumber}-{FileCode}.pdf`
- JobNumber-based: `{JobNumber}-{OpNumber}-{FileCode}.nc`
- Segmented G-code: `{...}-{FileCode}-{index}_{total}.nc` (ví dụ: `O0020-GC-1_3.nc`)

**MinIO object key structure:**
```
PartNumber-based: {PartNumber}/{RevCode}/{RoutingRevCode}/{OpNumber}/{Folder}/{filename}
JobNumber-based:  {JobNumber}/{OpNumber}/{Folder}/{filename}
```
→ RoutingRevCode nằm trong path — khi đổi RoutingRev phải upload lại file mới

**3 upload rules bắt buộc:**
```
Rule 1: BLOCK nếu Status=Approved → "File đã được approve"
        (kể cả creator cũng không sửa được — phải tạo RoutingRev mới)

Rule 2: BLOCK nếu Status=Pending + CreatedBy ≠ current user
        → "File đã được cập nhật bởi người khác"

Rule 3: ALLOW nếu Status=Rejected → rename file cũ thành "Rejected_{filename}"
        trên MinIO, upload file mới, reset Status=Pending
```

**Segment validation:**
- G-code file có segment (e.g. `1_3`) phải upload đủ cả 3 files cùng Code
- Nếu thiếu → tất cả files trong group bị mark Import=false

---

## Key Design Decisions

**Database:**
- PostgreSQL only — all logic in C#, no stored procedures
- `DECIMAL(14,4)` cho tất cả giá trị đo/kích thước — KHÔNG dùng VARCHAR (lỗi của legacy)
- `snake_case` cho tất cả tên bảng/cột
- Soft delete via `deleted_at TIMESTAMPTZ` trên các entity chính
- Schema managed by EF Core migrations — `init.sql` chỉ là reference

**Domain enums:**
```csharp
FileStatus:        Pending=0, Approved=1, Rejected=2
NcrAction:         Pending=0, Approve=1, Rework=2, Reject=3
NcrStatus:         Open=0, Closed=1
MeasureResult:     Pass=1, Fail=2       // 1-indexed để tương thích legacy
BorrowStatus:      Active=0, Returned=1, Cancelled=2
CalibRequestStatus:Pending=0, Approved=1, Completed=2, Cancelled=3
```

**Roles** (from `AppConstants.Roles`):
`Administrator`, `Manager`, `Engineer`, `QC Inspector`, `Operator`, `Planner`

**MinIO:** tất cả file trong bucket `shopfloor-storage`. Upload via pre-signed URL — client upload thẳng, API chỉ quản lý metadata.

**MQTT topics:** `factory/cnc/#` (all CNC data), `factory/cnc/{machineCode}/status` per machine.

---

## Project Status

*(cập nhật 2026-05-20)*

| Phase | Status |
|---|---|
| Phase 0 — Foundation (infrastructure, DB schema, .NET scaffold) | ✅ Done |
| Phase 1 — Auth & HR (JWT, users, roles, SignalR) | ✅ Done |
| Phase 2 — Production Core (Jobs, Parts, OPs, Documents) | 🔄 Refactoring |
| Phase 3 — Quality (Dimensions, FAI, NCR, SPC) | 🔄 Refactoring |
| Phase 4 — Desktop MES (WPF/MAUI, offline, FAI at machine) | ⏳ |
| Phase 5 — Advanced (Gage, Planning, MQTT pipeline, Dashboard) | ⏳ |

**Phase 1 — ✅ Hoàn tất** (2026-05-20)
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

**Phase 2+3 — Refactoring** (data model đúng hơn sau phân tích legacy)

Các vấn đề cần sửa:
- [ ] Tách `PartOp` ra khỏi `Part` — thêm `Routing` + `RoutingRev` entities
- [ ] `Part` model: `(PartNumber, Revision)` là identity, `IsActive` per PartNumber
- [ ] `Job` lưu snapshot `PartRevId` + `RoutingRevId`
- [ ] `GetJobOps` query: `RoutingRev.PartOps` UNION `Job.ForJobOnly.PartOps`
- [ ] `Dimension`: thêm `BalloonNumber` field (số bóng bản vẽ)
- [ ] `MeasureValue`: thêm `PartOpId` trực tiếp

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

**Production Core pattern (CRITICAL — phải theo đúng):**
- `PartOp` thuộc `RoutingRev`, KHÔNG thuộc `Part` trực tiếp
- `Job` phải lưu cả `PartRevId` và `RoutingRevId` (snapshot)
- Routing của Job = query động từ `RoutingRevId` + ForJobOnly OPs
- `Dimension.BalloonNumber` = số bóng trên bản vẽ (e.g. "Ø1", "L2")
- `MeasureValue` = upsert per (DimensionId, ProductId)

**Don't:**
- Put business logic in controllers or EF entities
- Add Python (Phase 0–5 are C# only)
- Hardcode credentials, URLs, or ports — use `appsettings.json` / env vars
- Copy logic from old WinForms source — use it only to understand business rules
- Store measurement values as VARCHAR — always DECIMAL(14,4)
