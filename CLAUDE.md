# CLAUDE.md

Shopfloor Manager — open-source factory management system for CNC machining shops. Replaces legacy WinForms (DevExpress). Solo project — **simple and maintainable** over clever. Target: 50–200 users / nhà máy gia công CNC.

---

## Hệ sinh thái

```
Web App (Next.js 16)  ↔  ASP.NET Core API (.NET 9)  ↔  Desktop App (WPF .NET 9)
 Office UI — browser      Business logic · Auth          CNC machine touchscreen
 Manager · QC · Eng       REST API · SignalR hub         FAI · NCR · G-code viewer
                                    │
                       PostgreSQL · MinIO · Mosquitto MQTT
```

| Cũ | Mới |
|---|---|
| ManageData WinForms (DevExpress) | Web App (Next.js) |
| Vinam-MES WinForms (touchscreen) | Desktop App (WPF) |
| MySQL stored procedures | ASP.NET Core Application layer |
| FTP Server / MySQL DB | MinIO / PostgreSQL |

---

## Triết lý xây dựng sản phẩm

- **Self-hosted first**: `docker compose up` chạy được trên Linux server nội bộ
- **Solo-developer friendly**: Không over-engineer. Giải pháp đơn giản nhất đủ dùng
- **C# là ngôn ngữ duy nhất** (Phase 0–5)
- **Business logic 100% ở API**: Database chỉ lưu trữ — không stored procedures, không trigger
- **Mã nguồn mở**: Chỉ dùng thư viện MIT/Apache 2.0
- **Audit trail**: Mọi thay đổi ghi `created_by`, `updated_by`, `created_at`, `updated_at`

---

## Tech Stack

### Backend (.NET 9)

| Layer | Công nghệ | License |
|---|---|---|
| API | ASP.NET Core Web API .NET 9 | MIT |
| ORM | Entity Framework Core 9 | MIT |
| Database | PostgreSQL 16 | OSS |
| File Storage | MinIO | AGPL v3 |
| Auth | JWT Bearer | MIT |
| Real-time | SignalR | MIT |
| MQTT | MQTTnet + Mosquitto | MIT/EPL |
| Excel | ClosedXML ✅ | MIT |
| PDF | QuestPDF ✅ | MIT |
| SPC/Math | MathNet.Numerics ✅ | MIT |
| Email | MailKit | MIT |

### Web Client (`clients/web`)

| Layer | Công nghệ |
|---|---|
| Framework | **Next.js 16** (App Router) + TypeScript v16.2.6 |
| UI | **@base-ui/react** + shadcn CLI · Tailwind CSS v4 |
| Forms/State | React Hook Form + Zod · Zustand + TanStack Query v5 |

### Desktop (`src/ShopfloorManager.Desktop`)

| Layer | Công nghệ |
|---|---|
| Framework | **WPF .NET 9** (Windows only) |
| UI | MaterialDesignThemes + CommunityToolkit.Mvvm |
| PDF viewer | Microsoft.Web.WebView2 |

**Không dùng:** ❌ Python (Phase 0–5) · ❌ DevExpress/Telerik · ❌ .NET MAUI · ❌ MySQL SP · ❌ FTP thuần · ❌ Hardcode credential

---

## Cấu trúc repo

```
shopfloor-manager/
├── src/                          # .NET solution
│   ├── ShopfloorManager.API      # REST API — http://localhost:5066
│   ├── ShopfloorManager.Desktop  # WPF touchscreen MES
│   ├── ShopfloorManager.Application
│   ├── ShopfloorManager.Domain
│   ├── ShopfloorManager.Infrastructure
│   └── ShopfloorManager.Shared
├── clients/web/                  # Next.js 16 — http://localhost:3000
└── Project_Documents/            # Tài liệu nghiệp vụ
```

---

## Dev Commands

