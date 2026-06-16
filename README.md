# Shopfloor Manager

**Open-source factory management system for CNC machining shops** — self-hosted, no vendor lock-in, built with .NET 9 + Next.js + PostgreSQL.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-blueviolet)](https://dotnet.microsoft.com/)
[![Next.js 16](https://img.shields.io/badge/Next.js-16-black)](https://nextjs.org/)
[![PostgreSQL 16](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)

Full replacement for a legacy WinForms system (ManageData + Vinam-MES) used in precision CNC machining shops of 50–200 people. All business logic lives in C#; the database stores data only — no stored procedures, no triggers.

---

## Screenshots

| Web App — Jobs & Production | Web App — Parts & Operations |
|---|---|
| ![Jobs](docs/screenshots/web-jobs-detail.png) | ![Parts](docs/screenshots/web-parts-operations.png) |

| Dimension Sheet | Technical Documents |
|---|---|
| ![Dimsheet](docs/screenshots/web-dimsheet-detail.png) | ![Documents](docs/screenshots/web-documents.png) |

| ERP Import — Preview | Bulk Job Import |
|---|---|
| ![ERP Preview](docs/screenshots/web-jobs-erp-preview.png) | ![Bulk Import](docs/screenshots/web-jobs-bulk-import.png) |

---

## Feature Documentation

Each feature has its own detailed page with screenshots, business rules, and API reference:

| Module | Route | Description |
|---|---|---|
| [Jobs & Production](docs/features/jobs.md) | `/jobs` | Production orders, serial tracking, progress, ERP import, bulk Excel import |
| [Parts & Routing](docs/features/parts-routing.md) | `/parts` `/parts/{id}/operations` | Part catalog, revision history, routing, operations, dimension import |
| [Dimension Sheet](docs/features/dimension-sheet.md) | `/dimsheet` | Bird's-eye view of all inspection dimensions across a routing |
| [Technical Documents](docs/features/documents.md) | `/documents` | Upload, version, approve DRW/GCD/RTC/FXT/TLS/CAM/THD/CAD files; bulk upload |
| [FAI & Measurement](docs/features/fai.md) | `/fai` | First Article Inspection matrix, measurement entry, SPC (Cpk/Cp) |
| [NCR](docs/features/ncr.md) | `/ncrs` | Non-Conformance Reports — creation, disposition, real-time notifications |
| [Gages](docs/features/gages.md) | `/gages` | Metrology equipment inventory, borrow/return, calibration due dates |
| [Calibration](docs/features/calibration.md) | `/calibration` | Calibration requests, vendor records, certificate tracking |
| [HR & Users](docs/features/hr.md) | `/hr` | User accounts, roles, departments, positions |
| [Master Data](docs/features/master-data.md) | `/master` | Machines, machine groups, op types, dim categories, document types |
| [ERP Integration](docs/features/erp-integration.md) | `/jobs` (dialog) | Pull jobs from Epicor / OData v4 — extensible connector architecture |
| [Planning](docs/features/planning.md) | `/planning` | Weekly Gantt chart *(Phase 5 — UI scaffold, API planned)* |
| [CNC Monitoring](docs/features/cnc-monitoring.md) | `/cnc` | Real-time MQTT machine status *(Phase 5 — UI scaffold, MQTT planned)* |
| [Desktop MES](docs/features/desktop-mes.md) | WPF app | Touchscreen app at CNC machines — FAI entry, NCR, document viewer, session management |

---

## System Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│  Web App  (Next.js 16 — clients/web)                             │
│  Office UI: Engineering · Management · QC · Planning             │
│  Routes: /jobs  /parts  /dimsheet  /documents  /fai  /ncrs      │
│          /gages  /calibration  /hr  /master  /planning  /cnc    │
│  → Accessible from any PC or tablet via browser                  │
└───────────────────────────┬──────────────────────────────────────┘
                            │ REST API (JSON) + SignalR (WebSocket)
┌───────────────────────────▼──────────────────────────────────────┐
│  ASP.NET Core Web API (.NET 9) — src/ShopfloorManager.API        │
│  Clean Architecture · MediatR · FluentValidation · JWT Auth      │
│  Business logic 100% here — no stored procedures                 │
└───┬───────────────────┬──────────────────┬────────────────────────┘
    │                   │                  │
PostgreSQL 16        MinIO              SignalR Hub
(data only)       (drawings,          (real-time push
                   G-code, PDFs)       to Web + Desktop)
                                              │
                               ┌──────────────▼──────────────────┐
                               │  Desktop App (WPF .NET 9)       │
                               │  src/ShopfloorManager.Desktop   │
                               │  Touchscreen MES at CNC machine │
                               │  FAI · NCR · G-code · PDF viewer│
                               └──────────────┬──────────────────┘
                                              │ MQTT
                               ┌──────────────▼──────────────────┐
                               │  Mosquitto MQTT Broker          │
                               │  CNC data: FANUC / MTConnect    │
                               └─────────────────────────────────┘
```

**Request flow:**
```
HTTP Request
  → Controller (thin — delegates to IMediator.Send)
  → MediatR Handler (all business logic)
  → Repository / Service interfaces (Application layer)
  → EF Core / MinIO / SignalR / MQTT
```

---

## Design Philosophy

- **Self-hosted first** — one `docker compose up` runs the entire stack on an internal Linux server.
- **Solo-developer friendly** — no over-engineering; the simplest solution that works.
- **Business logic 100% in the API** — database stores data only. No stored procedures, no triggers.
- **All MIT/Apache 2.0 dependencies** — no commercial library lock-in.
- **Audit trail everywhere** — every record carries `created_by`, `updated_by`, `created_at`, `updated_at`.

---

## Tech Stack

### Backend (.NET 9)

| Component | Technology | License |
|---|---|---|
| API framework | ASP.NET Core Web API .NET 9 | MIT |
| ORM | Entity Framework Core 9 | MIT |
| CQRS / Mediator | MediatR | MIT |
| Validation | FluentValidation | Apache 2.0 |
| Database | PostgreSQL 16 | PostgreSQL |
| File storage | MinIO (S3-compatible) | AGPL v3 |
| Authentication | JWT Bearer | MIT |
| Real-time | SignalR | MIT |
| MQTT client | MQTTnet | MIT |
| Excel | ClosedXML | MIT |
| PDF reports | QuestPDF | MIT |
| SPC / math | MathNet.Numerics | MIT |
| Email | MailKit | MIT |
| Container | Docker + Docker Compose | Apache 2.0 |

### Web Client (Next.js 16)

| Component | Technology |
|---|---|
| Framework | Next.js 16 (App Router) + TypeScript |
| UI primitives | @base-ui/react + shadcn/ui CLI |
| Styling | Tailwind CSS v4 |
| Server state | TanStack Query v5 |
| Client state | Zustand |
| Forms | React Hook Form + Zod |
| i18n | next-intl (Vietnamese + English) |

### Desktop Client (WPF .NET 9)

| Component | Technology |
|---|---|
| Framework | WPF .NET 9 (Windows only) |
| UI components | MaterialDesignThemes |
| MVVM | CommunityToolkit.Mvvm |
| PDF viewer | Microsoft.Web.WebView2 |
| Virtual keyboard | Custom WPF (NumPad + QWERTY) |
| i18n | RESX + custom MarkupExtension |

---

## Project Structure

```
shopfloor-manager/
├── src/
│   ├── ShopfloorManager.API/               # Controllers, middleware, DI root
│   ├── ShopfloorManager.Application/       # MediatR handlers, DTOs, interfaces
│   ├── ShopfloorManager.Domain/            # Entities, enums
│   ├── ShopfloorManager.Infrastructure/    # EF Core, MinIO, MQTT, MailKit, ERP connectors
│   ├── ShopfloorManager.Shared/            # PagedResult<T>, AppConstants
│   └── ShopfloorManager.Desktop/           # WPF touchscreen MES
│       ├── Views/                          # XAML pages
│       ├── ViewModels/                     # CommunityToolkit.Mvvm
│       ├── Services/                       # IApiClient, SignalR, Keyboard
│       └── local.json                      # Per-machine config (gitignored)
│
├── clients/
│   └── web/                                # Next.js 16 Web App
│       ├── app/(main)/                     # Authenticated routes
│       ├── components/va/                  # VA design system (sidebar, topbar, badges…)
│       ├── components/erp/                 # ERP import dialog
│       ├── components/documents/           # Bulk upload dialog
│       ├── lib/api-client.ts               # Typed fetch + JWT
│       └── messages/                       # vi.json + en.json (next-intl)
│
├── docs/
│   ├── features/                           # Per-feature documentation (this folder)
│   └── screenshots/                        # UI screenshots
│
├── Project_Documents/                      # Business logic specs (Vietnamese)
├── docker-compose.yml                      # Production stack
└── docker-compose.dev.yml                  # Dev: PostgreSQL + MinIO + Mosquitto
```

---

## Domain Model

```
PartNumber  (part catalog)
  └── PartRev  (design revision: Rev A, B, C…)
        ├── TechDocument  (DRW, CAD — part-level)
        └── Routing
              └── RoutingRev  (R1, R2…)
                    └── PartOp  (OP 10, 20, 30…)
                          ├── TechDocument  (GCD, TLS, CAM, THD — op-level)
                          └── Dimension  (dimension to inspect)
                                └── MeasureValue  (measured result — new record per measurement)

Job  (production order)
  ├── PartRevId    ─── snapshot at order creation
  ├── RoutingRevId ─── snapshot at order creation
  └── Product  (serial 001, 002… RunQty)
        └── MeasureValue

ProductionSession  (Desktop MES — one per machine at a time)
  ├── ProductId · PartOpId · MachineCode
  ├── StartedAt · CompletedAt
  └── ClaimedBy (user)

ErpConnection  (ERP integration)
  ├── ErpType (Epicor / Mock / …)
  ├── BaseUrl · Company · Username · Password
  └── IsActive
```

**Key design decisions:**

| Decision | Rationale |
|---|---|
| `Job` stores `PartRevId + RoutingRevId` snapshot | Routing changes after job creation must not affect in-progress jobs |
| No OP copying into Job | OPs queried dynamically from `RoutingRevId`; job-specific OPs use `ForJobOnly = true` |
| `Dimension` values in `DECIMAL(14,4)` | Legacy stored tolerances as `VARCHAR` — silent precision loss |
| `MeasureValue` — new record per measurement | Full history preserved; upsert would lose rework traceability |
| All business logic in Application layer | Zero stored procedures; enables unit testing without a database |

---

## Roles & Permissions

| Role | Key permissions |
|---|---|
| `Administrator` | Everything + Desktop Settings page |
| `Manager` | View all, approve NCRs, manage users, ERP import |
| `Engineer` | Create/edit Parts, Routings, Dimensions, upload docs, ERP import |
| `QC Inspector` | Approve/reject TechDocs, enter FAI Final dims, close NCRs |
| `Leader` | Operator + force-finish another operator's session |
| `Operator` | Desktop MES: select Job/OP/Serial, enter measurements, file NCRs |
| `Planner` | View all production data, ERP import |

---

## Getting Started

**Prerequisites:** Docker Desktop, .NET 9 SDK, Node.js 20+

```bash
# 1. Clone
git clone https://github.com/longnvht/shopfloor-manager.git
cd shopfloor-manager

# 2. Start infrastructure (PostgreSQL + MinIO + Mosquitto)
docker compose -f docker-compose.dev.yml up -d

# 3. Run the API
cd src
dotnet run --project ShopfloorManager.API
# → API:      http://localhost:5066
# → Swagger:  http://localhost:5066/swagger

# 4. Run the Web App
cd clients/web
npm install && npm run dev
# → http://localhost:3000
```

**Default dev credentials:**

| Service | URL | Credentials |
|---|---|---|
| Web App | `http://localhost:3000` | `admin` / `Admin@123` |
| Swagger | `http://localhost:5066/swagger` | — |
| PostgreSQL | `localhost:5432` | `shopfloor` / `dev_password` / `shopfloor_dev` |
| MinIO Console | `http://localhost:9001` | `minioadmin` / `minioadmin123` |

**EF Core migrations** (after any entity change):
```bash
cd src
dotnet ef migrations add {MigrationName} \
  --project ShopfloorManager.Infrastructure \
  --startup-project ShopfloorManager.API

dotnet ef database update \
  --project ShopfloorManager.Infrastructure \
  --startup-project ShopfloorManager.API
```

**Desktop App** (Windows only):
```bash
cd src
dotnet run --project ShopfloorManager.Desktop
# Edit src/ShopfloorManager.Desktop/local.json first:
# { "ApiBaseUrl": "http://localhost:5066", "MachineCode": "MACHINE-01", "MachineName": "My CNC" }
```

---

## Production Deployment

```bash
cp .env.example .env
# Edit .env with production credentials
docker compose up -d
docker compose ps   # verify all 6 services are healthy
```

**Services:** `postgres` · `minio` · `mosquitto` · `api` · `web` · `nginx`

**Nginx routing:**
- `/` → Next.js Web App
- `/api/*` → ASP.NET Core API
- `/hub/*` → SignalR WebSocket

**Desktop App deployment:**
```bash
dotnet publish src/ShopfloorManager.Desktop \
  -c Release -r win-x64 --self-contained \
  -o publish/desktop
# Copy publish/desktop/ to each CNC machine PC
# Edit local.json per machine (ApiBaseUrl, MachineCode, MachineName)
```

> **Note:** `clients/web/Dockerfile` is not yet created — required before the `web` service in `docker-compose.yml` can run.

---

## Project Status

| Phase | Scope | Status |
|---|---|---|
| **Phase 0** | Foundation: Docker, PostgreSQL schema, .NET scaffold | ✅ Done |
| **Phase 1** | Auth & HR: JWT, 7 roles, users, departments, SignalR | ✅ Done |
| **Phase 2** | Production Core: Parts, Jobs, Routing, OPs, TechDocuments, MinIO | ✅ Done |
| **Phase 3** | Quality: Dimensions, FAI, NCR, SPC | ✅ Done |
| **Phase 4** | Desktop MES: WPF touchscreen, FAI at machine, ProductionSession, SignalR | ✅ Done |
| **Web UI** | VA design system, 14 routes, i18n (VI + EN) | ✅ Done |
| **Phase 5a** | Gage & Calibration API + Web UI | ✅ Done |
| **Phase 5b** | Bulk Excel Import (Jobs + Parts + Routing + OPs) | ✅ Done |
| **Phase 5c** | ERP Integration (Epicor OData v4 connector) | ✅ Done |
| **Phase 5d** | Planning (Gantt), MQTT pipeline, Dashboard KPIs | ⏳ Planned |
| **Phase 6** | Multi-factory, MySQL→PostgreSQL migration tool, Docker polish, docs site | ⏳ Planned |

---

## Legacy System Comparison

| Feature | Legacy (WinForms) | Shopfloor Manager |
|---|---|---|
| Data access | Direct MySQL + 429 stored procedures | REST API — zero stored procedures |
| Document storage | FTP server | MinIO (S3-compatible, pre-signed URLs) |
| Authentication | MD5 + MySQL | JWT (8h expiry, bcrypt passwords) |
| Real-time notifications | Teams webhook (polling) | SignalR (push) |
| Measure precision | `FLOAT` | `DECIMAL(14,4)` |
| Measure history | Upsert — last value only | New record every measurement |
| UI framework | WinForms + Guna2 / DevExpress | WPF + MaterialDesign (Desktop) / Next.js (Web) |
| Multi-language | Vietnamese only | Vietnamese + English |
| ERP integration | Manual CSV export | Direct OData v4 pull (Epicor) |
| Self-hosted | Single-machine MySQL | Docker Compose stack |

---

## License

[MIT License](LICENSE) — all dependencies are MIT or Apache 2.0.

---

*Built with [Claude Code](https://claude.ai/code)*
