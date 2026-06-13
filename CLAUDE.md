# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Shopfloor Manager is an open-source factory management system for CNC machining shops, replacing a legacy WinForms (DevExpress) system. Solo project — 1 developer. Prioritize **simple and maintainable** over clever.

**Quy mô mục tiêu:** 50–200 người / nhà máy gia công cơ khí

---

## Hệ sinh thái

```
┌──────────────────────────────────────────────────────────┐
│  Web App  (Next.js 16 — clients/web)                     │
│  Văn phòng kỹ thuật / Quản lý                            │
│  HR · Job · OP · Dimension · Tech Docs · Planning        │
│  Gage · NCR · Dashboard · Reports                        │
│  → Truy cập từ mọi PC/tablet qua browser                │
└───────────────────────┬──────────────────────────────────┘
                        │ REST API + SignalR
┌───────────────────────▼──────────────────────────────────┐
│  ASP.NET Core Web API (.NET 9) — src/ShopfloorManager.API│
│  Business Logic · Auth · File Proxy · MQTT gateway       │
└───┬──────────────────┬──────────────────┬────────────────┘
    │                  │                  │
PostgreSQL           MinIO           SignalR Hub
(data only,       (file storage,    (real-time
 no SP/logic)      thay FTP)         notifications)
                                         │
┌───────────────────────▼──────────────────────────────────┐
│  Desktop App  (WPF .NET 9 — src/ShopfloorManager.Desktop)│
│  Tại máy CNC — màn hình cảm ứng xưởng sản xuất          │
│  FAI đo kiểm · NCR nhanh · Xem G-code/Drawing           │
│  Chọn Job/OP/Serial · Session quản lý                    │
│  → Cài đặt tại mỗi PC máy CNC (Windows)                 │
└───────────────────────┬──────────────────────────────────┘
                        │ MQTT (Mosquitto)
┌───────────────────────▼──────────────────────────────────┐
│  Mosquitto MQTT Broker                                   │
│  Thu thập dữ liệu real-time từ máy CNC                   │
│  (FANUC FOCAS / MTConnect → publish → API subscribe)     │
└──────────────────────────────────────────────────────────┘
```

**Hệ thống cũ cần thay thế:**

| Thành phần cũ | Thay thế | Ghi chú |
|---|---|---|
| ManageData WinForms (DevExpress) | Web App (Next.js) | Văn phòng kỹ thuật |
| Vinam-MES WinForms (touchscreen) | Desktop App (WPF) | Tại máy CNC xưởng |
| MySQL stored procedures | ASP.NET Core Application layer | Business logic 100% ở API |
| FTP Server | MinIO | File storage |
| MySQL DB | PostgreSQL | Database |
| MDC_NetCore | MQTT pipeline tích hợp vào API | Thu thập dữ liệu máy |

---

## Triết lý xây dựng sản phẩm

- **Self-hosted first**: Một lệnh `docker compose up` là chạy được trên Linux server nội bộ.
- **Solo-developer friendly**: Không over-engineer. Chọn giải pháp đơn giản nhất đủ dùng.
- **C# là ngôn ngữ duy nhất** (Phase 0–5): Không thêm Python cho đến khi có nhu cầu analytics cụ thể.
- **Business logic 100% ở API**: Database chỉ lưu trữ — không stored procedures, không trigger.
- **Thực dụng**: Giao diện rõ ràng cho người dùng nhà máy. Không fancy.
- **Module hóa**: Mỗi tính năng là module độc lập.
- **Mã nguồn mở**: Chỉ dùng thư viện MIT/Apache 2.0. Không dependency thương mại.
- **Audit trail**: Mọi thay đổi ghi `created_by`, `updated_by`, `created_at`, `updated_at`.

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
| MQTT | MQTTnet | MIT |
| MQTT Broker | Mosquitto | EPL |
| Excel | ClosedXML ✅ | MIT |
| PDF | QuestPDF ✅ | MIT |
| SPC/Math | MathNet.Numerics ✅ | MIT |
| Email | MailKit | MIT |
| Container | Docker + Docker Compose | Apache 2.0 |

### Web Client (`clients/web`)

| Layer | Công nghệ | Ghi chú |
|---|---|---|
| Framework | **Next.js 16** (App Router) + TypeScript | Hiện tại dùng v16.2.6 |
| UI primitives | **@base-ui/react** (thay Radix) + shadcn CLI | shadcn generate components dùng Base UI |
| Styling | Tailwind CSS v4 | |
| Charts | Apache ECharts | Phase 5 — chưa cài |
| Gantt | Frappe Gantt | Phase 5 — chưa cài |
| Forms | React Hook Form + Zod | ✅ |
| State | Zustand + TanStack Query v5 | ✅ |
| G-code viewer | Monaco Editor | Phase 5 — chưa cài |

### Desktop Client (`src/ShopfloorManager.Desktop`)

| Layer | Công nghệ | Ghi chú |
|---|---|---|
| Framework | **WPF .NET 9** (Windows only) | MAUI không dùng |
| UI | MaterialDesignThemes + CommunityToolkit.Mvvm | ✅ |
| PDF viewer | Microsoft.Web.WebView2 | ✅ |
| Virtual keyboard | Custom WPF (NumPad + QWERTY) | ✅ |

### Không dùng