```bash
# 1. Start infrastructure (PostgreSQL + MinIO + Mosquitto)
docker compose -f docker-compose.dev.yml up -d

# 2. Run API
cd src && dotnet run --project ShopfloorManager.API
# API: http://localhost:5066  |  Swagger: http://localhost:5066/swagger
# MinIO: http://localhost:9001 (minioadmin/minioadmin123)
# PostgreSQL: localhost:5432 (shopfloor/dev_password/shopfloor_dev)

# 3. Run Web app
cd clients/web && npm run dev   # → http://localhost:3000

# Build + Test
dotnet build src/ShopfloorManager.sln
dotnet test src/ShopfloorManager.sln

# EF Core migrations (chạy từ repo root)
dotnet ef migrations add {Name} --project ShopfloorManager.Infrastructure --startup-project ShopfloorManager.API
dotnet ef database update --project ShopfloorManager.Infrastructure --startup-project ShopfloorManager.API
```

---

## Web App — `clients/web`

```
clients/web/
├── app/
│   ├── (auth)/login/
│   └── (main)/                    # VASidebar 224px + VATopbar shell
│       ├── dashboard/  parts/  jobs/  dimsheet/  documents/
│       ├── fai/  ncrs/  gages/  calibration/
│       └── planning/  cnc/  hr/  master/
├── components/
│   ├── va/        # Design system: sidebar, topbar, badge, kpi, card, btn, seg, combobox
│   └── ui/        # shadcn components
├── lib/
│   ├── api-client.ts    # Typed fetch + JWT
│   └── va-tokens.ts     # Design tokens
└── stores/auth.store.ts
```

**Design system — VA warm industrial:** Sidebar 224px `#6D3B1A`, accent `#F57C00`, nền kem `#FFF8F0`. Fonts: Inter + Fraunces + JetBrains Mono. Inline styles với `va.*` tokens — không dùng Tailwind bên trong VA components. Xem [`Project_Documents/18_web_design_language.md`](Project_Documents/18_web_design_language.md) cho layout patterns.

**Trang API thật:** `/jobs`, `/parts`, `/dimsheet`, `/documents`, `/ncrs`, `/hr`, `/fai`, `/gages`, `/calibration`, `/master`
**Mock data (Phase 5 pending):** `/planning`, `/cnc`

### Lưu ý kỹ thuật quan trọng

**Scroll trong layout flex:** `(main)/layout.tsx` dùng `overflow: hidden` — mọi page root **phải có `minHeight: 0`**:
```tsx
<div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
```
Bảng dài trong VACard: `<VACard pad={false} style={{ flex: 1, minHeight: 0 }}><div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}><table>...`
Header sticky: `<th style={{ position: 'sticky', top: 0, background: va.surface2, zIndex: 1 }}`

**Zustand + App Router:** `useAuthStore` dùng `persist` → server renders `user=null`. Component hiển thị user dùng `useState/useEffect` mounted check.

**Next.js 16 breaking changes:** Đọc `clients/web/AGENTS.md` + `node_modules/next/dist/docs/` trước khi code.

---

## i18n — English + Tiếng Việt

Web dùng `next-intl` (cookie `NEXT_LOCALE`), Desktop dùng RESX + `{loc:Loc Key=...}` MarkupExtension.

**Web — Đã dịch:** `nav`+`common` (sidebar), `dashboard`, `parts`, `dimsheet`, `documents`, `jobs`, `erp`
**Web — Chưa dịch:** `/fai`, `/ncrs`, `/gages`, `/calibration`, `/hr`, `/master`, `/planning`, `/cnc`, `/login`

**Desktop — Đã dịch:** `LoginWindow.xaml`, `SettingsPage.xaml`
**Desktop — Chưa dịch:** DashboardPage, JobListPage, OperationPage, ProductListPage, FaiPage, DocumentViewerPage, NcrDialogWindow, keyboards

**Cách thêm namespace mới (Web):**
1. Thêm namespace vào CẢ `messages/vi.json` VÀ `messages/en.json`
2. `"use client"` + `const t = useTranslations('namespace')` → `t('key')`
3. Date/time: `useLocale()` → `toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US')`
4. Key động: `t(\`group.${key}\`)` — xem pattern trong `dashboard/page.tsx`

**Cách thêm key Desktop:**
1. Convention: `<Page>_<Element>` (vd `Login_UsernameHint`)
2. Thêm vào CẢ `Strings.resx` (VI) VÀ `Strings.en-US.resx` (EN)
3. XAML: `Text="{loc:Loc Key=...}"` | Code: `LocalizationManager.Instance["Key"]`

