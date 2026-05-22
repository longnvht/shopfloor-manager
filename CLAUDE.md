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
        ├── TechDocument  (DRW, CAD — Part-level, gắn partRevId)
        └── Routing (quy trình cho PartRev đó)
              └── RoutingRev (phiên bản quy trình: R1, R2...)
                    └── PartOp (công đoạn: 10, 20, 30...)
                          ├── TechDocument  (GCD, TLS, CAM, THD — Standard OP docs)
                          ├── [ForJobOnly OP chỉ tồn tại trong 1 Job — RTC, FXT]
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
   → Xác định loại tài liệu (Part-level / Standard OP / ForJobOnly OP)
   → Check 3 upload rules trước khi accept
   → MinIO path theo loại (xem bên dưới)
   → Sau upload thành công → Status = Pending, chờ Inspector duyệt
```

### TechDocument — 3 loại theo chủ sở hữu

```
1. Part-level  (partRevId set, partOpId null)
   → DRW (bản vẽ 2D), CAD (file 3D)
   → Thuộc Part/Rev, tái dùng qua mọi Job
   → Quản lý từ: Parts → [Part] → "Bản vẽ/CAD"

2. Standard OP (partOpId set → OP có routingRevId, jobId null)
   → GCD, TLS, CAM, THD — thuộc công nghệ routing
   → Tái dùng qua mọi Job cùng routing
   → Quản lý từ: Parts → [Part] → OP → "Tài liệu →"

3. ForJobOnly OP (partOpId set → OP có jobId, forJobOnly=true)
   → Mọi loại tài liệu trên OP bất thường chỉ tồn tại 1 Job
   → Quản lý từ: Jobs → [Job] → Custom OPs → "Quản lý →"
   → RTC, FXT thường thuộc loại này (job-specific execution docs)
```

**FileType flags và MinIO path:**
```
FileType  isPartNumber  isOpNumber  isJobNumber  MinIO path
─────────────────────────────────────────────────────────────────────────────
DRW       true          false       false        drawings/{part}/{rev}/{file}
GCD       true          true        false        gcodes/{part}/{op}/{rev}/{file}
RTC       false         true        true         routecards/{job}/{op}/{file}
FXT       false         true        true         fixtures/{job}/{op}/{file}
THD       true          true        false        threads/{part}/{op}/{rev}/{file}
TLS       true          true        false        tools/{part}/{op}/{rev}/{file}
CAM       true          true        false        cam/{part}/{op}/{rev}/{file}
CAD       true          false       false        cad/{part}/{rev}/{file}
```

**3 upload rules bắt buộc:**
```
Rule 1: BLOCK nếu Status=Approved → "File đã được approve"
        (kể cả creator cũng không sửa được)

Rule 2: BLOCK nếu Status=Pending + CreatedBy ≠ current user
        → "File đang chờ duyệt bởi người khác"

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
| Phase 2 — Production Core (Jobs, Parts, OPs, Documents) | ✅ Done |
| Phase 3 — Quality (Dimensions, FAI, NCR, SPC) | ✅ Done |
| Phase 4 — Desktop MES (WPF, FAI at machine, SignalR) | 🔄 In progress |
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

**Phase 2 — ✅ Hoàn tất** (2026-05-20)
- Entities: Part, PartRev, Routing, RoutingRev, PartOp, Job (snapshot PartRevId+RoutingRevId), Product
- `CreateJob` tự động tạo Products theo RunQty
- API: `/api/v1/parts`, `/api/v1/jobs`, `/api/v1/operations`
- MinIO: TechDocument upload với pre-signed URL + 3 upload rules
- FileTypes: DRW, GCD, RTC, FXT, THD, TLS, CAM, CAD (theo tài liệu 05)