- ❌ Python (Phase 0–5 — C# đủ cho mọi việc: MQTT, Excel, PDF, SPC)
- ❌ DevExpress, Telerik, Syncfusion (thương mại)
- ❌ .NET MAUI (đã chọn WPF)
- ❌ MySQL Stored Procedures (business logic chuyển vào API)
- ❌ FTP thuần (thay bằng MinIO)
- ❌ Hardcode credential trong source code

---

## Cấu trúc repo

```
shopfloor-manager/
├── src/                          # .NET solution (API + Desktop)
│   ├── ShopfloorManager.API      # REST API — http://localhost:5066
│   ├── ShopfloorManager.Desktop  # WPF touchscreen MES (Phase 4)
│   ├── ShopfloorManager.Application
│   ├── ShopfloorManager.Domain
│   ├── ShopfloorManager.Infrastructure
│   └── ShopfloorManager.Shared
│
├── clients/
│   └── web/                      # Web app "Office" — Next.js 16 + React 19 + TypeScript
│                                 # Tailwind CSS v4 + shadcn/ui + TanStack Query + Zustand
│                                 # http://localhost:3000
│
└── Project_Documents/            # Tài liệu nghiệp vụ
```

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

# 3. Run Web app (office UI)
cd clients/web
npm run dev
# Web: http://localhost:3000

# Build solution (.NET)
dotnet build src/ShopfloorManager.sln

# Run tests
dotnet test src/ShopfloorManager.sln

# EF Core migrations (run from src/ — required after any entity change)
dotnet ef migrations add {MigrationName} --project ShopfloorManager.Infrastructure --startup-project ShopfloorManager.API
dotnet ef database update --project ShopfloorManager.Infrastructure --startup-project ShopfloorManager.API
```

> The dev compose has **no auth** — PostgreSQL credentials are hardcoded (`shopfloor` / `dev_password`). Production uses `.env` (copy from `.env.example`).

---

## Web App — `clients/web`

**Next.js 16** (App Router) + **React 19** + **TypeScript** — Office UI cho Manager, QC, Engineer, Planner. Khác với Desktop MES (WPF touchscreen tại máy CNC).

```
clients/web/
├── app/
│   ├── (auth)/login/
│   └── (main)/                    # Authenticated layout — VASidebar + VATopbar shell
│       ├── layout.tsx             # Shell: VASidebar 224px + flex-1 content
│       ├── dashboard/             # Dashboard KPI (placeholder Phase 5)
│       ├── parts/                 # "Chi tiết kỹ thuật" — master-detail: part list + revision + routing + OP
│       │   └── [id]/              # Part detail (revisions, routing revs, operations)
│       ├── jobs/                  # "Lệnh SX & Sản phẩm" — master-detail: job list + progress + serials
│       │   └── [id]/              # Job detail + fai + documents
│       ├── planning/              # Gantt chart tuần (mock data)
│       ├── cnc/                   # CNC Live — machine status + gauges (mock data)
│       ├── fai/                   # FAI Dimension Sheet matrix (mock data)
│       ├── ncrs/                  # NCR list + detail
│       ├── gages/                 # Gage management (mock data)
│       ├── calibration/           # Calibration requests (mock data)
│       ├── documents/             # Tech documents approval (mock data)
│       ├── hr/                    # HR + user management (mock data)
│       └── master/                # Master data tabs: machines/op-types/dim-cats
├── components/
│   ├── va/                        # VA design system components
│   │   ├── sidebar.tsx            # VASidebar — 224px nâu, nav groups, user footer
│   │   ├── topbar.tsx             # VATopbar — breadcrumb + serif title + search
│   │   ├── badge.tsx              # VABadge (ok/warn/err/neutral/primary/running)
│   │   ├── kpi.tsx                # VAKpi card với trend indicator
│   │   ├── card.tsx               # VACard với header slot
│   │   ├── btn.tsx                # VABtn (primary/accent/ghost)
│   │   ├── seg.tsx                # VASeg segmented control
│   │   └── index.ts               # Barrel export
│   ├── ui/                        # shadcn components (Button, Card, Input...)
│   ├── auth/login-form.tsx
│   ├── jobs/create-job-dialog.tsx
│   └── parts/create-part-dialog.tsx
├── lib/
│   ├── api-client.ts              # Typed API client (fetch + JWT)
│   └── va-tokens.ts               # VA design tokens (colors, shadows, fonts)
└── stores/auth.store.ts           # Zustand auth store (JWT in localStorage)
```

**Dependencies:** `@tanstack/react-query` · `zustand` · `zod` · `react-hook-form` · `@base-ui/react` (shadcn CLI) · `tailwindcss v4` · `lucide-react`

**Design system — VA warm industrial** (từ template `D:\Temple\Shopfloor Manage`):
- Sidebar 224px nâu `#6D3B1A`, accent cam `#F57C00`, nền kem `#FFF8F0`
- Fonts: Inter (body) + Fraunces (serif title) + JetBrains Mono (numbers/code)
- Components: `VASidebar`, `VATopbar`, `VABadge`, `VAKpi`, `VACard`, `VABtn`, `VASeg`
- Inline styles với `va.*` tokens — không dùng Tailwind bên trong VA components

**Trang dùng API thật:** `/jobs` (Lệnh SX & Sản phẩm), `/parts` (Chi tiết kỹ thuật), `/ncrs`, `/hr` (Nhân sự & Tài khoản), `/fai` (FAI & Đo kiểm — chọn Job/OP rồi xem matrix thật)
**Trang dùng mock data (chờ Phase 5 API):** `/planning`, `/cnc`, `/gages`, `/calibration`, `/documents`, `/master`

**Lưu ý kỹ thuật — Zustand + Next.js App Router:**
- `useAuthStore` dùng `persist` middleware → trên server `user=null`, sau hydrate mới có data
- Các component hiển thị `user` dùng `useState/useEffect` mounted check để tránh flicker
- Sidebar user footer: `{mounted && user ? initials(user.name) : ''}`

**Lưu ý kỹ thuật — Scroll trong layout flex:**
- `(main)/layout.tsx` bọc mỗi page trong `<div className="flex-1 flex flex-col overflow-hidden min-w-0">` — nếu page root thiếu `minHeight: 0`, flexbox "automatic minimum size" sẽ làm div phình theo nội dung và bị `overflow: hidden` của layout cắt mất (clip), thay vì cho cuộn.
- **Mọi page root** (`<div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>`) phải có thêm `minHeight: 0`.
- **Bảng/danh sách dài trong `VACard`**: dùng `<VACard pad={false} style={{ flex: 1, minHeight: 0 }}><div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}><table>...</table></div></VACard>` — header `<th>` thêm `position: 'sticky', top: 0, background: va.surface2, zIndex: 1` để sticky khi cuộn. `VACard` mặc định `overflow: 'hidden'` nên không tự cuộn nếu thiếu wrapper này.
- `.va-scroll` (`globals.css`) chỉ style appearance scrollbar — luôn phải đi kèm `overflow: 'auto'` inline.

**Dev server:** `cd clients/web && npm run dev` → http://localhost:3000

**Lưu ý quan trọng về Next.js 16:** Đọc `clients/web/AGENTS.md` — version này có breaking changes so với training data. Đọc docs trong `node_modules/next/dist/docs/` trước khi code.

---

## i18n — English + Tiếng Việt

Hệ thống hỗ trợ 2 ngôn ngữ: **English** và **Tiếng Việt**. Web dùng `next-intl`, Desktop dùng RESX + custom `MarkupExtension`. Hạ tầng đã hoàn chỉnh — các trang/màn hình chưa dịch vẫn hardcode tiếng Việt, dịch dần theo pattern dưới đây.

### Web (`clients/web`) — next-intl

- **Cookie-based locale** (`NEXT_LOCALE`, không dùng URL prefix `/en/...`) — set qua Server Action `app/actions/locale.ts` (`setLocale('en'|'vi')`, maxAge 1 năm)
- `i18n/request.ts`: `getRequestConfig()` đọc cookie → load `messages/{locale}.json`; `next.config.ts` wrap bằng `createNextIntlPlugin('./i18n/request.ts')`
- `app/layout.tsx` (Server Component, async): đọc `getLocale()`/`getMessages()`, set `<html lang={locale}>`, wrap `{children}` trong `<NextIntlClientProvider>`
- `components/va/lang-switcher.tsx` (`VALangSwitcher`) — đặt trong `VASidebar` footer, mọi route đều thấy

**Đã dịch (pattern mẫu):** `components/va/sidebar.tsx` (namespace `nav` + `common`), `app/(main)/dashboard/page.tsx` (namespace `dashboard`), `app/(main)/parts/page.tsx` + `app/(main)/parts/[id]/operations/page.tsx` + `components/parts/*-dialog.tsx` (namespace `parts`), `app/(main)/dimsheet/page.tsx` (namespace `dimsheet`), `app/(main)/documents/page.tsx` (namespace `documents`) — toàn bộ nhóm sidebar "Kỹ thuật" đã dịch xong (2026-06-13)
**Chưa dịch (theo cùng pattern):** `/jobs`, `/fai`, `/ncrs`, `/gages`, `/calibration`, `/hr`, `/master`, `/planning`, `/cnc`, `/(auth)/login`

**Cách thêm trang mới:**
1. Thêm namespace mới (tên route, vd `jobs`) vào CẢ `messages/vi.json` VÀ `messages/en.json` — cùng cấu trúc key, khác giá trị
2. Trong component: `"use client"` + `const t = useTranslations('jobs')` → `t('title')`, `t('table.status')`...
3. Date/time format theo locale: `useLocale()` → `new Date().toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US', {...})`
4. Key động (vd label trong array/map) dùng pattern `t(\`group.${key}\`)` — xem `dashboard/page.tsx` production/quality cards

### Desktop (`src/ShopfloorManager.Desktop`) — RESX + MarkupExtension

- `Resources/Strings.resx` (default = **Tiếng Việt**, fallback) + `Resources/Strings.en-US.resx` (English satellite) — cùng key set, hand-written `Strings.Designer.cs` (không cần VS ResX code-generator; .NET SDK tự glob `**/*.resx` làm `EmbeddedResource` và build satellite assembly `en-US/ShopfloorManager.Desktop.resources.dll`)
- `Localization/LocalizationManager.cs` — singleton `INotifyPropertyChanged`, indexer `this[string key]`, `SetLanguage("vi"|"en")` set `Strings.Culture` + raise `PropertyChanged("Item[]")` để mọi binding indexer tự refresh (live switch, không cần restart)
- `Localization/LocExtension.cs` — `{loc:Loc Key=...}` MarkupExtension, dùng trong XAML
- `AppSettings.Language` ("vi"|"en", default "vi") — load từ `local.json`, áp dụng tại `App.xaml.cs OnStartup` qua `LocalizationManager.Instance.SetLanguage(settings.Language)`; `LocalizationManager.Instance` đăng ký singleton trong DI

**Đã dịch (pattern mẫu):** `Views/LoginWindow.xaml`, `Views/Pages/SettingsPage.xaml` + `ViewModels/SettingsViewModel.cs` (gồm section "NGÔN NGỮ" — 2 nút Tiếng Việt/English, đổi ngay lập tức + lưu vào `local.json`)
**Chưa dịch (theo cùng pattern):** `DashboardPage.xaml`, `JobListPage.xaml`, `OperationPage.xaml`, `ProductListPage.xaml`, `FaiPage.xaml`, `DocumentViewerPage.xaml`, `NcrDialogWindow.xaml`, virtual keyboards

**Cách thêm key mới:**
1. Đặt tên key theo convention `<Page>_<Element>` (vd `Settings_SaveButton`, `Login_UsernameHint`)
2. Thêm `<data name="Key" xml:space="preserve"><value>...</value></data>` vào CẢ `Strings.resx` (tiếng Việt) VÀ `Strings.en-US.resx` (tiếng Anh)
3. Trong XAML: thêm `xmlns:loc="clr-namespace:ShopfloorManager.Desktop.Localization"` vào root, dùng `Text="{loc:Loc Key=...}"` / `Content="{loc:Loc Key=...}"` / `md:HintAssist.Hint="{loc:Loc Key=...}"`
4. Trong code-behind/ViewModel (status messages, không phải binding): `LocalizationManager.Instance["Key"]` hoặc inject `LocalizationManager` qua DI; placeholder `{0}` dùng `string.Format(_loc["Key"], value)`
5. Computed/read-only property binding trong WPF mặc định TwoWay — `{loc:Loc}` đã set `Mode=OneWay` sẵn nên không cần thêm

---

## Architecture (.NET)

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
- **`DateTimeOffset` + Npgsql `timestamptz`**: Npgsql chỉ chấp nhận offset=0 (UTC) khi ghi/so sánh `timestamp with time zone`. KHÔNG dùng `DateTimeOffset.UtcNow.Date` (trả về `DateTime` Kind=Unspecified → convert ngầm lấy offset local của máy, vd +07:00 → ném `ArgumentException`). Luôn dựng mốc ngày bằng `new DateTimeOffset(y, m, d, 0, 0, 0, TimeSpan.Zero)`.

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

*(cập nhật 2026-06-04 — Web App VA design system complete)*

| Phase | Status |
|---|---|
| Phase 0 — Foundation (infrastructure, DB schema, .NET scaffold) | ✅ Done |
| Phase 1 — Auth & HR (JWT, users, roles, SignalR) | ✅ Done |
| Phase 2 — Production Core (Jobs, Parts, OPs, Documents) | ✅ Done |
| Phase 3 — Quality (Dimensions, FAI, NCR, SPC) | ✅ Done |
| Phase 4 — Desktop MES (WPF, FAI at machine, SignalR) | ✅ Done |
| Phase 5 — Advanced (Gage ✅, Planning, MQTT pipeline, Dashboard) | ⏳ |
| Phase 6 — Polish & Open Source (multi-factory, migration tool, docs) | ⏳ |

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

**Phase 4 — ✅ Hoàn tất** (2026-05-21 → 2026-06-09)
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
- ✅ FAI Final mode: shortcut "FAI Final" visible khi Operation Mode + session started + tất cả dims đã đo + có ít nhất 1 Fail; `FaiViewModel.IsFinalMode=true` — chỉ load dims có `State=Fail`; title bar đỏ thẫm `#B71C1C`; lưu với `IsFinal=true` trong API; API: `SaveMeasureCommand` hỗ trợ `IsFinal` flag
- ✅ SignalR real-time notifications: API `IRealtimeNotifier` interface (Application layer) + `SignalRNotifier` (API layer, dùng `IHubContext<ShopfloorHub>`); Desktop `ISignalRService` + `SignalRService` singleton; `ConnectAsync` sau login (fire-and-forget); `NcrCreated` event consumed bởi `DashboardViewModel`; banner đỏ `#B71C1C` auto-dismiss sau 8 giây
- ✅ SetPage() pattern: `MainViewModel.SetPage(vm)` gọi `CurrentPage?.Cleanup()` trước khi switch — ngăn ghost event subscription (NcrCreated leak) khi navigate away rồi back về Dashboard

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
- **FAI Final mode**: `FaiViewModel.IsFinalMode = true` được set TRƯỚC khi gọi `SetPage(vm)`. `InitializeAsync` khi `IsFinalMode=true` chỉ load dims có `State=Fail` (dựa trên `MeasureResult.Fail` của lần đo cuối). API `SaveMeasureCommand`: `IsFinal=true` → ghi `is_final=true` vào MeasureValue. Shortcut "FAI Final": `canFaiFinal = !IsViewMode && hasProduct && sessionStarted && allMeasured && hasAnyFail`.
- **IRealtimeNotifier pattern**: Interface `IRealtimeNotifier` định nghĩa ở Application layer (`ShopfloorManager.Application/Common/Interfaces/`); implementation `SignalRNotifier` ở API layer; đăng ký trong `Program.cs` là `services.AddScoped<IRealtimeNotifier, SignalRNotifier>()`. Inject vào MediatR handlers qua constructor.
- **SignalR Desktop singleton**: `ISignalRService` đăng ký là **singleton** — connection và event subscriptions sống suốt vòng đời app. `DashboardViewModel` (transient) subscribe/unsubscribe `NcrCreated` trong constructor/Cleanup. `SetPage()` đảm bảo Cleanup được gọi khi navigate away.
- **SetPage() cleanup pattern**: `MainViewModel.SetPage(vm)` → `CurrentPage?.Cleanup()` → `CurrentPage = vm`. Tất cả ViewModel phải override `Cleanup()` để unsubscribe events (đặc biệt `DashboardViewModel.NcrCreated` và `DispatcherTimer.Stop()`). Không gọi `CurrentPage = vm` trực tiếp — luôn dùng `SetPage()`.

**Phase 5 — Gage & Calibration — ✅ Hoàn tất** (2026-06-04 → 2026-06-10)
- Entities: `GageType`, `GageLocation`, `GageSlot`, `Gage`, `BorrowTransaction`, `CalibVendor`, `CalibProcedure`, `CalibRequest`, `CalibRecord` — migration `AddGageAndCalibration`
- `Gage` computed: `IsValid`, `DueDate`, `DaysRemaining` (`due_date = last_calibration + calib_frequency_days`); denormalized `IsBorrowed`, `HasPendingCalib`
- API: `GET /api/v1/gages` (search/statusCode/gageTypeId/isBorrowed), `GET /api/v1/gages/calib-due`, `POST /api/v1/gages`, `GET /api/v1/gage-types`, `GET /api/v1/gage-locations`, `POST /api/v1/borrow-transactions`, `GET /api/v1/borrow-transactions` (gageId/status filter), `PUT /api/v1/borrow-transactions/{id}/return`, `GET/POST /api/v1/calib-vendors`, `GET/POST /api/v1/calib-requests`, `PUT /api/v1/calib-requests/{id}/approve`, `POST /api/v1/calib-records`
- Web: `/gages` (KPIs, filter Tất cả/Hợp lệ/Đang mượn/Sắp hết hạn, mượn/trả) + `/calibration` (calib-due list, CreateRequestModal, CompleteModal) — đều dùng `api.*` client thật
- Migration `SeedGageReferenceData`: seed `calib_procedures` (3), `calib_vendors` (2), `gage_slots` (5, dưới location "GAGE ROOM" id=44)
- **Lưu ý dữ liệu dev DB**: `gage_types` (36 dòng) và `gage_locations` (89 dòng) đã có sẵn dữ liệu thực import từ legacy MySQL (không phải seed migration) — KHÔNG seed thêm vào 2 bảng này để tránh đụng PK. `gages` cũng đã có 85 dòng thực.
- **Phát hiện cần điều tra riêng**: `gage_locations` (89 dòng) chứa toàn mã máy/process (300-1, ASY, ENG1-6, GAGE ROOM, WDP...) — giống dữ liệu `machine_groups`/Epicor ResourceGroup hơn là "vị trí lưu trữ gage". `machine_groups` hiện đang trống (0 dòng). Có thể import trước đó đã ghi nhầm bảng — cần xem lại khi làm `17_machines_equipment.md` / migration tool.
- **GET /api/v1/borrow-transactions**: `GetBorrowTransactionsQueryHandler` trong `GageQueries.cs` — dùng bởi web `handleReturn()` để tìm `BorrowTransaction` đang `Active` theo `gageId` trước khi gọi `return`.

**i18n (English + Tiếng Việt) — Hạ tầng + Pilot pages — ✅ Hoàn tất** (2026-06-10)
- Web: cài `next-intl`; `i18n/request.ts` (cookie `NEXT_LOCALE`, default `vi`) + `messages/vi.json` + `messages/en.json`; `next.config.ts` wrap `createNextIntlPlugin`; `app/layout.tsx` async đọc `getLocale()/getMessages()`, wrap `NextIntlClientProvider`; `app/actions/locale.ts` Server Action `setLocale()`; `components/va/lang-switcher.tsx` (`VALangSwitcher`) đặt trong sidebar footer
- Web pilot pages dịch toàn bộ: `components/va/sidebar.tsx` (namespace `nav`+`common` — toàn bộ nav groups/items + tooltip đăng xuất), `app/(main)/dashboard/page.tsx` (namespace `dashboard` — title, breadcrumb date-locale, KPI, machine status, production/quality cards)
- Desktop: `Resources/Strings.resx` (default = Tiếng Việt) + `Resources/Strings.en-US.resx` (English satellite) + hand-written `Strings.Designer.cs`; `Localization/LocalizationManager.cs` (singleton, indexer, `SetLanguage()` live switch) + `Localization/LocExtension.cs` (`{loc:Loc Key=...}`); `AppSettings.Language` ("vi"|"en") load/save qua `local.json`, áp dụng tại `App.xaml.cs OnStartup`
- Desktop pilot pages dịch toàn bộ: `Views/LoginWindow.xaml` (title, app name/subtitle, hint, nút đăng nhập); `Views/Pages/SettingsPage.xaml` + `ViewModels/SettingsViewModel.cs` — thêm section "NGÔN NGỮ" (2 nút Tiếng Việt/English, `SetLanguageCommand` đổi ngay lập tức + lưu `local.json`), toàn bộ label tĩnh + message (`ConnectionStatus`, `SaveStatus`) chuyển sang `{loc:Loc}`/`LocalizationManager.Instance["Key"]`
- Pattern + danh sách trang đã/chưa dịch: xem section "i18n — English + Tiếng Việt" phía trên — dịch dần các trang còn lại theo đúng pattern này

---

## Coding Conventions

### Backend (C# / ASP.NET Core)

- Controller: thin — chỉ gọi MediatR, không chứa business logic
- Business logic 100% trong Application layer (MediatR handlers)
- Validate ở handler (FluentValidation pipeline behavior)
- Ghi migration sau mỗi thay đổi entity: `dotnet ef migrations add {Name}`
- Swagger annotation cho mọi endpoint mới
- Không hardcode credential, URL, port — dùng `appsettings.json` / env vars

### Web Client (Next.js / TypeScript)

- **Server Components mặc định** — chỉ `"use client"` khi cần interactivity (event handlers, hooks)
- **Không dùng `any`** — type everything
- **TanStack Query** cho server state (không dùng useState + useEffect để fetch)
- **Zod** validate form input tại boundary — không validate ở giữa logic
- **Không hardcode URL** — dùng `NEXT_PUBLIC_API_URL` từ env
- **Next.js 16 có breaking changes** — đọc `clients/web/AGENTS.md` và `node_modules/next/dist/docs/` trước khi code

```typescript
// ✅ Server state với TanStack Query
const { data: jobs } = useQuery({
  queryKey: ['jobs', filters],
  queryFn: () => api.jobs.list(filters),
})

// ✅ Form với Zod
const schema = z.object({ value: z.number().min(0) })
```

### Desktop Client (WPF)

- KHÔNG kết nối DB trực tiếp — chỉ qua REST API
- JWT token lưu in-memory (không persist ra disk)
- `HttpClient` + `IApiClient` phải là **singleton** — token share giữa mọi ViewModel
- Trigger data load từ ViewModel (NavigateTo command), KHÔNG dùng `Loaded` event
- `WorkContext` là singleton ObservableObject — state chia sẻ giữa tất cả pages
- Touch target: Button `MinHeight=56`, TextBox `MinHeight=52`, DataGridRow `MinHeight=52`

### Chung

- Không comment WHAT — chỉ comment WHY khi logic không rõ ràng
- Không thêm error handling cho tình huống không thể xảy ra
- Không tạo abstraction sớm — đợi đến lần thứ 3 mới extract
- Tham khảo code cũ (ManageData, Vinam-MES) để hiểu nghiệp vụ, **không copy**

---

## Mapping công nghệ cũ → mới

| Cũ (WinForms) | Mới | Trạng thái |
|---|---|---|
| MySQL stored procedures | EF Core + MediatR handlers | ✅ |
| DevExpress XtraGrid | TanStack Table + shadcn | Web — Phase 5 |
| RDLC / DevExpress Report | QuestPDF | ✅ installed |
| FTP (`FtpClient.cs`) | MinIO pre-signed URL | ✅ |
| Outlook Interop | MailKit | ✅ |
| Office Interop Excel | ClosedXML | ✅ installed |
| FastColoredTextBox | Monaco Editor (web) / GcodeViewerBehavior (desktop) | ✅ desktop |
| GanttChart library | Frappe Gantt | Phase 5 |
| WinForms Timer (polling) | SignalR + TanStack Query refetch | Partial |
| `BindingSource` + DataTable | TanStack Query + TypeScript types | Web — in progress |
| `FormKeyboard` (virtual KB) | Custom WPF NumPad + QWERTY | ✅ |
| PdfiumViewer | WebView2 (WPF) | ✅ |
| `MySqlHelper.cs` static | EF Core Repositories | ✅ |

---

## Deploy Production

```
Nginx routing:
  shopfloor.factory.local        → Web client (Next.js)
  shopfloor.factory.local/api/*  → API backend
  shopfloor.factory.local/hub/*  → SignalR

Docker Compose:
  docker compose -f docker-compose.yml up -d
  (Cần .env từ .env.example)

⚠️ clients/web/Dockerfile chưa có — cần tạo trước khi deploy web service.
Desktop app: build riêng bằng dotnet publish, deploy thủ công lên từng PC CNC.
```

---

## Roadmap

| Phase | Scope | Trạng thái |
|---|---|---|
| 0 — Foundation | Infrastructure, DB scaffold, .NET | ✅ |
| 1 — Auth & HR | JWT, users, roles, SignalR | ✅ |
| 2 — Production Core | Jobs, Parts, OPs, Documents | ✅ |
| 3 — Quality | Dimensions, FAI, NCR, SPC | ✅ |
| 4 — Desktop MES | WPF, FAI tại máy, session management | ✅ |
| **Web UI** | VA design system + 18 routes (clients/web) | ✅ |
| 5 — Advanced | Gage, Planning, MQTT pipeline, Dashboard web | ⏳ |
| 6 — Polish & Open Source | Multi-factory, migration tool MySQL→PG, docs site, one-command setup | ⏳ |

**Web UI — ✅ Hoàn tất** (2026-06-04)
- VA warm industrial design system: tokens, sidebar, topbar, badge, kpi, card, btn, seg
- 18 routes — tất cả có VA shell, sidebar navigation
- `/parts` redesign → "Chi tiết kỹ thuật": master-detail Part list + Revision + Routing + OP flow
- `/jobs` redesign → "Lệnh SX & Sản phẩm": master-detail Job list + progress bar + serial grid
- API thật: `/jobs`, `/parts`, `/ncrs`, `/hr`, `/fai`; mock data: `/planning`, `/cnc`, `/gages`, `/calibration`, `/documents`, `/master`
- Fonts: Inter + Fraunces + JetBrains Mono (next/font/google)
- Theme: override shadcn CSS vars → VA palette

**Web UI — bổ sung Phase 5 gaps** (2026-06-10)
- `/hr`: nút "+ Tạo tài khoản" → `UserDialog` (create/edit user, load roles/userTypes/positions/workStatuses); menu "⋯" mỗi dòng → Sửa / Vô hiệu hoá-Kích hoạt (`api.users.update` với `isActive` toggle, soft-disable không xoá cứng); nút "Danh mục" → `ManageLookupsDialog` (Phòng ban: list+inline edit+thêm; Chức vụ: list+thêm — API chưa có update/delete cho Position)
- `UserDto` (Application layer) bổ sung `Sex, RoleId, UserTypeId, PositionId, WorkStatusId` — cần thiết để pre-select dropdown khi sửa user (additive, không cần migration)
- `/fai` (top-level "FAI & Đo kiểm" sidebar route, khác `/jobs/[id]/fai`): viết lại từ mock 100% → chọn Job (search) + Operation rồi render `FaiSheetDto` thật qua `FaiMatrix`
- Tách `components/fai/fai-matrix.tsx` dùng chung giữa `/fai` và `/jobs/[id]/fai` — tránh trùng lặp ~100 dòng matrix table

**Web UI — hợp nhất `/documents` + `/parts/[id]/documents`** (2026-06-10)
- Fix bug: `api.techDocuments.inspect()` gửi sai payload `{action, note}` — backend `InspectRequest(bool Approve, string? Note)` cần `{approve: boolean, note}`. Bug khiến bấm "Duyệt" luôn ghi `Approve=false` → tài liệu bị set Rejected thay vì Approved.
- `api.techDocuments.list()` thêm params `partRevId`/`partOpId`/`jobId`; thêm `fileTypes()` (`GET /api/v1/tech-documents/file-types`) và `create()` (`POST /api/v1/tech-documents` → presigned upload URL)
- `/documents` (Tài liệu KT) đọc query params `partRevId`/`partOpId`/`jobId` (filter data) + `partNumber`/`opNumber`/`revCode`/`jobNumber`/`backHref` (hiển thị) — breadcrumb/title động theo context; nút "← Quay lại" khi có `backHref`; nút "⬆ Upload" + form upload (port từ trang cũ) chỉ hiện khi có context (`partRevId` hoặc `partOpId`)
- Xoá `/parts/[id]/documents/page.tsx` (route + thư mục) — đã hợp nhất vào `/documents`
- `/parts/[id]`: 2 nút "Bản vẽ / CAD" và "Tài liệu →" giờ trỏ sang `/documents?partRevId=...` / `/documents?partOpId=...&...&backHref=/parts/{id}` thay vì trang riêng
- **Lưu ý**: `/jobs/[id]/documents` (trang doc cho ForJobOnly OP — RTC/FXT) vẫn là trang độc lập, chưa hợp nhất — out of scope đợt này, có thể áp dụng cùng pattern sau

**Web UI — fix scroll trên toàn bộ (main) routes** (2026-06-10)
- Bug: hầu hết các trang không cuộn được danh sách/bảng — page root div thiếu `minHeight: 0` nên bị `(main)/layout.tsx`'s `overflow: hidden` clip nội dung thay vì cho cuộn
- Fix: thêm `minHeight: 0` vào page root style của 15 trang — `dashboard`, `jobs`, `jobs/[id]`, `jobs/[id]/fai` (4 nhánh return), `parts`, `parts/[id]`, `planning`, `cnc`, `fai`, `ncrs`, `gages`, `calibration`, `documents`, `hr`, `master`
- Fix bảng "Operations"/"Chi tiết công đoạn" trong `parts/page.tsx` và `parts/[id]/page.tsx`: đổi `minHeight: <số cố định>` → `minHeight: 0` trên `VACard`, bọc `<table>` trong `<div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>` + sticky `<th>` — theo đúng pattern đã có ở `gages`/`hr`/`documents`
- Xem chi tiết pattern tại "Lưu ý kỹ thuật — Scroll trong layout flex" phía trên — áp dụng cho mọi trang/bảng mới sau này

**Master Data CRUD** (2026-06-10)
- Thêm `IsActive` (bool, default true) vào 4 entity trước đây thiếu: `MachineGroup`, `OpType`, `DimensionCategory`, `FileType` — migration `AddIsActiveToMasterDataLookups` (cột mới `defaultValue: true` để dữ liệu cũ không bị ẩn)
- `MasterDataController` mới (`src/ShopfloorManager.API/Controllers/MasterDataController.cs`) — gom toàn bộ GET/POST/PUT cho 5 entity: `/api/v1/machines`, `/api/v1/machine-groups`, `/api/v1/op-types`, `/api/v1/dimension-categories`, `/api/v1/tech-documents/file-types` (`activeOnly` query param, default `false` trừ machines `true`); POST/PUT yêu cầu role Administrator
- `LookupsController` chỉ còn Positions/UserTypes/WorkStatuses/NcrReasons — các GET op-types/dimension-categories/machines/machine-groups/file-types đã chuyển sang `MasterDataController` (tránh route trùng)
- Application layer: `MachineCommands.cs`, `OpTypeCommands.cs`, `DimensionCategoryCommands.cs`, `FileTypeCommands.cs` (mới) + `MachineQueries.cs` cập nhật DTO thêm `IsActive`/`SerialNumber`
- Web `/master`: nút "+ Thêm mục" + click vào dòng để sửa → `MasterItemDialog` (`components/master/master-item-dialog.tsx`) — 1 dialog dùng chung cho cả 5 tab, field theo `kind`; mỗi bảng thêm cột "Trạng thái" (`VABadge` Hoạt động/Đã ẩn)
- `api-client.ts`: `machines`/`machineGroups`/`opTypes`/`dimCategories`/`fileTypes2` đều có `list(activeOnly)`, `create()`, `update()`; types mới `MachineDto`, `MachineGroupDto`, `OpTypeDto`, `DimensionCategoryDto`, `FileTypeDto` (thêm `isActive`)

**`/parts` — CRUD Drawing Rev/Routing Rev/OP + Excel import + i18n** (2026-06-11)
- 3 dialog mới (`components/parts/`): `AddRevisionDialog` (`POST /parts/{id}/revisions`, helper text "tự tạo Routing Standard R1"), `AddRoutingRevDialog` (`POST /parts/routing-revs`, copy OP từ rev active), `AddOpDialog` (`POST /operations`, chọn OpType từ `api.opTypes.list(true)`)
- `/parts` và `/parts/[id]`: VACard "Drawing Rev"/"Routing Rev"/"Operations" đều có nút "+" trong `right` prop để mở dialog tương ứng; mỗi OP row có nút "⤓ Dims" để import dimensions cho riêng OP đó
- **Import Excel — Operations**: `POST /api/v1/operations/import` (`multipart/form-data`: `file` + `routingRevId`, role Administrator/Manager/Engineer) — upsert theo `OpNumber` (đã tồn tại → update Description/OpType/SetupTime/ProdTime, giữ nguyên Dimensions/TechDocuments; chưa có → tạo mới). Cột Excel (header không phân biệt hoa/thường, bỏ space): `OpNumber`/`Op`, `OpType` (code, vd "CNC"/"GRIND" — không match → warning, vẫn import với OpTypeId=null), `Description`, `Setup`/`SetupTime`, `Prod`/`ProdTime`
- **Import Excel — Dimensions**: `POST /api/v1/operations/{opId}/dimensions/import` (role +QC Inspector) — chỉ tạo mới, bỏ qua (skip) nếu `BalloonNumber` đã tồn tại trong OP đó. Cột Excel: `BalloonNumber`/`Balloon`, `Code`, `Description`, `Nominal` (numeric → dimension số với `TolPlus`/`TolMinus`/`Unit`(default mm)/tính `MaxValue`/`MinValue`; không parse được số → `IsTextType=true`, lưu `NominalText`), `TolPlus`/`Tol+`, `TolMinus`/`Tol-`, `Unit`, `Category` (code LIN/ANG/THD/GEO/SFC — không match → warning, vẫn tạo với CategoryId=null)
- Cả 2 import endpoint trả `ImportResultDto { created, updated, skipped, errors: [{ rowNumber, message }] }` — `ImportOpsDialog`/`ImportDimensionsDialog` hiển thị kết quả + danh sách lỗi/cảnh báo theo dòng
- `ExcelImportReader` (`src/ShopfloorManager.API/Common/`): đọc sheet đầu, dòng 1 = header (normalize lower+trim+bỏ space); **trả về `List<Dictionary<string,string>>` (giá trị cell đã extract sẵn)**, KHÔNG trả `IXLRow` — vì `XLWorkbook` bị dispose (`using`) trước khi LINQ `.Select().ToList()` ở controller chạy → `ObjectDisposedException` nếu giữ reference `IXLRow`
- i18n: namespace `parts` đầy đủ cho `/parts`, `/parts/[id]` và 5 dialog mới (`addRevision`/`addRoutingRev`/`addOp`/`importOps`/`importDims`)

**Web UI Redesign — kế hoạch (Claude design, 2026-06-12)**

Đã nhận bộ thiết kế mới (AI design tool, dựa trên `Project_Documents/01-13`) — tái cấu trúc sidebar thành 5 nhóm + thêm trang "Dimension Sheet" + cập nhật nội dung nhiều trang. Quyết định đã chốt với user:
- Nhóm sidebar cuối cùng vẫn giữ tên **"Hệ thống"** (HR + Master Data) — không đổi thành "Master Data"
- Theme color **giữ nguyên `#6D3B1A`** — không đổi sang preset "caphe" `#553421` trong mockup
- NCR: **redesign đầy đủ** theo workflow 5 bước (Phát hiện → Phân loại → Quyết định → Xác minh → Đóng) — cần migration, thiết kế chi tiết ở `07_ncr.md` (mục "UI Redesign — Phase I")
- Master Data: **redesign theo mockup** (`va-master.jsx`) — chi tiết ở `13_master_data.md` (mục "UI Redesign — Phase K"), cần xử lý lại tab MachineGroup đã có CRUD (2026-06-10) nhưng không có trong mockup

| Phase | Nội dung | File thiết kế chi tiết | Trạng thái |
|---|---|---|---|
| A | Sidebar 5 nhóm (Tổng quan/Kỹ thuật/Sản xuất/Chất lượng/Hệ thống) + i18n, thêm placeholder route `dimsheet` | `messages/*.json`, `components/va/sidebar.tsx` | ✅ |
| B | Trang "Dimension Sheet" (`/dimsheet`) — bảng tổng hợp dimension toàn Part across OP | `04_routing_operations.md` § UI Redesign — Phase B | ✅ |
| C | FAI: stat strip (Inspector, Pass/Fail/Pending, Pass rate%) | `06_dimensions_fai.md` § UI Redesign — Phase C | ⏳ |
| D | Jobs: "Tiến độ đo kiểm" progress bar + routing OP strip thành read-only reference → Part&Routing | `03_job_management.md` § UI Redesign — Phase D | ⏳ |
| E | Documents: cascading filter Part→Drawing Rev→Routing Rev→OP cho truy cập top-level | `05_technical_documents.md` § UI Redesign — Phase E | ✅ |
| F | Jobs: Serial/Product grid 4 trạng thái (available/claimed/inprogress/complete) — cần ProductionSession status trong ProductDto | `03_job_management.md` § UI Redesign — Phase F | ⏳ |
| G | Part & Routing: KPI strip, revision history timeline, drawing 2D placeholder, OP detail tabs (Tài liệu/Dimension) — điều chỉnh: tách 2 trang `/parts` (list+overview) và `/parts/[id]/operations` (OP detail) | `04_routing_operations.md` § UI Redesign — Phase G | ✅ |
| H | HR: org tree phòng ban (bên trái) + user table (bên phải) | `02_hr.md` § UI Redesign — Phase H | ⏳ |
| I | NCR: redesign đầy đủ — workflow 5 bước, thêm bước "Xác minh" (Verification) | `07_ncr.md` § UI Redesign — Phase I | ⏳ |
| J | FAI: panel "Chi tiết Balloon" — measure history + distribution chart + "Mở NCR cho ô này" | `06_dimensions_fai.md` § UI Redesign — Phase J | ⏳ |
| K | Master Data: redesign theo `va-master.jsx` — Machines/OpTypes/DimCategories/Fixtures(mới)/DocumentTypes, xử lý lại MachineGroup | `13_master_data.md` § UI Redesign — Phase K | ⏳ |

**Quy trình triển khai (bắt buộc theo từng phase):**
1. Mô tả chi tiết việc sẽ làm + thay đổi dự kiến (UI, API, schema nếu có) cho phase đó
2. User review → confirm
3. Implement (theo Clean Architecture, đúng quy trình "Triển khai tính năng — quy trình bắt buộc")
4. Build (`dotnet build` / `npm run build` hoặc dev server) — kiểm tra thực tế trên browser
5. Cập nhật log kết quả vào CLAUDE.md (mục tương ứng phase), đánh dấu ✅

**Phase G — Part & Routing tách 2 trang (✅ 2026-06-12)**
- Theo yêu cầu user, Phase G tách thành 2 trang thay vì 1 trang duy nhất như mockup gốc:
  - **`/parts` (Page 1)** — giữ master-detail Part list + Drawing Rev/Routing Rev selector + OP table, **bỏ OP detail panel**. Thêm KPI strip (Operations/Jobs/Dimensions/Current Routing — `t('kpi.*')`), card "Bản vẽ 2D" (link sang `/documents?partRevId=...`), info "Tạo bởi {name} · {date}" trên Drawing Rev card. OP table thêm cột "Documents"/"Dim" (số lượng), bỏ nút action — click row → điều hướng `/parts/{id}/operations?routingRevId=...&opId=...`. Sidebar Part list hiển thị thêm routing code + op/job count + ngày tạo.
  - **`/parts/[id]/operations` (Page 2, file mới)** — master-detail: danh sách OP bên trái (click chọn), detail bên phải gồm `VASeg` 2 tab "Tài liệu"/"Dimension" (đúng nội dung OP detail panel trong mockup). Tab Tài liệu: bảng TechDocument + link "Quản lý tài liệu →" sang `/documents?partOpId=...`. Tab Dimension: bảng Dimension (balloon/category/nominal/tol/max/min/unit/final) + nút "⤓ Dims" mở `ImportDimensionsDialog` (chuyển từ Page 1 sang đây).
  - Xoá `app/(main)/parts/[id]/page.tsx` (trang detail cũ, 1 trang duy nhất) — đã được thay thế hoàn toàn bởi Page 1 + Page 2.
- i18n: thêm đầy đủ key `parts.operationsLink`, `parts.kpi.*`, `parts.drawing2d.*`, `parts.drawingRev.createdBy`, `parts.opTable.headers.dim`, `parts.opDetail.*` (breadcrumb/opList/selectOp/tabs/documents/dimensions) cho cả `vi.json` và `en.json`.
- Build: `npm run build` — 0 lỗi TypeScript, 18 routes (bao gồm `/parts` và `/parts/[id]/operations` — cả 2 đều `ƒ` dynamic). Lưu ý: sau khi xoá `parts/[id]/page.tsx`, cần `rm -rf .next` để xoá stale Turbopack type-check cache trước khi build lại.
- Verify: dev server log cho thấy đã duyệt qua `/parts/105/operations?routingRevId=105&opId=...` cho cả 12 OP của part 105 — load thành công, không lỗi runtime.
- **Out of scope (deferred — cần migration riêng)**: Part status (draft/active/complete) + "Confirm Part" workflow, `Part.material`/`Part.type` fields.

**Phase B — Dimension Sheet (✅ 2026-06-12)**
- Backend: `GetDimensionsByRoutingRevQuery` (`FaiCommands.cs`) → `RoutingRevDimensionDto` (opId/opNumber/opNumberSort, balloon/balloonSort, nominal/tol/max/min, unit, isTextType/nominalText, categoryCode, isCritical/isFinal, sortOrder) — join Dimension → PartOp → RoutingRev, order by `opNumberSort` rồi `balloonSort`. Endpoint `GET /api/v1/routing-revs/{routingRevId}/dimensions` (absolute route trong `OperationsController`).
- Backend: `UpdateDimensionCommand`/Validator/Handler — `PUT /api/v1/dimensions/{id}` nhận `{nominalValue, tolerancePlus, toleranceMinus}`, tự tính lại `MaxValue/MinValue`, chặn sửa dimension `IsTextType=true`.
- Frontend `/dimsheet` (mới): layout master-detail giống `/parts` — panel trái search/chọn Part (`api.parts.list`), panel phải `DimSheetDetail` cascading load: `api.parts.revisions(partId)` → active PartRev → `api.parts.routingRevs(revId)` → active RoutingRev → `api.routingRevs.dimensions(routingRevId)`. Bảng 10 cột (OP/Balloon/Loại/Nominal/Tol+/Tol-/Max/Min/ĐV/Final) + cột action inline-edit (✎ → 3 input Nominal/Tol+/Tol- + preview Max/Min realtime → ✓ lưu/✕ huỷ), dimension `IsTextType` hiện colSpan thay vì input.
- `api-client.ts`: thêm `RoutingRevDimensionDto`, `routingRevs.dimensions(id)`, `dimensions.update(id, body)`.
- i18n: namespace `dimsheet` đầy đủ (vi+en) — title/breadcrumb/search/table headers/edit tooltip.
- Sidebar: `dimsheet` nav item đổi `live: false` → `true` (bỏ badge "SOON").
- **Lưu ý quan trọng — API process phải restart sau khi thêm route mới**: route `GET /api/v1/routing-revs/{id}/dimensions` trả 404 khi test qua `curl` dù code đã build thành công — vì tiến trình `dotnet run` đang chạy là build CŨ (trước khi thêm controller method). Build riêng từng `.csproj` (để tránh file-lock) KHÔNG cập nhật code của process đang chạy — phải kill process cũ (`Stop-Process`) và `dotnet run` lại để route mới có hiệu lực.
- **Lesson — KHÔNG chạy `npm run build` (production) khi `npm run dev` đang chạy trên cùng `.next`**: 2 process tranh nhau ghi `.next`, làm dev server trả "Internal Server Error" cho mọi route. Nếu cần build production để kiểm tra type-check, dừng dev server trước hoặc build ra dir riêng (`-o`); dev server (Turbopack) đã tự type-check khi chạy.
- Verify: browser test part "00210155402" (RING,SHOULDER-STAB) — Routing 001 · 9 dim, bảng hiển thị đúng 9 dòng (kể cả 2 dòng `IsTextType` SFC "Rq" colSpan). Inline-edit dimension `1*` (balloon LIN, Nominal 8.6254/Tol±0.004) — sửa Nominal→8.7, lưu → `PUT /api/v1/dimensions/10120` thành công, Max/Min cập nhật 8.704/8.696; sửa lại về 8.6254 → Max/Min trả về 8.6294/8.6214 đúng. Không có lỗi console (chỉ warning a11y "form field thiếu id/name" — pattern có sẵn từ các trang khác).

**Phase E — Documents redesign: flat-list filter (✅ 2026-06-13, thay thế cascading selector 2026-06-12)**
- User phản hồi UI cascading selector (Phase E gốc, 2026-06-12) không đúng — yêu cầu viết lại `/documents` theo mockup `va-docs.jsx` (flat-list filterable, 1 trang duy nhất, không qua nhiều bước chọn).
- Xoá `components/documents/doc-selector.tsx` + thư mục `components/documents/` (component `DocSelector` của Phase E gốc không còn dùng).
- Migration `AddFileSizeToTechDocuments`: thêm `tech_documents.file_size_bytes BIGINT NULL`. `TechDocument` entity thêm `FileSizeBytes`; `TechDocDto`/`TechDocListDto`/`UploadRequest`/`UploadDocBody` đều có `fileSizeBytes`; `RequestUpload` lưu `req.FileSizeBytes` (cả nhánh tạo mới và nhánh update existing/Rejected).
- `documents/page.tsx` viết lại toàn bộ — flat list + filter bar:
  - `GET /api/v1/tech-documents` (không filter) → load TOÀN BỘ docs 1 lần; lọc client-side qua `useMemo`
  - Filter bar: Part · Drawing Rev · Routing Rev · OP (cascading theo `fPart` — chọn Part reset 3 filter sau) + separator + Loại (file type) + Trạng thái + tìm tên file; đếm `{filtered.length}/{docs.length}` + "✕ Xóa lọc"
  - Type legend chips (DRW/GCD/RTC/FXT/THD/TLS/CAM/CAD, màu riêng từng loại) — click để filter nhanh theo `fType`, click lại để bỏ
  - KPI strip (Tổng/Chờ duyệt/Đã duyệt/Từ chối) + pending banner "N tài liệu chờ Inspector duyệt"
  - Bảng 10 cột: Tên file/Loại/Part/Routing/OP/Rev/Trạng thái/Người tạo/**Kích thước**/action — cột Kích thước dùng `formatBytes()` (B/KB/MB, `—` nếu null)
  - Vẫn giữ context-aware behavior cho 3 inbound link cũ (`/parts`, `/parts/[id]/operations`, `/jobs/[id]`): query params `partRevId`/`partOpId`/`jobId`/`backHref` pre-seed filter (`fPart`/`fRev`/`fOp`) + hiện nút "← Quay lại" + nút "⬆ Upload" (chỉ hiện khi `hasContext`)
- Verify browser (4 scenario, console không lỗi — chỉ warning a11y có sẵn):
  1. `/documents?partRevId=105&partNumber=00210155402&revCode=E&backHref=%2Fparts` — filter pre-seed đúng (Part=00210155402, Rev=E), 2/9 rows, "← Quay lại" → `/parts`, "⬆ Upload" hiện
  2. `/documents` (sidebar, không context) — Upload ẩn, full list, "✕ Xóa lọc" hoạt động đúng (reset → hiện thêm option Rev/Routing/OP mới)
  3. Type legend toggle GCD → lọc đúng 2 file `.nc`, toggle lại → bỏ lọc
  4. **Upload + Approve + Reject + Xem (đầy đủ vòng đời)**: upload `test_tls_upload.txt` (loại TLS) → xuất hiện "Chờ duyệt" 47 B, KPI Pending 1 → "Duyệt" → KPI Approved 8/Pending 0, badge "Đã duyệt" → "Xem →" mở presigned MinIO URL đúng path `tools/00210155402/E/test_tls_upload.txt`; upload `test_cam_reject.txt` (loại CAM) → "Từ chối" (prompt lý do) → KPI Rejected 1, badge "Từ chối" — toàn bộ luồng hoạt động đúng, `fileSizeBytes` lưu/hiển thị chính xác.
- **Lưu ý**: `formatBytes()` hiện `—` cho các doc cũ chưa có `fileSizeBytes` (test_drw_v2.pdf, test_model.step — tạo trước khi có cột này) — đúng như thiết kế.

**`/documents` — filter bar combobox gõ để tìm (✅ 2026-06-13)**
- User phản hồi: số lượng Part trong thực tế rất lớn, `<select>` thường không khả dụng để chọn Part — cần gõ để tìm (type-to-search), áp dụng cho mọi combobox trong filter bar.
- Component mới `components/va/combobox.tsx` (`VACombobox` + type `VAComboboxOption{value,label}`), export qua `components/va/index.ts` — dựng trên `@base-ui/react` `Combobox` primitive (dependency có sẵn, không cần thêm package). `Combobox.Root` controlled bằng `value`/`onValueChange` + `isItemEqualToValue` (so theo `value` vì object option tạo lại mỗi render) + `itemToStringLabel` (filter theo label).
- Áp dụng cho cả 6 combobox filter bar `/documents`: Part, Drawing Rev, Routing Rev, OP, Loại, Trạng thái — mỗi combobox có list option riêng (`partOptions`/`revOptions`/`routOptions`/`opOptions`/`typeOptions`/`statusOptions`, đều `useMemo`), thay thế `<select>` cũ.
- CSS mới trong `globals.css`: `.va-combobox-group:focus-within` (viền cam khi focus), `.va-combobox-item[data-highlighted]`/`[data-selected]` (highlight item trong dropdown).
- **Lesson**: `Combobox.Input` không tự select-all text khi focus — click vào sẽ đặt cursor giữa label hiện tại, gõ tiếp sẽ chèn vào giữa (vd "Tất cả paSHAFTrt") thay vì thay thế. Fix: `onFocus={e => e.currentTarget.select()}` trên `Combobox.Input`.
- Verify browser: `/documents` → 6 combobox đều render `role=combobox` + nút `▾` riêng (thay `<select>`); gõ "SHAFT" vào Part → input hiện đúng "SHAFT" (không bị chèn giữa), dropdown lọc còn "SHAFT-50H6" → ArrowDown+Enter chọn → bảng lọc 4/9 (đúng 4 doc của SHAFT-50H6), Drawing/Routing/OP reset về "Mọi..." (cascading `pickPart`); "✕ Xóa lọc" → Part về "Tất cả part", bảng về 9/9. Không lỗi console.
- **Tái sử dụng**: `VACombobox` là component chung — có thể áp dụng cho các select danh sách lớn khác trong app (vd `/parts`, `/jobs` filter) khi cần, ngoài phạm vi đợt này.

**Dimension Sheet (`/dimsheet`) — redesign theo mockup `va-dimsheet.jsx` (✅ 2026-06-13)**
- User cung cấp mockup mới (`Shopfloor Manage.zip` trên OneDrive, file `src/va-dimsheet.jsx`) — viết lại toàn bộ panel phải (`DimSheetDetail`) của `/dimsheet` theo layout này. Panel trái (Part list + ô tìm kiếm) giữ nguyên theo yêu cầu user.
- Header: part number (mono, lớn) + `VABadge kind="primary"` "Bản vẽ Rev {rev}" (từ `partRevCode` — active PartRev) + description.
- KPI strip (4 `VAKpi`): Tổng dimension (`dims.length`), Balloon unique (`new Set(dims.map(d=>d.balloonNumber)).size`), FAI Final (`dims.filter(d=>d.isFinal).length`, sub "chỉ QC nhập"), Số OP có dim (`ops.filter(o=>o.dimCount>0).length` / tổng `ops.length`, qua `api.operations.listForRoutingRev`).
- Filter bar (1 dòng, `VACard`-style): select OP (sort theo `opNumberSort`) · category chips (`ALL_CATS` + `CAT_COLORS = {LIN: va.primary, ANG: va.accent, THD: va.primaryLt, GEO: '#5D4037', SFC: '#795548'}`, đếm số dim mỗi loại) · checkbox "Chỉ FAI Final" · input tìm balloon + counter `{filtered.length}/{dims.length}`.
- Master table (`TABLE_COLS` thứ tự mới: OP/Balloon/Loại/Nominal/Tol+/Tol-/Max/Min/ĐV/Final + action): balloon hiện dạng circle badge (viền đỏ nếu `isCritical`), OP hiện badge nền `va.primary`, category code màu theo `CAT_COLORS`, Max màu `va.ok` xanh / Min màu `va.err` đỏ, Tol+/- có dấu `+`/`−`, Final hiện `●` (primary) hoặc `—`. Dimension `isTextType` dùng `colSpan={5}` hiện `nominalText`. Inline-edit (✎/✓/✕) giữ nguyên logic cũ (`startEdit`/`handleSave`/`previewLimit`), không cho edit dimension text-type.
- Empty states mới: `noRouting` (Part chưa có Routing active), `empty` (chưa có dimension nào), `noMatch` (filter không khớp).
- Footnote cuối trang giải thích balloon có thể đo lại nhiều OP + dòng Final là điểm chốt FAI.
- i18n: thêm key mới vào `dimsheet` namespace (vi+en) — `revBadge`, `noMatch`, `footnote`, `kpi.*`, `filter.*`.
- **Bỏ khỏi mockup (chưa có API/tránh nửa vời)**: cột "Dụng cụ đo" (gage) — chưa có liên kết Dimension↔Gage; nút "⬆ Export Excel" — chưa có endpoint export.
- Verify: `npx tsc --noEmit` 0 lỗi. Browser test 2 part: `00210155402` (9 dim, LIN/SFC, "2/12 OP") và `002H061671800` (36 dim, LIN/GEO, "3/10 OP") — KPI đúng, filter category chip (LIN → 7/9), checkbox "Chỉ FAI Final" (→7/9), tìm balloon không khớp → `noMatch` 0/9, inline-edit mở/huỷ vẫn hoạt động, đổi Part reset filter + load lại đúng. Không lỗi console (chỉ warning a11y có sẵn).
- **Bổ sung — combobox gõ để tìm cho filter OP (✅ 2026-06-13)**: select OP trong filter bar đổi sang `VACombobox` (cùng component dùng ở `/documents`) — options `[{value:'all', label:t('filter.allOps')}, ...sortedOps.map(...)]`. Verify: gõ "70" → dropdown lọc còn đúng option "70", Enter chọn → bảng lọc 4/9 (đúng 4 dim OP=70); xoá input → list đầy đủ tất cả OP, gõ "Tất" → chọn "Tất cả" → reset về 9/9. Không lỗi console.

**`/documents` — i18n English (✅ 2026-06-13)**
- Hoàn tất nhóm sidebar "Kỹ thuật" (3 view: `/parts`+`/parts/[id]/operations`, `/dimsheet`, `/documents`) — `/documents` là view cuối cùng còn hardcode tiếng Việt 100% (0 `useTranslations`).
- Thêm namespace `documents` đầy đủ vào `messages/vi.json` + `messages/en.json`: `title`/`breadcrumb`/`backLink`/`queueButton`/`uploadButton`/`loading`, `kpi.*` (total/pending/approved/rejected), `pendingBanner.*` (title có `{count}`, sub, action), `upload.*` (title/fileType/fileTypePlaceholder/revision/revisionPlaceholder/file/description/cancel/submit/submitting/errorGeneric), `filter.*` (part/drawingRev/routingRev/op/type/status/search/searchPlaceholder/clear + 11 option label: allParts/allRevs/noRev/revPrefix `{rev}`/allRouting/noRouting/allOps/noOp/opPrefix `{op}`/allTypes/allStatuses), `table.*` (headers.* 9 cột + empty/noMatch/clearFilter), `actions.*` (reject/approve/view/rejectPrompt/errApprove/errReject/errViewUrl), `status.*` (Pending/Approved/Rejected).
- `STATUS_META: Record<string,{label,kind}>` → tách thành `STATUS_KIND: Record<string,VaBadgeKind>` (chỉ giữ màu badge); label lấy qua `t(\`status.${d.status}\`)` (pattern key động giống dashboard production/quality cards).
- 6 combobox option list (`partOptions`/`revOptions`/`routOptions`/`opOptions`/`typeOptions`/`statusOptions`) đổi label hardcode → `t('filter.allX')` + `t('filter.revPrefix', {rev})`/`t('filter.opPrefix', {op})`/`t('filter.noRev')`/`t('filter.noRouting')`/`t('filter.noOp')`.
- `alert`/`prompt` messages (lỗi duyệt/từ chối/upload, "Lý do từ chối:", "Không tải được URL tài liệu") → `t('actions.*')`/`t('upload.errorGeneric')`.
- Date cột "Người tạo": `new Date(d.createdAt).toLocaleDateString('vi-VN')` (hardcode) → `useLocale()` + `toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US')` — đúng pattern chuẩn của dự án.
- Verify: `npx tsc --noEmit` 0 lỗi. Browser test VI→EN qua `VALangSwitcher`: toàn bộ topbar/KPI/banner/filter bar (label + placeholder + 6 combobox option)/type legend/table header/status badge/action button đều dịch đúng; ngày "13/6/2026" (VI) ↔ "6/13/2026" (EN); dropdown Drawing Rev hiện "Rev A"/"Rev E"/"Rev NONE" (EN) đúng `revPrefix`. Không lỗi console ở cả 2 ngôn ngữ.

**Phase 6 chi tiết:**
- Multi-factory support (FactoryId đã chuẩn bị trên Machine entity)
- Migration tool: MySQL → PostgreSQL (C# console app, đọc từ DB cũ)
- Documentation site
- Docker polish, one-command setup
- Python analytics service (cân nhắc nếu SPC nâng cao C# không đủ)

---

## Source Code Reference (cũ → mới)

Khi implement tính năng, tham khảo business logic tại:

| Tính năng | Source cũ (đọc để hiểu logic) |
|---|---|
| FAI đo kiểm | `Vinam-MES/FANUC/Forms/FormFAI.cs` |
| Process Monitor | `Vinam-MES/FANUC/Forms/FormProcessMonitor.cs` |
| NCR tại máy | `Vinam-MES/FANUC/Common/MySqlHelper.cs` (AddNCR, RenderNCRCode) |
| Tech Documents | `ManageData/Common/Techdocuments/StoreTechdocuments.cs` |
| Dimension import | `ManageData/Forms/FormUpdateDimension.cs` |
| FAI Report | `ManageData/Forms/Report/DimensionFAI/FormReportFAI.cs` |
| Planning Gantt | `ManageData/Forms/Planning/FormManagePlanning.cs` |
| Dashboard | `ManageData/Common/Dashboard/StoreDashboard.cs` |

**Không copy code cũ.** Chỉ tham khảo business logic.

---

## Rules for Claude

**Luôn trả lời bằng tiếng Việt** (kể cả khi người dùng hỏi bằng tiếng Anh).

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
| Máy móc, MachineGroup, Epicor ResourceGroup | [`Project_Documents/17_machines_equipment.md`](Project_Documents/17_machines_equipment.md) |
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