---

## Architecture (.NET)

Clean Architecture — Dependency direction: **API → Application → Domain ← Infrastructure**

```
ShopfloorManager.API            # Controllers, middleware, Program.cs, DI composition
ShopfloorManager.Application   # MediatR commands/queries, FluentValidation, DTOs, interfaces
ShopfloorManager.Domain        # Entities, enums — no framework dependencies
ShopfloorManager.Infrastructure # EF Core DbContext, MinIO, MQTT, MailKit
ShopfloorManager.Shared        # PagedResult<T>, AppConstants, enums shared across layers
```

**Request flow:**
```
HTTP → Controller (thin — chỉ gọi IMediator.Send)
     → MediatR Handler (toàn bộ business logic)
     → Repository/Service interfaces → EF Core / MinIO / MQTT
```

**Base types:**
```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}
public abstract class SoftDeletableEntity : BaseEntity
{
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
}
```

**Standard API response:** `{ "success": true, "data": {}, "error": null, "pagination": { "page": 1, "pageSize": 20, "total": 100 } }`

---

## Domain Model — Production Core

### Sơ đồ

```
PartNumber → PartRev ──→ TechDocument (DRW, CAD — Part-level)
                └──→ Routing → RoutingRev → PartOp ──→ TechDocument (GCD/TLS/CAM/THD)
                                                   └──→ Dimension → MeasureValue
                                           ForJobOnly OP → TechDocument (RTC/FXT)

Job ──→ PartRevId (snapshot) + RoutingRevId (snapshot)
    └──→ Product (serial 001..N) → MeasureValue
```

### Các thực thể

**PartRev** — `PartNumber` có nhiều PartRev (Rev A, B, C...). Thực tế 1 Routing active per PartRev.

**RoutingRev** — Chỉ 1 `IsActive=true` per Routing. Tạo rev mới → copy toàn bộ PartOps từ rev cũ.

**PartOp** — Thuộc `RoutingRev`, KHÔNG thuộc Part trực tiếp. `ForJobOnly=true` = OP bổ sung riêng cho 1 Job.

**Dimension** — Thuộc `PartOp`. `BalloonNumber` = số bóng trên bản vẽ ("Ø1", "L2"). Prefix `*` = kích thước trung gian (process control, không kiểm soát sản phẩm cuối). `DECIMAL(14,4)` — KHÔNG dùng VARCHAR.

**Job** — Snapshot `PartRevId` + `RoutingRevId`. Routing = query từ RoutingRev + ForJobOnly OPs. **KHÔNG copy PartOps vào Job.**

**MeasureValue** — Gắn với `(DimensionId, ProductId, MeasureStage)`. Không ghi đè — tạo record mới (giữ lịch sử). 3 giai đoạn: `InprocessFAI=0` (Operator), `QCInline=1`, `QCFinal=2`. `MeasureValue.MeasuredAt` — KHÔNG phải `CreatedAt` (không extends BaseEntity).

### Business rules

```
Tạo RoutingRev mới   → deactivate cũ, copy toàn bộ PartOps
Tạo Job              → lưu PartRevId + RoutingRevId (snapshot)
Routing của Job      → PartOps WHERE RoutingRevId = job.RoutingRevId
                        UNION PartOps WHERE JobId = job.Id
Upload TechDocument  → Status=Pending, chờ Lead Engineer/Manager/Admin duyệt
Upload rules         → (1) BLOCK nếu Approved; (2) BLOCK nếu Pending + người khác;
                        (3) ALLOW nếu Rejected → rename cũ "Rejected_*", upload mới
```

### TechDocument — 3 loại

```
1. Part-level  (partRevId set, partOpId null)               → DRW, CAD
2. Standard OP (partOpId set, routingRevId set, jobId null) → GCD, TLS, CAM, THD
3. ForJobOnly  (partOpId set, jobId set, forJobOnly=true)   → RTC, FXT
```

Xem [`Project_Documents/05_technical_documents.md`](Project_Documents/05_technical_documents.md) cho MinIO path và FileType flags.

---

## Key Design Decisions