**Phase 3 — ✅ Hoàn tất** (2026-05-20)
- Dimension: BalloonNumber + BalloonSort, TolerancePlus/Minus (cả 2 dương), MaxValue/MinValue, IsTextType, CategoryId, IsFinal
- DimensionCategory: LIN, ANG, THD, GEO, SFC (seed)
- MeasureValue: KHÔNG upsert — tạo record mới mỗi lần đo (giữ lịch sử)
- NCR: format `NCR-{YY}-{NNNN}`, thêm ReasonId, DepartmentId, MachineCode
- NcrReason: seed 7 lý do (Tool wear, Setup error, Drawing error...)
- SPC: ISpcService + MathNet dùng MaxValue/MinValue

**Phase 4 — 🔄 In progress** (bắt đầu 2026-05-21)
- Project: `ShopfloorManager.Desktop` (WPF .NET 9, trong cùng solution)
- Spec: [`Project_Documents/14_desktop_mes.md`](Project_Documents/14_desktop_mes.md) — dựa trên phân tích Vinam-MES WinForms cũ
- Stack: WPF + CommunityToolkit.Mvvm + MaterialDesignThemes + SignalR.Client
- Skeleton đã có: DI (Microsoft.Extensions.DI), IApiClient (HttpClient+JWT), IAuthService, NavigationService, LoginWindow, MainWindow shell
- Per-machine config: `local.json` (gitignored) override `appsettings.json`
- ✅ JobListPage: search, ShowCompleted toggle, pagination 20/trang, overdue highlight, status badge
- ✅ OperationPage: danh sách OP dạng card, badge ForJobOnly/Complete, SetupTime/ProdTime, nút "Bắt đầu FAI", back về JobList
- ✅ Virtual Keyboard: NumPadWindow (số, floating no-focus), QwertyWindow (QWERTY + 123 panel, CapsLock toggle)
- ✅ Touch-optimized: Button 56px, TextBox 52px, DataGridRow 52px, KeyboardBehavior attached property
- ✅ ProductListPage: card grid 4 màu trạng thái (available/claimed/inprogress/complete), claim session
- ✅ ProductionSession backend: entity + migration + API (claim/start/complete/cancel)
- **Chưa implement:** FAIPage (với gage selection + NumPad), DocumentViewer, NCR dialog

**Ràng buộc ProductionSession (2 constraints):**
- Per-product: 1 product chỉ có 1 session open tại 1 thời điểm (không chọn ở 2 máy/OP cùng lúc)
- Per-machine: 1 máy chỉ gia công 1 product tại 1 thời điểm (không claim thêm khi đang có session open)

**FAI workflow (cần implement):**
1. Claim session → màn hình FAI
2. Nút "Bắt đầu" → PUT start → timer bắt đầu
3. Dimension card grid → tap card → BƯỚC 1: chọn Gage → BƯỚC 2: nhập giá trị (NumPad) → confirm
4. Khi tất cả dims đo xong → nút "Kết thúc" → PUT complete
5. Nếu Fail → dialog NCR

**Desktop MES — kiến trúc quan trọng:**
- KHÔNG kết nối DB trực tiếp — chỉ qua REST API
- JWT token lưu in-memory (không persist ra disk)
- Window orchestration trong `App.xaml.cs` (NavigationService.Navigated event)
- `local.json` chứa: ApiBaseUrl, MachineCode, MachineName — khác nhau giữa các máy tại xưởng
- `HttpClient` + `IApiClient` phải là **singleton** — nếu transient, mỗi ViewModel nhận instance riêng và không có token
- Trigger data load từ ViewModel (NavigateTo command), KHÔNG dùng `Loaded` event của View — tránh race condition DataContext timing
- Khi implement API call mới: luôn kiểm tra field name của request/response khớp đúng với API contract (dùng Swagger hoặc curl để verify trước)
- **`Run.Text` binding trong WPF mặc định TwoWay** — computed/read-only properties trên record phải dùng `Mode=OneWay`: `{Binding PropName, Mode=OneWay}`
- Khi thêm child element vào XAML tag đang có attributes (như DataGrid.InputBindings), các attributes còn lại phải nằm trong tag mở `<Tag attr1="" attr2="">`, không được để lơ lửng sau closing `>`
- Virtual keyboard dùng `WS_EX_NOACTIVATE` để không steal focus — TextBox vẫn giữ focus khi gõ phím
- Keyboard label và output phải nhất quán từ đầu: gọi `UpdateLetterKeys(panel, caps: false)` trong `Loaded` để sync label với trạng thái mặc định

