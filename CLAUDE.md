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
`Administrator`, `Manager`, `Engineer`, `QC Inspector`, `Operator`, `Planner`, `Leader`

**Role phân quyền Desktop MES (ProductionSession):**
- `Operator`: chỉ tạo và kết thúc session của chính mình
- `Leader`: có thể force-finish session của Operator bất kỳ trên cùng máy
- `Administrator`: quyền tương đương Leader + access Settings page
- Các role khác (`Engineer`, `QC Inspector`, `Manager`, `Planner`): View_Mode only khi máy đang có session của người khác

**MinIO:** tất cả file trong bucket `shopfloor-storage`. Upload via pre-signed URL — client upload thẳng, API chỉ quản lý metadata.

**MQTT topics:** `factory/cnc/#` (all CNC data), `factory/cnc/{machineCode}/status` per machine.

---

## Project Status

*(cập nhật 2026-05-26)*

| Phase | Status |
|---|---|
| Phase 0 — Foundation (infrastructure, DB schema, .NET scaffold) | ✅ Done |
| Phase 1 — Auth & HR (JWT, users, roles, SignalR) | ✅ Done |
| Phase 2 — Production Core (Jobs, Parts, OPs, Documents) | ✅ Done |
| Phase 3 — Quality (Dimensions, FAI, NCR, SPC) | ✅ Done |
| Phase 4 — Desktop MES (WPF, FAI at machine, SignalR) | ✅ Done |
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
- ✅ Virtual Keyboard light theme: nền cam kem #FFF8F0, viền bo nâu #A0522D, Caps ON cam #E65100
- ✅ ProductListPage: card grid 4 màu trạng thái (available/claimed/inprogress/complete), claim session
- ✅ ProductionSession backend: entity + migration + API (claim/start/complete/cancel)
- ✅ WorkContext singleton: chia sẻ Job/OP/Product/Session state giữa tất cả pages
- ✅ Dashboard: layout 4 rows (TitleBar / Machine+Operator / WorkInfo / Utilities) cho 10" 16:9
- ✅ JobListPage/OperationPage/ProductListPage redesign: TitleBar + Search Bar + Card Grid + BottomBar
- ✅ Design Language thống nhất: `16_design_language.md` — màu sắc, component, navigation pattern
- ✅ Dashboard Work Info buttons: CanNavigate / CanStart / CanStop — hiển thị đúng theo trạng thái session
- ✅ Dashboard Utilities: thêm "Chọn OP", card fill chiều cao còn lại, ScrollViewer cuộn dọc
- ✅ Virtual keyboard tự đóng khi chuyển màn hình (MainViewModel inject IKeyboardService, gọi Hide() trước mỗi Navigate)
- ✅ FAIPage (Bảng đo): split layout 55/45, dimension card grid (xám/xanh/đỏ), NumPad cho số, PASS/FAIL cho text, POST `/api/v1/fai/measure`, auto-advance sang dim tiếp theo
- ✅ Shortcuts cập nhật: "Bảng đo" (khi HasProduct), "Cài đặt" (khi Admin), "Xem G-code" thay "Load G-code"
- ✅ Virtual keyboard drag: drag handle strip (⠿ icon, 22px) ở đầu mỗi keyboard — kéo được mà không mất focus TextBox
- ✅ NCR dialog: department chip + reason ComboBox + tùy chọn "Khác" (yêu cầu mô tả), POST `/api/v1/ncrs`
- ✅ NcrReasons seed data: 15 lý do gắn DepartmentId (PROD×6, QC×3, ENG×5, null×1)
- ✅ DragScrollBehavior: attached property `kb:DragScrollBehavior.Enabled` — drag-to-scroll trên JobList/OP/Product/FAI pages
- ✅ Operation_Mode/View_Mode: AppMode enum trong WorkContext; login flow check active session → set mode + restore WorkContext nếu resume; View_Mode: navigation không ghi WorkContext, ProductListPage hiện "Xem sản phẩm →" thay "Lựa chọn →"
- ✅ session resume on login: GET /api/v1/machines/{code}/active-session → reconstruct minimal DTOs → SetJob/SetOp/SetProduct
- ✅ force-finish session: PUT /api/v1/production-sessions/{id}/force-complete (role Leader/Manager/Admin); DashboardPage hiện "Kết thúc phiên" button
- ✅ ClaimedBy FK: ProductionSession.ClaimedBy → Users (thay shadow `CancelledByUserId`); ProductionSessionConfiguration Fluent API
- ✅ Leader role: thêm vào AppConstants.Roles + DB seed (Id=7)
- ✅ VIEW MODE toggle chip: luôn visible trên TitleBar (kể cả forced View_Mode), DataTrigger styling — Operation: BrandPrimary bg, View: gray; `ToggleModeCommand` trong DashboardViewModel
- ✅ WorkContext dual context: `ViewJob/ViewOp/ViewProduct` slot hoàn toàn độc lập với `CurrentJob/Op/Product`; context giữ nguyên khi toggle (OnModeChanged KHÔNG clear view context, chỉ clear khi logout)
- ✅ DashboardViewModel mode-aware: `CtxJob/CtxOp/CtxProduct` helpers đọc đúng slot theo mode; Work Info card hiển thị thông tin đúng khi toggle mode
- ✅ View Mode context persistence: view context giữ nguyên khi toggle về Operation Mode rồi toggle lại
- ✅ View Mode product selection: ProductListPage IsViewMode=true → chọn product, set ViewProduct, không tạo session
- ✅ FAI one-time entry (IsInputLocked): dimension đã đo hiện giá trị cũ, lock re-entry; TextBox IsEnabled=false (disabled hoàn toàn), amber notice banner, CanConfirm/CanSetPass/CanSetFail guard
- ✅ Work Info button mutual exclusion: ShowSelectJobButton + ShowNavigateButton gate on !CanForceFinish — tại một thời điểm chỉ 1 nút visible trong 5: SelectJob / Navigate / Start / Stop / ForceFinish
- ✅ Non-admin users login: bỏ FirstLogin check trong LoginViewModel Desktop — Desktop không redirect đổi mật khẩu
- ✅ FAI session started guard: NavigateToFai kiểm tra `_work.ActiveSession?.StartedAt.HasValue != true` → NavigateToDashboard; shortcut "Bảng đo" chỉ visible khi Operation Mode + session đã started
- ✅ DocumentViewer — G-code text viewer: `DocumentViewerPage` + `DocumentViewerViewModel` + `GcodeViewerBehavior` (syntax highlight N/G/M/axis/feed/tool/comment); `HexToBrushConverter` cho badge màu; auto-select G-code doc; shortcuts "Xem G-code"/"Hướng dẫn CW"/"Xem bản vẽ"/"Hướng dẫn gá" đều route về DocumentViewer
- ✅ DocumentViewer — PDF viewer: WebView2 (Microsoft.Web.WebView2 1.0.3967.48); Edge render PDF native; `IsPdfViewerVisible = IsNonGcodeSelected && PdfUrl != null && !IsLoadingContent`; `IsVisibleChanged` event khởi tạo WebView2 lần đầu + navigate; MinIO presigned URL navigated directly
- ✅ Session constraint redesign: Claim = client-side only (WorkContext, không ghi DB); chỉ `BeginSession` ghi DB (tạo + start atomically); ràng buộc per-machine chỉ áp dụng khi inprogress (`started_at IS NOT NULL`)
- ✅ Shortcut lock khi inprogress: Operation Mode + IsWip → "Chọn Job/OP/Sản phẩm" disabled (opacity 0.4), View Mode → re-enable
- ✅ Settings page (Admin): ApiBaseUrl, MachineCode, MachineName — edit + test connection + save to `local.json`; URL đổi → cần restart app