**Database:**
- PostgreSQL only — all logic in C#, no stored procedures
- `DECIMAL(14,4)` cho tất cả giá trị đo/kích thước — KHÔNG dùng VARCHAR
- `snake_case` tên bảng/cột · Soft delete via `deleted_at TIMESTAMPTZ`
- **`DateTimeOffset` + Npgsql**: Npgsql chỉ nhận offset=0 (UTC). KHÔNG dùng `DateTimeOffset.UtcNow.Date`. Luôn dùng `new DateTimeOffset(y, m, d, 0, 0, 0, TimeSpan.Zero)`

**Domain enums:**
```csharp
FileStatus:        Pending=0, Approved=1, Rejected=2
MeasureResult:     Pass=1, Fail=2       // 1-indexed (legacy compat)
MeasureStage:      InprocessFAI=0, QCInline=1, QCFinal=2
NcrStatus:         Open=0, Closed=1
BorrowStatus:      Active=0, Returned=1, Cancelled=2
CalibRequestStatus:Pending=0, Approved=1, Completed=2, Cancelled=3
```

**Roles:** `Administrator`, `Manager`, `Lead Engineer`, `Engineer`, `QC Inspector`, `Operator`, `Planner`, `Leader`

**Phân quyền:**
- Approve/Reject TechDocuments & Dimensions: chỉ `Lead Engineer`, `Manager`, `Administrator`
- `QC Inspector`, `Operator`: chỉ xem file/dimension đã Approved — không có quyền duyệt
- Desktop ProductionSession: `Operator` (own session) · `Leader` (force-finish) · `Administrator` (Leader + Settings) · Các role khác → View_Mode when máy có session người khác

**MinIO:** bucket `shopfloor-storage`, pre-signed URL — client upload thẳng.
**MQTT topics:** `factory/cnc/#` (all CNC), `factory/cnc/{machineCode}/status` per machine.

---

## Project Status

| Phase | Scope | Trạng thái |
|---|---|---|
| 0–4 | Foundation, Auth, Production Core, Quality, Desktop MES | ✅ Done |
| Gage + Web UI | Gage/Calibration, 18 web routes, VA design system | ✅ Done |
| **Phase 5 active** | Dimension Approval ✅, MeasureStage ✅, Web UI Redesign (C/J/I/H/K ⏳) | ⏳ |
| Phase 5 remaining | Planning (L), CNC Live (M), Dashboard web | ⏳ |
| Phase 6 | Multi-factory, migration tool, docs site, one-command setup | ⏳ |

Xem chi tiết lịch sử triển khai trong [`Project_Documents/20_progress_log.md`](Project_Documents/20_progress_log.md).

---

## Coding Conventions

### Backend (C# / ASP.NET Core)
- Controller thin — chỉ gọi MediatR, không chứa business logic
- Business logic 100% trong Application layer (MediatR handlers)
- Validate ở handler (FluentValidation pipeline behavior)
- Ghi migration sau mỗi thay đổi entity: `dotnet ef migrations add {Name}`
- Swagger annotation cho mọi endpoint mới
- Không hardcode credential, URL, port

### Web Client (Next.js / TypeScript)
- **Server Components mặc định** — chỉ `"use client"` khi cần interactivity
- **Không dùng `any`** — type everything
- **TanStack Query** cho server state (không dùng useState + useEffect để fetch)
- **Zod** validate form input tại boundary
- **Không hardcode URL** — dùng `NEXT_PUBLIC_API_URL`
- **Next.js 16 breaking changes** — đọc `clients/web/AGENTS.md` trước khi code

```typescript
// ✅ Server state
const { data: jobs } = useQuery({ queryKey: ['jobs', filters], queryFn: () => api.jobs.list(filters) })
// ✅ Form
const schema = z.object({ value: z.number().min(0) })
```

### Desktop Client (WPF)
- KHÔNG kết nối DB trực tiếp — chỉ qua REST API
- JWT token lưu in-memory (không persist ra disk)
- `HttpClient` + `IApiClient` phải là **singleton** — token share giữa mọi ViewModel
- Trigger data load từ ViewModel (NavigateTo command), KHÔNG dùng `Loaded` event
- `WorkContext` là singleton ObservableObject — state chia sẻ giữa tất cả pages
- Touch target: Button `MinHeight=56`, TextBox `MinHeight=52`