---

## Rules for Claude

**Always ask before:**
- Changing DB schema (EF Core migrations are hard to rollback cleanly)
- Adding a NuGet package (must be MIT/Apache 2.0, must have a clear reason)
- Restructuring directories

**Quy trình mỗi tính năng (Desktop MES):**
1. Viết code
2. Build (`dotnet build`) — phải 0 error trước khi báo xong
3. Chạy app thực tế, kiểm tra bằng tay
4. Fix bug nếu có
5. Update CLAUDE.md (progress + bài học)
6. Commit + push GitHub

---

### Triển khai tính năng — quy trình bắt buộc

**Bước 0 — ĐỌC TÀI LIỆU TRƯỚC KHI CODE:**

Mỗi module có file tài liệu trong `Project_Documents/`. Trước khi implement bất kỳ tính năng nào, **phải đọc file tương ứng** để nắm đúng business logic:

| Module | Tài liệu |
|---|---|
| Auth, Login, Permissions | [`Project_Documents/01_auth.md`](Project_Documents/01_auth.md) |
| Users, HR, Departments | [`Project_Documents/02_hr.md`](Project_Documents/02_hr.md) |
| Job, Part, Product serial | [`Project_Documents/03_job_management.md`](Project_Documents/03_job_management.md) |
| OP, Routing, Technology | [`Project_Documents/04_routing_operations.md`](Project_Documents/04_routing_operations.md) |
| Tech Documents, Upload, Approval | [`Project_Documents/05_technical_documents.md`](Project_Documents/05_technical_documents.md) |
| Dimensions, FAI, Measure values | [`Project_Documents/06_dimensions_fai.md`](Project_Documents/06_dimensions_fai.md) |
| NCR, CPAR, Rework | [`Project_Documents/07_ncr.md`](Project_Documents/07_ncr.md) |
| Gage, Borrow/Return | [`Project_Documents/08_gage_management.md`](Project_Documents/08_gage_management.md) |
| Calibration, Vendors, Procedures | [`Project_Documents/09_calibration.md`](Project_Documents/09_calibration.md) |
| Planning, Gantt, Shifts | [`Project_Documents/10_planning.md`](Project_Documents/10_planning.md) |
| Dashboard, Reports, PDF/Excel | [`Project_Documents/11_dashboard_reports.md`](Project_Documents/11_dashboard_reports.md) |
| CNC Data, MQTT, SignalR | [`Project_Documents/12_cnc_mqtt.md`](Project_Documents/12_cnc_mqtt.md) |
| Master data (Machine, Factory...) | [`Project_Documents/13_master_data.md`](Project_Documents/13_master_data.md) |
| Desktop MES (WPF, FAI at machine) | [`Project_Documents/14_desktop_mes.md`](Project_Documents/14_desktop_mes.md) |
| Desktop MES — Dashboard UI | [`Project_Documents/15_dashboard_desktop.md`](Project_Documents/15_dashboard_desktop.md) |

**Tài liệu là nguồn sự thật duy nhất về business logic.** Nếu code cũ (ManageData, Vinam-MES) và tài liệu mâu thuẫn → ưu tiên tài liệu.

**Bước 1–6 — Implement theo Clean Architecture:**
1. Đọc tài liệu module → xác định Entity, Business Rules, Workflow, Edge Cases
2. Define entity trong `Domain` extending `BaseEntity` hoặc `SoftDeletableEntity`
3. Define command/query + handler trong `Application` (MediatR) — toàn bộ business logic ở đây
4. Define repository interface trong `Application`, implement trong `Infrastructure`
5. Add thin controller trong `API` — chỉ gọi `_mediator.Send(request)`
6. Add EF migration: `dotnet ef migrations add {Name} ...`
7. Add OpenAPI/Swagger annotation cho tất cả endpoint mới

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