**Ràng buộc ProductionSession (thiết kế mới 2026-05-27):**
- **Claim = client-side only**: chọn product → `_work.SetProduct(product, null)` — KHÔNG ghi DB
- **Per-product inprogress**: block nếu product đã có session `open + started_at IS NOT NULL` ở máy khác
- **Per-machine inprogress**: block nếu máy đã có session `open + started_at IS NOT NULL`
- **BeginSession** (POST `/api/v1/production-sessions`): tạo session + set `started_at` ngay, check 2 constraints trên

**FAI workflow (đã implement):**
1. Chọn product → `_work.SetProduct(product, null)` — Dashboard hiện nút "Bắt đầu"
2. Nút "Bắt đầu" → POST `/api/v1/production-sessions` → tạo + start atomically → timer chạy
3. Shortcut "Bảng đo" → FAIPage: dimension card grid → tap card → NumPad nhập số / PASS·FAIL cho text → confirm → auto-advance
4. Khi tất cả dims đo xong → Dashboard nút "Kết thúc" → PUT complete
5. Nếu Fail → NCR dialog (đã implement)

**Operation_Mode / View_Mode — thiết kế (✅ implemented):**

Hai mode giải quyết các vấn đề: operator browse hồ sơ mà không ảnh hưởng session đang chạy; user B login khi máy đang được dùng bởi user A.