### Chung
- Không comment WHAT — chỉ comment WHY khi logic không rõ ràng
- Không tạo abstraction sớm — đợi đến lần thứ 3 mới extract

---

## Mapping công nghệ cũ → mới

| Cũ (WinForms) | Mới | Trạng thái |
|---|---|---|
| MySQL stored procedures | EF Core + MediatR handlers | ✅ |
| DevExpress XtraGrid | TanStack Table + shadcn | Web Phase 5 |
| RDLC / DevExpress Report | QuestPDF | ✅ installed |
| FTP (`FtpClient.cs`) | MinIO pre-signed URL | ✅ |
| Outlook Interop | MailKit | ✅ |
| Office Interop Excel | ClosedXML | ✅ installed |
| FastColoredTextBox | GcodeViewerBehavior (WPF) / Monaco (web Phase 5) | ✅ desktop |
| GanttChart library | Frappe Gantt | Phase 5 |
| `FormKeyboard` | Custom WPF NumPad + QWERTY | ✅ |
| PdfiumViewer | WebView2 (WPF) | ✅ |

---

## Deploy Production

```
Nginx: shopfloor.factory.local        → Web (Next.js)
       shopfloor.factory.local/api/*  → API
       shopfloor.factory.local/hub/*  → SignalR

Docker: docker compose -f docker-compose.yml up -d  (cần .env từ .env.example)

⚠️ clients/web/Dockerfile chưa có — cần tạo trước khi deploy web.
Desktop: dotnet publish → deploy thủ công lên từng PC CNC.
```

---

## Roadmap

| Phase | Scope | Trạng thái |
|---|---|---|
| 0–4 + Gage + Web UI | Foundation → Desktop MES + Gage/Calibration + 18 web routes | ✅ |
| 5 — Advanced | Gage ✅, Dimension Approval ✅, Planning, MQTT, Dashboard web | ⏳ |
| 6 — Polish & Open Source | Multi-factory, migration tool MySQL→PG, docs site | ⏳ |

### Web UI Redesign — trạng thái

Sidebar 5 nhóm. Thứ tự: **Kỹ thuật ✅ → Sản xuất (D/F ✅) → Chất lượng → Hệ thống**.

| Nhóm | Phase | Nội dung | Thiết kế chi tiết | Status |
|---|---|---|---|---|
| Kỹ thuật | B, E, G | Dimsheet, Documents, Parts & Operations | `04`, `05_technical_documents.md` | ✅ |
| Sản xuất | D, F | Jobs: progress bar, serial/product grid | `03_job_management.md` | ✅ |
| Sản xuất | L | Planning: Gantt + API thật | `10_planning.md` (cần viết) | 🆕 |
| Sản xuất | M | CNC Live: MQTT thật | `12_cnc_mqtt.md` (cần viết) | 🆕 |
| Chất lượng | C | FAI: stat strip + stage filter | `06_dimensions_fai.md` § Phase C | ⏳ |
| Chất lượng | J | FAI: chi tiết balloon + history | `06_dimensions_fai.md` § Phase J | ⏳ |
| Chất lượng | I | NCR: workflow 5 bước (cần migration) | `07_ncr.md` § Phase I | ⏳ |
| Chất lượng | N, O | Gages, Calibration: redesign + API thật | `08`, `09_calibration.md` (cần viết) | 🆕 |
| Hệ thống | H | HR: org tree + user table | `02_hr.md` § Phase H | ⏳ |
| Hệ thống | K | Master Data redesign | `13_master_data.md` § Phase K | ⏳ |

**Quy trình mỗi phase:** (1) Mô tả UI/API/schema thay đổi → (2) User confirm → (3) Implement → (4) Build + verify browser → (5) Cập nhật `20_progress_log.md`

**Design Language:** xem [`Project_Documents/18_web_design_language.md`](Project_Documents/18_web_design_language.md) — master-detail, KPI strip, filter bar, `VACombobox`, sticky table header, inline-edit, status badge, `VASeg` tabs.

**Decisions đã chốt:** Sidebar nhóm cuối = "Hệ thống" · Theme `#6D3B1A` giữ nguyên · NCR = workflow 5 bước đầy đủ.