```
Operation_Mode                         View_Mode
────────────────────────────────────   ────────────────────────────────────
WorkContext operation slot ACTIVE       WorkContext operation slot FROZEN
Dashboard hiện Work Info + timer        Dashboard ẩn Work Info
Navigation GHI WorkContext              Navigation KHÔNG ghi WorkContext
Claim/Start/Stop session                Chỉ đọc / xem hồ sơ
```

**Login flow — xác định mode tự động:**
```
Sau login → GET /api/v1/production-sessions/active?machineCode=X

Không có session active trên máy:
  → Operation_Mode (mọi role)

Session của chính mình trên máy:
  → Operation_Mode (resume — khôi phục WorkContext Job/OP/Product/Session)

Session của người khác trên máy:
  → Role là Leader hoặc Admin  → Operation_Mode
  │    Dashboard hiện: "[Tên A] đang gia công [serial]"
  │    Có button "Kết thúc thay [Tên A]" (force-finish)
  │    Sau force-finish → session clear, máy tự do
  └─ Role là Operator (và các role khác) → View_Mode (forced, không toggle được)
       Dashboard hiện thông báo "Máy đang được sử dụng bởi [Tên A]"
```

**Mode toggle (manual):**
- Toggle chip trên TitleBar: **luôn visible** (kể cả khi forced View_Mode — user vẫn thấy trạng thái hiện tại)
- Operator có session của mình → có thể toggle sang View_Mode để browse hồ sơ → toggle về Operation_Mode, context cũ còn nguyên
- TitleBar màu khác khi View_Mode: DataTrigger `IsViewMode=True` → gray background (không phải BrandPrimary)
- View context độc lập: `WorkContext.ViewJob/ViewOp/ViewProduct` — hoàn toàn tách biệt với `CurrentJob/Op/Product`
- `DashboardViewModel.CtxJob/CtxOp/CtxProduct` → computed helpers đọc đúng slot: `IsViewMode ? ViewJob : CurrentJob`

**Phân quyền force-finish:**
- Chỉ `Leader` và `Administrator` có button "Kết thúc thay"
- Thực hiện từ chính máy đang có session đó (không remote)
- API: `PUT /api/v1/production-sessions/{id}/force-complete` (yêu cầu role Leader/Admin)

**API endpoints (✅ implemented):**
- `GET /api/v1/machines/{machineCode}/active-session` — trả về `ActiveSessionDto?` đang active trên máy + thông tin user, dùng cho login check
- `PUT /api/v1/production-sessions/{id}/force-complete` — Leader/Manager/Admin force-finish session của người khác
- Begin: `POST /api/v1/production-sessions` nhận `BeginSessionRequest(ProductId, PartOpId, MachineCode)`; tạo + start atomically, check per-product/per-machine inprogress; server inject `UserId` từ JWT