---

## Source Code Reference (cũ → mới)

| Tính năng | Source cũ |
|---|---|
| FAI đo kiểm | `Vinam-MES/FANUC/Forms/FormFAI.cs` |
| Process Monitor | `Vinam-MES/FANUC/Forms/FormProcessMonitor.cs` |
| NCR tại máy | `Vinam-MES/FANUC/Common/MySqlHelper.cs` |
| Tech Documents | `ManageData/Common/Techdocuments/StoreTechdocuments.cs` |
| Dimension import | `ManageData/Forms/FormUpdateDimension.cs` |
| FAI Report | `ManageData/Forms/Report/DimensionFAI/FormReportFAI.cs` |
| Planning Gantt | `ManageData/Forms/Planning/FormManagePlanning.cs` |

**Không copy code cũ.** Chỉ tham khảo business logic.

---

## Rules for Claude

**Luôn trả lời bằng tiếng Việt** (kể cả khi người dùng hỏi bằng tiếng Anh).

**Always ask before:**
- Changing DB schema (EF Core migrations are hard to rollback)
- Adding a NuGet/npm package (must be MIT/Apache 2.0, must have a clear reason)
- Restructuring directories

---

### Triển khai tính năng — quy trình bắt buộc

**Bước 0 — ĐỌC TÀI LIỆU TRƯỚC KHI CODE:**

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
| Planning, Gantt | [`Project_Documents/10_planning.md`](Project_Documents/10_planning.md) |
| Dashboard, Reports, PDF/Excel | [`Project_Documents/11_dashboard_reports.md`](Project_Documents/11_dashboard_reports.md) |
| CNC Data, MQTT, SignalR | [`Project_Documents/12_cnc_mqtt.md`](Project_Documents/12_cnc_mqtt.md) |
| Master data | [`Project_Documents/13_master_data.md`](Project_Documents/13_master_data.md) |
| Desktop MES (WPF, FAI at machine) | [`Project_Documents/14_desktop_mes.md`](Project_Documents/14_desktop_mes.md) |
| Desktop Dashboard UI | [`Project_Documents/15_dashboard_desktop.md`](Project_Documents/15_dashboard_desktop.md) |
| Desktop Design Language | [`Project_Documents/16_design_language.md`](Project_Documents/16_design_language.md) |
| Máy móc, MachineGroup, Epicor | [`Project_Documents/17_machines_equipment.md`](Project_Documents/17_machines_equipment.md) |
| Web Design Language (layout patterns) | [`Project_Documents/18_web_design_language.md`](Project_Documents/18_web_design_language.md) |
| Lịch sử triển khai, lessons learned | [`Project_Documents/20_progress_log.md`](Project_Documents/20_progress_log.md) |

**Tài liệu là nguồn sự thật duy nhất về business logic.** Nếu code cũ và tài liệu mâu thuẫn → ưu tiên tài liệu.

**Bước 1–6 — Implement theo Clean Architecture:**
1. Đọc tài liệu module → xác định Entity, Business Rules, Workflow, Edge Cases
2. Define entity trong `Domain` extending `BaseEntity` hoặc `SoftDeletableEntity`
3. Define command/query + handler trong `Application` (MediatR) — toàn bộ business logic
4. Define repository interface trong `Application`, implement trong `Infrastructure`
5. Add thin controller trong `API` — chỉ gọi `_mediator.Send(request)`
6. Add EF migration + Swagger annotation

**Production Core pattern (CRITICAL):**
- `PartOp` thuộc `RoutingRev`, KHÔNG thuộc `Part` trực tiếp
- `Job` phải lưu cả `PartRevId` và `RoutingRevId` (snapshot)
- Routing của Job = query động từ `RoutingRevId` + ForJobOnly OPs
- `MeasureValue` = tạo record mới mỗi lần đo (không ghi đè)
- `DECIMAL(14,4)` cho mọi giá trị đo/kích thước

**Don't:**
- Business logic trong controllers hoặc EF entities
- Python (Phase 0–5)
- Hardcode credentials, URLs, ports — dùng `appsettings.json` / env vars
- Copy code cũ WinForms — chỉ tham khảo business logic
- VARCHAR cho measurement values