**Desktop changes:**
- `WorkContext`: thêm `AppMode` enum (`Operation` | `View`) + `ViewJob/ViewOp/ViewProduct` slots + `HasViewJob/Op/Product` computed + `SetViewJob/Op/Product` + `ClearViewContext()`; `OnModeChanged` KHÔNG clear view context
- `LoginViewModel`: sau login gọi active-session API, set mode + khôi phục WorkContext nếu resume; KHÔNG check FirstLogin
- `MainViewModel`: mode-aware navigation — khi View_Mode gọi `_work.SetViewJob/Op/Product`; khi Operation Mode gọi `_work.SetJob/Op/Product`; `_browseJob/_browseOp` private state để truyền context giữa các pages trong View Mode
- `DashboardViewModel`: `CtxJob/CtxOp/CtxProduct` helpers; `ToggleModeCommand`; Work Info hiển thị context đúng mode; `ShowSelectJobButton`, `ShowNavigateButton`, `ShowStopButton` mutual exclusion; shortcut "Bảng đo" guard `canFai`
- `FaiViewModel`: `IsInputLocked`, `OnSelectedDimensionChanged` restore, `CanConfirm/CanSetPass/CanSetFail` guard
- `DashboardPage.xaml`: toggle chip luôn visible (DataTrigger styling); button visibility bindings cập nhật

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
- **WorkContext** là singleton ObservableObject — inject vào mọi ViewModel cần đọc/ghi Job/OP/Product/Session
- **TextBlock.Text binding** read-only property cũng phải `Mode=OneWay` (không chỉ `Run.Text`)
- **`Border` chỉ nhận 1 child** — khi có nhiều state panels, phải wrap trong `<Grid>` bên trong Border
- **DispatcherTimer** dùng cho clock/elapsed time trong WPF — khởi tạo trong ViewModel, `Stop()` khi cleanup
- Dashboard là màn hình chính sau login — không dùng sidebar, mọi navigation từ WorkInfo card + shortcuts
- **Design Language**: xem [`Project_Documents/16_design_language.md`](Project_Documents/16_design_language.md) — màu sắc, component, pattern, checklist khi thêm màn hình mới
- Sub-page layout chuẩn: TitleBar(52) / SearchBar(60) / Cards(*) / BottomBar(64)
- Card selection: `ListBox + ItemContainerStyle` với trigger `IsSelected` → BrandPrimary border 3px + BrandAccentLight bg
- "Lựa chọn" button ở BottomBar: enabled khi có item selected, disabled khi không
- **Dashboard Work Info button logic**: `CanNavigate = HasJob && ActiveSession == null` (Tiếp tục); `CanStart = IsWip && !StartedAt`; `CanStop = IsWip && StartedAt` — 3 trạng thái loại trừ nhau
- **ProductListViewModel claim flow**: sau claim, gọi `_work.SetProduct(product, session)` TRƯỚC khi invoke `OnProductSelected` callback — callback trong MainViewModel chỉ gọi `NavigateToDashboard()`, KHÔNG gọi SetProduct lại (sẽ xóa session)
- **Keyboard auto-hide**: inject `IKeyboardService` vào MainViewModel, gọi `_keyboard.Hide()` đầu mỗi `NavigateTo*` method
- **Keyboard drag (no-focus window)**: dùng `ReleaseCapture()` + `SendMessage(hwnd, WM_NCLBUTTONDOWN, HTCAPTION, 0)` trong `MouseLeftButtonDown` của drag handle — kéo được mà `WS_EX_NOACTIVATE` vẫn giữ focus TextBox; KHÔNG dùng `DragMove()` vì nó yêu cầu window activate
- **Floating window có dynamic content**: dùng `SizeToContent="Height"` thay `Height` cố định — tránh bị cắt khi thêm/bớt element; gọi `PositionBottomRight()` trong `Loaded` event (sau khi `ActualHeight` đã xác định) thay vì trong constructor
- **Dashboard Utilities card**: dùng `Grid` 2 rows (Auto + *) bên trong Border để card fill chiều cao; bọc ItemsControl trong `ScrollViewer` với `PanningMode="VerticalFirst"`
- **FAIPage layout**: split 55% card grid / 45% input panel — dùng `Grid.ColumnDefinitions` với `0.55*` và `0.45*`; divider là `Border Width=1 Background=#E8D5C4`
- **DimensionCardVm**: `ObservableObject` riêng với `[NotifyPropertyChangedFor]` trên `State` → tự notify `IsMeasured`, `StateLabel`; màu card dùng `DataTrigger` trong `ItemContainerStyle` (không bind color từ VM)
- **FAI API route**: `GET /api/v1/fai?jobId=&partOpId=` → `FaiSheetDto`; `POST /api/v1/fai/measure` → `MeasureValueDto`; field `ProductId` khớp với `ProductWithSessionDto.ProductId` (không phải `.Id`)
- **Text dimension** (`IsTextType=true`): PASS/FAIL button auto-save ngay (không cần bước confirm riêng); gửi `ManualResult=true/false`, `Value=null`
- **WrapPanel trong ListBox**: set `ScrollViewer.HorizontalScrollBarVisibility="Disabled"` trên ListBox, bọc ListBox trong `ScrollViewer` ngoài để scroll dọc
- **Shortcut "Cài đặt"**: chỉ hiện cho role `Administrator` — `always: true` nhưng check role trước khi gọi `Add()`
- **DragScrollBehavior**: attached property `kb:DragScrollBehavior.Enabled="True"` trên outer `ScrollViewer` — nhận `PreviewMouseMove`, capture mouse khi drag > 8px, scroll bằng `ScrollToVerticalOffset`; state lưu per-instance qua DependencyProperty (không dùng static field)
- **AppMode (Operation/View)**: `WorkContext.AppMode` quyết định behavior của `MainViewModel.NavigateTo*` — khi View_Mode KHÔNG gọi `_work.SetJob/SetOp/SetProduct`, bỏ qua WorkContext guards; DashboardViewModel ẩn Work Info section khi View_Mode
- **Session resume**: sau login thành công, gọi `GET /api/v1/machines/{code}/active-session` trước khi navigate → nếu `session.ClaimedBy == auth.UserId` thì reconstruct minimal DTOs từ `ActiveSessionDto` rồi gọi `_work.SetJob/SetOp/SetProduct` → vào Dashboard với WorkContext đã restore
- **BeginSessionRequest**: Desktop POST body chỉ cần `ProductId, PartOpId, MachineCode` — server inject `UserId` từ JWT. Controller dùng `BeginSessionRequest` record riêng để tránh expose `UserId` field trong API contract. `BeginSessionHandler` tạo session + set `started_at` atomically trong 1 transaction
- **ProductionSessionConfiguration**: `HasForeignKey(s => s.ClaimedBy)` + `HasForeignKey(s => s.CancelledBy)` — map explicit int FK props thay shadow properties. Nếu thiếu config này, EF tạo shadow `ClaimedByUserId` và `CancelledByUserId`, khiến Include navigation luôn null
- **Force-finish**: chỉ Leader/Admin; thực hiện từ máy đang có session đó; sau force-finish máy tự do, user có thể bắt đầu session mới
- **Desktop FirstLogin**: Desktop app KHÔNG redirect đổi mật khẩu khi `FirstLogin=true` — LoginViewModel bỏ qua check đó và navigate thẳng MainViewModel. FirstLogin chỉ xử lý trên Web app.
- **WorkContext dual context**: `ViewJob/ViewOp/ViewProduct` là slot độc lập cho View Mode. `OnModeChanged` KHÔNG gọi `ClearViewContext()` — view context giữ nguyên khi toggle; chỉ clear khi `Clear()` (logout). `DashboardViewModel.CtxJob/CtxOp/CtxProduct` đọc đúng slot dựa trên mode.
- **FAI IsInputLocked**: `SelectedDimension?.IsMeasured == true` → lock mọi input. `OnSelectedDimensionChanged` restore giá trị đã đo vào InputValue. `CanConfirm/CanSetPass/CanSetFail` return `false` khi locked. FaiPage.xaml: TextBox `IsEnabled="{Binding IsInputEnabled}"` (disabled hoàn toàn — grayed out, không focus, NumPad không mở) + amber notice banner. `IsInputEnabled = !IsInputLocked` trong FaiViewModel với `[NotifyPropertyChangedFor]`.
- **Work Info button mutual exclusion**: 5 nút không đồng thời: SelectJob (`ShowSelectJobButton = !HasWork && !CanForceFinish`), Navigate (`ShowNavigateButton = CanNavigate && !CanForceFinish`), Start (`CanStart`), Stop (`ShowStopButton = CanStop && !CanForceFinish`), ForceFinish (`CanForceFinish`). Tại mọi thời điểm, nhiều nhất 1 nút visible.
- **Settings page**: `SettingsViewModel` + `SettingsPage.xaml` — chỉ Administrator; đọc/ghi `local.json` tại `AppContext.BaseDirectory`; `MachineCode`/`MachineName` áp dụng ngay (in-memory `AppSettings`); `ApiBaseUrl` áp dụng sau restart (HttpClient singleton đã tạo với URL cũ); TestConnection dùng `GET {url}/api/v1/auth/login` với `new HttpClient()` riêng (không share singleton)
- **FAI session started guard**: `NavigateToFai` check `_work.ActiveSession?.StartedAt.HasValue != true` → redirect Dashboard. Shortcut "Bảng đo" condition: `canFai = !_work.IsViewMode && hasProd && _work.ActiveSession?.StartedAt.HasValue == true`.
- **DocumentViewer navigation**: `HandleDashboardNavigation` cases "gcode"/"drawing"/"fixture"/"routecard" → `NavigateToDocumentViewer()`; dùng browse job/op context (View Mode safe). API: `GET /api/v1/tech-documents?partOpId=&status=Approved` → list; `GET /api/v1/tech-documents/{id}/download-url` → string URL → `HttpClient.GetStringAsync`.
- **GcodeViewerBehavior**: attached property `kb:GcodeViewerBehavior.Text` trên `RichTextBox` — parse G-code thành `FlowDocument` với colored `Run`s. Token colors: N=gray, G=blue #1565C0, M=purple #6A1B9A, X/Y/Z/I/J/K=orange #E65100, F/S=green #2E7D32, T/H/D=teal #00838F, O=red, comment(`;`/`(`)=gray. Limit 5000 dòng.
- **HexToBrushConverter**: converter mới `string hex → SolidColorBrush`, dùng `ColorConverter.ConvertFromString`. Đăng ký trong App.xaml với key `HexToBrushConverter`.
- **VIEW MODE toggle chip**: luôn visible trong TitleBar Dashboard. `DataTrigger IsViewMode=True` → orange bg `#FF8F00` + text "VIEW MODE"; `IsViewMode=False` → transparent/BrandPrimary bg + text "VIEW". `ToggleModeCommand(CanExecute = nameof(CanSwitchMode))` — disabled khi forced View Mode (IncomingSession từ người khác, role không phải Leader/Admin). XAML: `DataTrigger CanSwitchMode=False` → `Opacity=0.4` + `Cursor=Arrow`.
- **CanSwitchMode**: `_work.IncomingSession is null || ClaimedBy == auth.UserId || role in Leader/Manager/Admin`. Notify trong `RefreshWorkInfo()` + `ToggleModeCommand.NotifyCanExecuteChanged()`.
- **`FlowDocument.PageWidth/ColumnWidth`**: `double.PositiveInfinity` KHÔNG hợp lệ trong .NET 9 WPF (ArgumentException tại runtime) — dùng `100000.0` cho code viewer để tắt line-wrap. Thiếu 2 thuộc tính này → mỗi ký tự xuống 1 dòng riêng.
- **MinIO presigned URL download**: KHÔNG dùng shared `HttpClient` singleton (đã có `Authorization: Bearer` header) — MinIO trả 400/403 vì presigned URL đã có auth sẵn trong query string. Luôn tạo `new System.Net.Http.HttpClient()` riêng (không header) để download file từ presigned URL.
- **WebView2 PDF viewer**: Dùng `Microsoft.Web.WebView2` (NuGet) — Edge runtime có sẵn trên Windows 10/11. PDF rendering native qua Edge PDF viewer (zoom/pan built-in). `EnsureCoreWebView2Async()` gọi một lần khi WebView2 lần đầu trở nên visible (dùng `IsVisibleChanged` event). Presigned URL navigate trực tiếp — MinIO auth trong query string, không cần header. `IsPdfViewerVisible = IsNonGcodeSelected && PdfUrl != null && !IsLoadingContent` — đảm bảo WebView2 chỉ visible sau khi loading xong (tránh airspace problem với WPF elements).
- **WebView2 airspace problem**: Win32 control (WebView2) render trên WPF elements — WPF loading spinner sẽ bị che khuất. Giải pháp: chỉ set `IsPdfViewerVisible=true` sau khi loading xong, spinner đã ẩn trước khi WebView2 xuất hiện.
- **ProductList select flow (thiết kế mới)**: Claim = client-side only — `SelectProductAsync` không gọi API. Logic: (1) resume nếu `inprogress && SessionId == _work.ActiveSession?.Id`; (2) block nếu `inprogress` (máy khác); (3) block nếu `complete`; (4) còn lại → `_work.SetProduct(product, null)` + navigate. Không có bước POST claim nữa.
- **BeginSession flow**: Khi bấm "Bắt đầu" trên Dashboard → `POST /api/v1/production-sessions` với `{productId, partOpId, machineCode}` → server tạo session + set `started_at` ngay → trả về `ProductionSessionDto` → `_work.SetProduct(currentProduct, session)`.
- **CanNavigate/CanStart (thiết kế mới)**: `CanNavigate = HasWork && !HasProduct && ActiveSession == null` (có job/op nhưng chưa chọn product); `CanStart = HasProduct && !IsWip && IsOperationMode` (đã chọn product, chưa có session). Mutual exclusion đảm bảo chỉ 1 button visible.
- **WorkState "has-product"**: thay thế "complete" — `HasProduct && !IsWip` → `"has-product"` (đã chọn sản phẩm, chưa bắt đầu gia công). `TapWorkInfo` case "has-product" → navigate to products.
- **Shortcut disabled khi inprogress**: `canChangeContext = IsViewMode || !IsWip` — truyền `isEnabled: canChangeContext` vào `Add()` cho 3 shortcuts "Chọn Job/OP/Sản phẩm". `UtilBtn` ControlTemplate có `Trigger IsEnabled=False → Opacity=0.4 + Cursor=Arrow`. View Mode → re-enable (thao tác trên view context).

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
| Desktop MES — Design Language | [`Project_Documents/16_design_language.md`](Project_Documents/16_design_language.md) |

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
