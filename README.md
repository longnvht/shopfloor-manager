# Shopfloor Manager

**Open-source factory management system for CNC machining shops** — self-hosted, no vendor lock-in, built with .NET 9 + Next.js + PostgreSQL.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-blueviolet)](https://dotnet.microsoft.com/)
[![Next.js 16](https://img.shields.io/badge/Next.js-16-black)](https://nextjs.org/)
[![PostgreSQL 16](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)

Developed as a full replacement for a legacy WinForms system (ManageData + Vinam-MES) used in precision CNC machining shops of 50–200 people. All business logic lives in C#; the database stores data only (no stored procedures, no triggers).

> **UI language:** Vietnamese — the target market is Vietnamese CNC machining factories.

---

## Table of Contents

- [Introduction](#introduction)
- [Features](#features)
  - [Production Management](#production-management)
  - [Technical Documents](#technical-documents)
  - [Quality Inspection — FAI](#quality-inspection--fai)
  - [NCR — Non-Conformance Reports](#ncr--non-conformance-reports)
  - [Gage Management](#gage-management)
  - [Production Planning](#production-planning)
  - [CNC Machine Monitoring](#cnc-machine-monitoring)
  - [Dashboard](#dashboard)
  - [Desktop MES — WPF Touchscreen App](#desktop-mes--wpf-touchscreen-app)
- [System Architecture](#system-architecture)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Domain Model](#domain-model)
- [Core Business Rules](#core-business-rules)
- [Roles & Permissions](#roles--permissions)
- [Getting Started](#getting-started)
- [Production Deployment](#production-deployment)
- [Project Status](#project-status)
- [License](#license)

---

## Introduction

Shopfloor Manager replaces two legacy WinForms applications that were built on top of MySQL stored procedures and DevExpress controls:

| Legacy system | Replacement | Where |
|---|---|---|
| ManageData (WinForms + DevExpress) | Web App (Next.js) | Engineering office / management |
| Vinam-MES (WinForms touchscreen) | Desktop App (WPF) | At each CNC machine on the shop floor |
| MySQL stored procedures (429 total) | ASP.NET Core Application layer | Business logic 100% in C# |
| FTP server | MinIO (S3-compatible) | Technical document storage |

**Design philosophy:**

- **Self-hosted first** — one `docker compose up` runs the entire stack on an internal Linux server.
- **Solo-developer friendly** — no over-engineering; the simplest solution that works.
- **Business logic 100% in the API** — the database stores data only. No stored procedures, no triggers.
- **All MIT/Apache 2.0 dependencies** — no commercial library lock-in.
- **Audit trail everywhere** — every record carries `created_by`, `updated_by`, `created_at`, `updated_at`.

---

## Features

### Production Management

Manage production orders (Jobs), part definitions (Parts/Revisions), manufacturing routings, and individual product serials.

![Web App — Jobs & Serials](docs/screenshots/web-jobs.png)

**Jobs (`/jobs`)** — master-detail view: job list on the left, job details (part, revision, routing, progress bar, serial grid) on the right. Each job tracks progress per serial (0/N completed) and flags overdue orders.

![Web App — Parts & Routing](docs/screenshots/web-parts.png)

**Parts (`/parts` — "Chi tiết kỹ thuật")** — part catalog with revision history, routing revisions, and the full operation sequence (OP10 → OP20 → ... → Final Inspection). Each operation shows its type (CNC Machining, Turning, Heat Treatment, etc.), setup time, run time, and linked technical documents.

**Key concepts:**

- A **Part** has multiple **Revisions** (Rev A, B, C…). A revision change creates a new record — old data is never overwritten.
- A **Routing** defines the process steps for a given PartRev. Routing changes create a new **RoutingRev**, copying all operations forward.
- A **Job** stores a snapshot of both `PartRevId` and `RoutingRevId` at creation time — subsequent routing changes do not affect jobs already in production.
- **Product** serials (001, 002, … N) are auto-generated when a Job is created. Each serial is the unit of measurement tracking.

---

### Technical Documents

Upload, version, and approve technical documents per part or per operation.

Eight document types are supported, each with its own MinIO storage path:

| Type | Description | Level |
|---|---|---|
| `DRW` | 2D Drawing | Part/Revision |
| `CAD` | 3D CAD file | Part/Revision |
| `GCD` | G-code program | Operation |
| `TLS` | Tool list | Operation |
| `CAM` | CAM file | Operation |
| `THD` | Thread inspection sheet | Operation |
| `RTC` | Route card (job-specific) | Job Operation |
| `FXT` | Fixture drawing (job-specific) | Job Operation |

**Approval workflow (3 upload rules):**

1. `BLOCK` if status = `Approved` → file is locked; even the uploader cannot overwrite.
2. `BLOCK` if status = `Pending` and `CreatedBy ≠ current user` → another person is awaiting approval.
3. `ALLOW` if status = `Rejected` → renames old file to `Rejected_{filename}` on MinIO, uploads new file, resets status to `Pending`.

Documents are uploaded directly to MinIO via pre-signed URL; the API manages metadata only.

---

### Quality Inspection — FAI

Define dimension sheets per operation and record measurement results per product serial.

Each **Dimension** belongs to a specific **PartOp** and carries:
- `BalloonNumber` — the balloon label on the drawing (e.g. `"Ø1"`, `"L2"`, `"Ra3"`)
- `Nominal`, `TolerancePlus`, `ToleranceMinus` — stored as `DECIMAL(14,4)`, never as text
- `IsTextType` — for thread callouts and geometric symbols (Pass/Fail instead of numeric)
- `DimensionCategory` — `LIN`, `ANG`, `THD`, `GEO`, or `SFC` (determines which gage types are valid)
- `IsFinal` — marks dimensions checked after all rework; only QC Inspectors may enter these values

**SPC** — `MathNet.Numerics` calculates Cpk/Cp from measurement history per dimension.

Measurement entry happens both in the Web App (office QC) and the Desktop MES (at the machine).

---

### NCR — Non-Conformance Reports

![Web App — NCR](docs/screenshots/web-ncrs.png)

Track and resolve non-conforming parts from initial report through disposition.

- Auto-generated number format: `NCR-{YY}-{NNNN}` (e.g. `NCR-26-0005`)
- Linked to: Job, serial number, PartOp
- Categorized by reason (Tool wear, Setup error, Drawing error, Material defect, …) and department (PROD, QC, ENG)
- **Disposition workflow**: `Pending` → `Approve` (accept as-is) / `Rework` (send back) / `Reject` (scrap)
- Full audit log: every status change is recorded with timestamp and user

NCRs can be created from the Desktop MES directly at the machine after a failed FAI measurement, or from the Web App office interface.

---

### Gage Management

![Web App — Gages](docs/screenshots/web-gages.png)

Manage the metrology equipment inventory: calipers, micrometers, bore gauges, CMM probes, thread gauges, and more.

- **Borrow / Return** — log who borrowed which gage and when; overdue borrows are highlighted.
- **Calibration due dates** — automatic warnings when a gage's calibration expires.
- **Gage types** aligned to dimension categories: `CAL`, `MIC`, `BOR`, `DPG`, `HEG` (Linear); `PLG`, `PDG` (Thread); `CMM`, `IND`, `PPM` (Geometric); `SRM` (Surface).

---

### Production Planning

![Web App — Planning](docs/screenshots/web-planning.png)

Weekly Gantt chart view per machine: visualize job scheduling, identify conflicts, and track shift loading. (Phase 5 — API integration in progress.)

---

### CNC Machine Monitoring

![Web App — CNC Live](docs/screenshots/web-cnc.png)

Real-time machine status collected via **MQTT** (Mosquitto broker). The API subscribes to `factory/cnc/{machineCode}/status` topics and pushes updates to the Web App via **SignalR**.

Supports FANUC FOCAS and MTConnect adapters (publishes to the same MQTT topic schema).

---

### Dashboard

![Web App — Dashboard](docs/screenshots/web-dashboard.png)

Role-aware KPI overview combining:
- **Machine status panel** — live CNC availability via MQTT/SignalR
- **Production summary** — jobs running, serials completed today, average progress
- **Quality panel** — FAI pass rate (30-day), open NCRs needing action
- **Gage alerts** — calibration expiring / overdue borrows
- **Documents** — pending approval queue

Time filters: Day / Week / Month / Quarter.

---

### Desktop MES — WPF Touchscreen App

A separate WPF application (`ShopfloorManager.Desktop`) installed on each CNC machine PC. Designed for 10–15" touchscreens; all interactive elements are ≥56px tall. Connects to the shared API over HTTP — **no direct database access**.

#### Hardware & configuration

Each machine PC has a `local.json` that overrides global settings:

```json
{
  "ApiBaseUrl": "http://192.168.1.100:5066",
  "MachineCode": "CNC-LINE1-03",
  "MachineName": "MAZAK QTN-350 #3"
}
```

`MachineCode` is used to tag every measurement record (traceability — know which machine produced which result) and to subscribe to the correct MQTT topic for real-time CNC data.

---

#### Login

![Desktop MES — Login](docs/screenshots/desktop-login.png)

Standard JWT login (`POST /api/v1/auth/login`). Token is held **in-memory only** — never written to disk. On success, the app checks for an active session on this machine before navigating to the Dashboard.

**Login → mode determination:**

| Machine state at login | Result |
|---|---|
| No active session | → **Operation Mode** (operator is free to work) |
| Active session belonging to *this* user | → **Operation Mode** (session is resumed; WorkContext restored) |
| Active session belonging to *another* user — role is `Leader` or `Administrator` | → **Operation Mode** (ForceFinish button visible) |
| Active session belonging to *another* user — role is `Operator` / other | → **View Mode** (forced read-only; cannot be overridden) |

---

#### Dashboard

![Desktop MES — Dashboard](docs/screenshots/desktop-dashboard.png)

The hub of the app — no sidebar navigation. Everything accessible from one screen.

**Four zones:**

| Zone | Content |
|---|---|
| **Top bar** | Logo · MODE toggle chip · clock · logout |
| **Machine card** (top-left) | Availability %, quality %, uptime today, parts completed today |
| **Operator card** (top-right) | Check-in time, work time, idle time, parts produced |
| **Work Info card** (center) | Job / OP / Serial + action button (context-aware) |
| **Shortcuts grid** (bottom) | Icon buttons — role and context aware |

**Work Info card — 5 exclusive states** (only one action button visible at any time):

```
State            Button shown          Condition
─────────────────────────────────────────────────────────────────
No job           [+ Chọn Job]          !HasWork && !CanForceFinish
Has job/OP,      [Tiếp tục →]          CanNavigate && !CanForceFinish
no serial
Serial chosen,   [▶ Bắt đầu]           HasProduct && !IsWip
not started
Session active   [■ Kết thúc] + timer  CanStop && !CanForceFinish
Another user's   [Kết thúc phiên X]    CanForceFinish (Leader/Admin)
session
```

**Shortcuts — visibility rules:**

| Shortcut | Visible when |
|---|---|
| Chọn Job | Always |
| Chọn OP | HasJob |
| Chọn sản phẩm | HasOp |
| Xem bản vẽ / G-code / Hướng dẫn gá / CW | HasOp |
| **Bảng đo** | Operation Mode + HasProduct + `session.StartedAt != null` |
| Tạo NCR | HasProduct + Operation Mode (QC/Engineer/Admin) |
| Cài đặt | Role = Administrator |

*Shortcuts "Chọn Job/OP/Sản phẩm" are disabled (opacity 40%) while a session is in progress — prevents context switch mid-production. View Mode re-enables them (operates on separate ViewContext).*

---

#### Job Selection

![Desktop MES — Job List](docs/screenshots/desktop-joblist.png)

Card grid (5 columns) showing active production orders. Each card shows job number, part number, revision, quantity, and ship date. **Red date badge** = overdue. Supports text search and drag-to-scroll.

---

#### Operation List

After selecting a job, the operator chooses which operation they are performing:

```
┌─────────────────────────────────────────────────────┐
│  OP 10 — Inspection                                 │
│  Setup: 0.5h  |  Run: 0.1h                          │
│  [📐 Bản vẽ]  [📋 Route Card]                      │
└─────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────┐
│  OP 20 — Turning                                    │
│  Setup: 2.0h  |  Run: 4.0h                          │
│  [💾 G-code]  [🔩 Fixture]                          │
└─────────────────────────────────────────────────────┘
```

Each OP card shows: operation number, type, setup/run times, and document availability badges. The list combines template operations (from `RoutingRev`) and any job-specific operations (`ForJobOnly = true`).

---

#### Product Serial Selection

After selecting an operation, the operator selects which product serial they are machining:

```
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐
│   001    │  │   002    │  │   003    │  │   004    │
│   ○      │  │   ⚙      │  │   ✓      │  │   ✓      │
│ READY    │  │ IN PROG  │  │ COMPLETE │  │ COMPLETE │
└──────────┘  └──────────┘  └──────────┘  └──────────┘
```

Four card states (color-coded):

| Color | State | Meaning |
|---|---|---|
| Gray | Ready | Available to select |
| Amber | In Progress | Currently being machined (this machine) |
| Orange | Locked | Being machined on another machine |
| Green | Complete | FAI finished |

**Session constraints** (enforced server-side):
- One active session per **product** — a serial being machined elsewhere cannot be claimed.
- One active session per **machine** — starting a second session on the same machine is blocked.

---

#### FAI Measurement Entry

The core feature. After pressing **Start**, the operator measures each dimension:

```
┌─────────────────────────────────┬─────────────────────────┐
│  DIMENSION CARDS (55% width)    │  INPUT PANEL (45%)      │
│                                 │                         │
│  ┌────┐ ┌────┐ ┌────┐ ┌────┐   │  Balloon:  Ø5           │
│  │ 1  │ │ 2  │ │ 3  │ │ 4  │   │  Nominal:  25.0000      │
│  │PASS│ │FAIL│ │    │ │    │   │  Min:      24.9800      │
│  │25.0│ │25.3│ │    │ │    │   │  Max:      25.0200      │
│  └────┘ └────┘ └────┘ └────┘   │                         │
│  Green   Red   Gray  Gray       │  ┌──────────────────┐   │
│                                 │  │  [7] [8] [9]     │   │
│  ┌────┐ ┌────┐                  │  │  [4] [5] [6]     │   │
│  │ 5  │ │Ra1 │                  │  │  [1] [2] [3]     │   │
│  │    │ │TEXT│                  │  │  [±] [0] [.]     │   │
│  └────┘ └────┘                  │  └──────────────────┘   │
│  Gray  (PASS/FAIL)              │  [ ✓ Confirm ]          │
└─────────────────────────────────┴─────────────────────────┘
```

**Measurement rules:**
- **Numeric dimensions** — NumPad input → Confirm → `Pass` if `min ≤ value ≤ max`, else `Fail`.
- **Text dimensions** (`IsTextType = true`) — PASS/FAIL buttons, auto-save immediately; no confirm step.
- **Final dimensions** (`IsFinal = true`) — visible but disabled for Operators; only QC Inspectors can enter.
- **Already measured** (`IsInputLocked`) — card shows previous value in gray; input panel fully disabled with amber notice. Each measurement creates a new record — history is preserved.

On **Fail**: an NCR dialog opens immediately, requiring the operator to select a reason category before proceeding.

Auto-advance: after confirming a measurement, focus moves to the next unmeasured dimension automatically.

---

#### Virtual Keyboard

Two keyboard types, both implemented as floating `WS_EX_NOACTIVATE` windows (focused TextBox never loses focus):

- **NumPad** — numeric input with `±`, `.`, backspace. Floats near the input field.
- **QWERTY** — full keyboard with CapsLock toggle and numeric panel. Used for search fields and NCR descriptions.

Both keyboards support **drag** via a handle strip at the top (`ReleaseCapture` + `WM_NCLBUTTONDOWN` trick — avoids `DragMove()` which would steal focus).

---

#### Document Viewer

Accessible from operation shortcuts (Drawing, G-code, Route Card, Fixture):

- **G-code** — `RichTextBox` with syntax highlighting: N=gray, G=blue, M=purple, X/Y/Z=orange, F/S=green, T/H/D=teal, comments=gray. Renders up to 5,000 lines.
- **PDF** (drawings, route cards) — native PDF rendering via **Microsoft WebView2** (Edge PDF engine; zoom/pan built-in). Documents are downloaded from MinIO via pre-signed URL.

Only `Status = Approved` documents are shown to operators.

---

#### Operation Mode vs View Mode

Two modes allow operators to browse production records without disrupting an active session.

```
Operation Mode                    View Mode
────────────────────────────       ────────────────────────────
WorkContext: CurrentJob/Op/Prod    WorkContext: ViewJob/Op/Prod
                                   (independent slot)
Navigation writes CurrentJob...    Navigation writes ViewJob...
Session Start/Stop available       No session operations
Shortcuts act on current context   Shortcuts act on view context
FAI sheet accessible               FAI shortcut hidden
```

**MODE toggle chip** (top bar, always visible):
- Brown background + "VIEW" text → Operation Mode
- Orange `#FF8F00` background + "VIEW MODE" text → View Mode
- Chip is dimmed (opacity 40%) and non-clickable when **forced** View Mode (another user's session + Operator role)

**Forced View Mode** — when an Operator logs in while another user has an active session on that machine, they are placed in View Mode automatically and cannot toggle out. A `Leader` or `Administrator` can use the ForceFinish button to end the other session.

**Dual context** — the two context slots are completely independent. Toggling mode does not clear or copy between them. View context persists until logout.

---

#### NCR Creation at Machine

When a measurement fails, an NCR dialog appears:

1. Select reason from dropdown (15 seeded categories: Tool wear, Setup error, Drawing error, …)
2. Select department responsible (PROD / QC / ENG)
3. Enter optional description
4. Submit → `POST /api/v1/ncrs` → NCR number generated (`NCR-{YY}-{NNNN}`)
5. QC Inspector receives a real-time notification via **SignalR**

---

#### Settings Page (Administrator only)

Accessible from the shortcuts grid when logged in as `Administrator`:

- Edit `ApiBaseUrl`, `MachineCode`, `MachineName`
- **Test Connection** button — verifies the new URL with a fresh `HttpClient` (not the shared singleton)
- Save → writes `local.json` at `AppContext.BaseDirectory`
- API URL change requires app restart (the singleton `HttpClient` was created with the old URL); other fields apply immediately

---

#### Real-time Updates (SignalR)

After login, the app connects to `/hub/shopfloor` and joins groups by role:

| Event | Received by | Action |
|---|---|---|
| `ncr-created` | QC Inspector | Toast notification + badge count |
| `job-status-changed` | All | Refresh job list |
| `measure-submitted` | Engineer, QC | Update FAI progress counter |
| `document-approved` | All | Refresh document availability |

---

#### Comparison: Legacy Vinam-MES vs Shopfloor Manager Desktop

| Feature | Vinam-MES (WinForms) | Shopfloor Manager (WPF) |
|---|---|---|
| Data access | Direct MySQL queries | REST API only |
| Documents | FTP download | MinIO pre-signed URL |
| Authentication | MD5 + MySQL | JWT (8h expiry) |
| Notifications | Teams webhook (polling) | SignalR (push) |
| Measure history | Upsert — last value only | New record every measurement |
| Measure precision | `FLOAT` | `DECIMAL(14,4)` |
| Offline mode | No | No (planned Phase 4b) |
| UI framework | WinForms + Guna2 | WPF + MaterialDesignThemes |
| Virtual keyboard | Custom WinForms | Custom WPF (no-focus window) |
| PDF viewer | PdfiumViewer | WebView2 (Edge) |

---

## System Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│  Web App  (Next.js 16 — clients/web)                             │
│  Office UI: Engineering · Management · QC · Planning             │
│  Routes: /jobs  /parts  /ncrs  /gages  /planning  /cnc  /fai    │
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
(data only)       (tech docs,         (real-time push
                   drawings,           to Web + Desktop)
                   G-code, PDF)              │
                                 ┌───────────▼──────────────────────┐
                                 │  Desktop App (WPF .NET 9)        │
                                 │  src/ShopfloorManager.Desktop    │
                                 │  Touchscreen MES at CNC machine  │
                                 │  FAI · NCR · G-code · PDF viewer │
                                 └───────────────┬──────────────────┘
                                                 │ MQTT
                                 ┌───────────────▼──────────────────┐
                                 │  Mosquitto MQTT Broker            │
                                 │  CNC data: FANUC / MTConnect      │
                                 └──────────────────────────────────┘
```

**Request flow (Web/Desktop → API):**

```
HTTP Request
  → Controller (thin — delegates to IMediator.Send)
  → MediatR Handler (all business logic lives here)
  → Repository / Service interfaces (Application layer)
  → EF Core / MinIO / SignalR / MQTT
```

---

## Tech Stack

### Backend (.NET 9)

| Component | Technology | License |
|---|---|---|
| API framework | ASP.NET Core Web API .NET 9 | MIT |
| ORM | Entity Framework Core 9 | MIT |
| CQRS/Mediator | MediatR | MIT |
| Validation | FluentValidation | Apache 2.0 |
| Database | PostgreSQL 16 | PostgreSQL |
| File storage | MinIO (S3-compatible) | AGPL v3 |
| Authentication | JWT Bearer | MIT |
| Real-time | SignalR | MIT |
| MQTT client | MQTTnet | MIT |
| Excel export | ClosedXML | MIT |
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
| Charts | Apache ECharts (Phase 5) |
| Gantt | Frappe Gantt (Phase 5) |

### Desktop Client (WPF .NET 9)

| Component | Technology |
|---|---|
| Framework | WPF .NET 9 (Windows only) |
| UI components | MaterialDesignThemes |
| MVVM | CommunityToolkit.Mvvm |
| PDF viewer | Microsoft.Web.WebView2 |
| Virtual keyboard | Custom WPF (NumPad + QWERTY) |
| HTTP client | System.Net.Http.HttpClient + JWT |

---

## Project Structure

```
shopfloor-manager/
├── src/                                    # .NET 9 Solution
│   ├── ShopfloorManager.API/               # Controllers, middleware, DI composition root
│   │   └── Controllers/                    # Thin controllers — IMediator.Send only
│   ├── ShopfloorManager.Application/       # MediatR handlers, FluentValidation, DTOs
│   │   ├── Commands/                       # Write operations
│   │   └── Queries/                        # Read operations
│   ├── ShopfloorManager.Domain/            # Entities, enums — no framework dependencies
│   ├── ShopfloorManager.Infrastructure/    # EF Core DbContext, MinIO, MQTT, MailKit
│   │   └── Migrations/                     # EF Core migrations
│   ├── ShopfloorManager.Shared/            # PagedResult<T>, AppConstants, enums
│   └── ShopfloorManager.Desktop/           # WPF touchscreen MES app
│       ├── Pages/                          # Dashboard, JobList, OperationList, FAI, …
│       ├── ViewModels/                     # CommunityToolkit.Mvvm ObservableObject
│       ├── Services/                       # IApiClient, IAuthService, IKeyboardService
│       ├── Controls/                       # Virtual keyboard windows
│       └── local.json                      # Per-machine config (gitignored)
│
├── clients/
│   └── web/                                # Next.js 16 Web App
│       ├── app/
│       │   ├── (auth)/login/               # Login page
│       │   └── (main)/                     # Authenticated shell (sidebar + topbar)
│       │       ├── dashboard/
│       │       ├── jobs/[id]/
│       │       ├── parts/[id]/
│       │       ├── ncrs/
│       │       ├── gages/
│       │       ├── planning/
│       │       ├── cnc/
│       │       └── master/
│       ├── components/
│       │   └── va/                         # VA design system components
│       │       ├── sidebar.tsx             # VASidebar — 224px brown, nav groups
│       │       ├── topbar.tsx              # VATopbar — breadcrumb + serif title
│       │       ├── badge.tsx               # VABadge (ok/warn/err/neutral/primary)
│       │       ├── kpi.tsx                 # VAKpi card with trend indicator
│       │       └── btn.tsx                 # VABtn (primary/accent/ghost)
│       └── lib/
│           └── api-client.ts               # Typed fetch wrapper with JWT
│
├── docs/
│   └── screenshots/                        # UI screenshots for this README
│
├── docker-compose.yml                      # Production stack
├── docker-compose.dev.yml                  # Dev: PostgreSQL + MinIO + Mosquitto only
└── Project_Documents/                      # Business logic specs (Vietnamese)
    ├── 01_auth.md
    ├── 03_job_management.md
    ├── 06_dimensions_fai.md
    └── …
```

---

## Domain Model

The production core revolves around the following entity hierarchy:

```
PartNumber  (part catalog)
  └── PartRev  (design revision: Rev A, B, C…)
        ├── TechDocument  (DRW, CAD — part-level files)
        └── Routing  (manufacturing process definition)
              └── RoutingRev  (process revision: R1, R2…)
                    └── PartOp  (operation: OP10, OP20, OP30…)
                          ├── TechDocument  (GCD, TLS, CAM, THD — op-level files)
                          └── Dimension  (dimension to inspect)
                                └── MeasureValue  (actual measured result)

Job  (production order)
  ├── PartRevId    ─── snapshot of PartRev at order creation
  ├── RoutingRevId ─── snapshot of RoutingRev at order creation
  └── Product  (serial: 001, 002, … RunQty)
        └── MeasureValue  (measured value per dimension per serial)

ProductionSession  (machine session at CNC)
  ├── ProductId
  ├── PartOpId
  ├── MachineCode
  ├── StartedAt
  └── CompletedAt
```

### Key entity design decisions

| Decision | Rationale |
|---|---|
| `Job` stores `PartRevId` + `RoutingRevId` snapshot | Routing changes after job creation must not affect in-progress jobs |
| No OP copying into Job | Operations are queried dynamically from `RoutingRevId`; job-specific OPs use `ForJobOnly = true` |
| `Dimension` values in `DECIMAL(14,4)` | Legacy system stored tolerances as `VARCHAR`, causing silent precision loss |
| `MeasureValue` — new record per measurement | Preserves full measurement history; upsert would lose rework traceability |
| Soft delete via `deleted_at TIMESTAMPTZ` | Major entities (Parts, Jobs, Users) are never hard-deleted |
| `snake_case` table/column names | PostgreSQL convention; consistent with EF Core `UseSnakeCaseNamingConvention()` |
| All business logic in Application layer | Zero stored procedures; enables unit testing without a database |

---

## Core Business Rules

### Parts & Routing

- `(part_number, revision)` must be **unique** — a revision change creates a new PartRev record.
- Creating a new **RoutingRev**: deactivates the current rev and copies all `PartOps` forward. Engineers edit on the new rev only.
- Only one `RoutingRev` per `Routing` can have `IsActive = true` at a time.

### Jobs

- `job_number` is a business key (e.g. `J2026-001`) — set by the engineer, not auto-generated.
- Creating a job auto-generates `Product` serials from `001` to `RunQty`.
- A job's routing = `PartOps WHERE RoutingRevId = job.RoutingRevId` UNION `PartOps WHERE JobId = job.Id AND ForJobOnly = true`.

### Dimensions & FAI

- `BalloonNumber` must be unique within a `PartOp`.
- `Pass` when `Nominal − ToleranceMinus ≤ MeasuredValue ≤ Nominal + TolerancePlus`.
- `IsTextType = true` → operator selects Pass/Fail manually; no numeric bounds.
- `IsFinal = true` → only `QC Inspector` role may enter the value; Operators see it but it is disabled.

### Technical Documents

1. `Approved` → **locked**. No overwrite, even by the original uploader.
2. `Pending` + `CreatedBy ≠ currentUser` → **blocked**. Must wait for the current reviewer to act.
3. `Rejected` → **allowed**. Old file renamed `Rejected_{filename}` on MinIO; new upload resets to `Pending`.

### NCR

- Format: `NCR-{YY}-{NNNN}` — year-sequential, never recycled.
- Disposition: `Pending → Approved` (use as-is), `Pending → Rework` (return for correction), `Pending → Rejected` (scrap).
- NCR closes automatically when all rework is verified and a final FAI passes.

### Production Sessions (Desktop MES)

- **One active session per machine** — a new session is blocked if the machine already has one with `started_at IS NOT NULL`.
- **One active session per product** — a product being worked on at another machine is blocked.
- `Leader` and `Administrator` can force-complete another operator's session.
- Other roles logging in while a session is active → **View Mode** (read-only; no input allowed).

---

## Roles & Permissions

| Role | Description | Key permissions |
|---|---|---|
| `Administrator` | System admin | All permissions + Settings page on Desktop |
| `Manager` | Factory manager | View all data, approve NCRs, manage users |
| `Engineer` | Process engineer | Create/edit Parts, Routings, Dimensions, upload documents |
| `QC Inspector` | Quality inspector | Approve/reject TechDocs, enter FAI finals, close NCRs |
| `Operator` | CNC machine operator | Desktop MES: select Job/OP/Serial, enter measurements, file NCRs |
| `Leader` | Team leader | Operator permissions + force-finish other sessions |
| `Planner` | Production planner | View all production data, manage planning/Gantt |

---

## Getting Started

**Prerequisites:** Docker Desktop, .NET 9 SDK, Node.js 20+

### Development setup (`docker-compose.dev.yml`)

In development, only the **infrastructure** runs in Docker. The API and Web App run directly on your machine for fast hot-reload.

```
┌─────────────────────────────────────────────────────────┐
│  docker-compose.dev.yml  (infrastructure only)          │
│                                                         │
│  postgres:5432   minio:9000/9001   mosquitto:1883/9002  │
└─────────────────────────────────────────────────────────┘
       ↑                  ↑                  ↑
  dotnet run          npm run dev       (MQTT test)
  :5066               :3000
```

```bash
# 1. Clone the repository
git clone https://github.com/longnvht/shopfloor-manager.git
cd shopfloor-manager

# 2. Start infrastructure (PostgreSQL + MinIO + Mosquitto)
docker compose -f docker-compose.dev.yml up -d
```

Dev services started by Docker:

| Container | Image | Ports | Purpose |
|---|---|---|---|
| `postgres` | `postgres:16-alpine` | `5432` | Database — no auth required in dev |
| `minio` | `minio/minio:latest` | `9000` (API), `9001` (Console) | File storage (drawings, G-code, PDFs) |
| `mosquitto` | `eclipse-mosquitto:2` | `1883` (MQTT), `9002` (WebSocket) | MQTT broker for CNC data |

```bash
# 3. Run the API
cd src
dotnet run --project ShopfloorManager.API
# → API:        http://localhost:5066
# → Swagger UI: http://localhost:5066/swagger

# 4. Run the Web App
cd clients/web
npm install
npm run dev
# → http://localhost:3000
```

**Default dev credentials** (hardcoded — no `.env` needed):

| Service | URL | Credentials |
|---|---|---|
| Web App | `http://localhost:3000` | `admin` / `Admin@123` |
| API Swagger | `http://localhost:5066/swagger` | — |
| PostgreSQL | `localhost:5432` | `shopfloor` / `dev_password` / db `shopfloor_dev` |
| MinIO Console | `http://localhost:9001` | `minioadmin` / `minioadmin123` |
| MQTT | `localhost:1883` | no auth |

**EF Core migrations** (run after any entity change):

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
dotnet build ShopfloorManager.Desktop

# Edit src/ShopfloorManager.Desktop/local.json:
# {
#   "ApiBaseUrl": "http://localhost:5066",
#   "MachineCode": "MACHINE-01",
#   "MachineName": "My CNC Lathe"
# }

dotnet run --project ShopfloorManager.Desktop
```

---

## Production Deployment

### Full stack (`docker-compose.yml`)

The production compose file runs **6 services** with health checks and `restart: unless-stopped`:

```
                    ┌─────────────────┐
Internet ──── 80/443 │     nginx       │ (reverse proxy)
                    └────┬───────┬────┘
                         │       │
               ┌─────────▼─┐ ┌───▼──────┐
               │    web    │ │   api    │
               │ Next.js   │ │ .NET 9   │
               └───────────┘ └──┬───┬──┘
                                │   │
                     ┌──────────▼┐  └────────────┐
                     │ postgres  │               │
                     │    16     │    ┌──────────▼┐
                     └──────────┘    │   minio   │
                                     └──────────┘
                    ┌──────────────────────────────┐
                    │         mosquitto            │ :1883 ← CNC machines
                    └──────────────────────────────┘
```

| Service | Image | Ports | Depends on |
|---|---|---|---|
| `postgres` | `postgres:16-alpine` | internal only | — |
| `minio` | `minio/minio:latest` | internal only | — |
| `mosquitto` | `eclipse-mosquitto:2` | `1883` (exposed to LAN) | — |
| `api` | built from `src/ShopfloorManager.API/Dockerfile` | internal only | postgres ✓, minio ✓ |
| `web` | built from `clients/web/Dockerfile` | internal only | api ✓ |
| `nginx` | `nginx:alpine` | `80`, `443` | web, api |

### Step-by-step

```bash
# 1. Copy and configure the environment file
cp .env.example .env
```

Edit `.env` with production values:

```env
# PostgreSQL
DB_USER=shopfloor
DB_PASSWORD=<strong-password>
DB_NAME=shopfloor

# JWT — generate with: openssl rand -base64 48
JWT_SECRET=<64-char-random-string>
JWT_EXPIRY_HOURS=8

# MinIO
MINIO_USER=<minio-admin-user>
MINIO_PASSWORD=<strong-password>
MINIO_BUCKET=shopfloor-storage

# MQTT
MQTT_TOPIC_CNC=factory/cnc/#

# Email (optional — for password reset)
SMTP_HOST=smtp.yourcompany.com
SMTP_PORT=587
SMTP_USER=noreply@yourcompany.com
SMTP_PASSWORD=<smtp-password>
SMTP_FROM=noreply@yourcompany.com

# Teams webhook (optional — for NCR alerts)
TEAMS_WEBHOOK_URL=https://outlook.office.com/webhook/...

# App
NEXT_PUBLIC_APP_NAME=Shopfloor Manager
```

```bash
# 2. Start the full stack
docker compose up -d

# 3. Check all services are healthy
docker compose ps
```

Expected output:

```
NAME                    STATUS
shopfloor-postgres      running (healthy)
shopfloor-minio         running (healthy)
shopfloor-mosquitto     running
shopfloor-api           running (healthy)
shopfloor-web           running
shopfloor-nginx         running
```

### Nginx routing

| Host path | Proxied to | Notes |
|---|---|---|
| `/` | `web:3000` | Next.js Web App |
| `/api/*` | `api:5000` | REST API |
| `/hub/*` | `api:5000` | SignalR WebSocket |
| `minio.*` | `minio:9001` | MinIO Console (optional) |

### Data volumes (persistent)

| Volume | Contains |
|---|---|
| `postgres_data` | All application data |
| `minio_data` | Drawings, G-code, PDFs, CAD files |
| `mosquitto_data` | MQTT retained messages |
| `mosquitto_logs` | Mosquitto broker logs |

### Desktop App deployment

The Desktop MES app is a Windows-only WPF application deployed manually to each CNC machine PC:

```bash
# Build a self-contained publish (on Windows)
dotnet publish src/ShopfloorManager.Desktop \
  -c Release -r win-x64 --self-contained \
  -o publish/desktop

# Copy publish/desktop/ to each CNC machine PC
# Then edit local.json on each machine:
# {
#   "ApiBaseUrl": "http://shopfloor.factory.local/api",
#   "MachineCode": "CNC-LINE1-01",
#   "MachineName": "MAZAK QTN-350 #1"
# }
```

> **Note:** `clients/web/Dockerfile` is not yet created — the `web` service in `docker-compose.yml` requires it before production deployment of the web app.

---

## Project Status

| Phase | Scope | Status |
|---|---|---|
| **Phase 0** | Foundation: Docker, PostgreSQL schema, .NET Clean Architecture scaffold | ✅ Done |
| **Phase 1** | Auth & HR: JWT, 7 roles, users, departments, SignalR hub | ✅ Done |
| **Phase 2** | Production Core: Parts, Jobs, Routing, Operations, TechDocuments, MinIO | ✅ Done |
| **Phase 3** | Quality: Dimensions, FAI measurements, NCR, SPC (Cpk/Cp) | ✅ Done |
| **Phase 4** | Desktop MES: WPF touchscreen app, FAI at machine, ProductionSession, SignalR | ✅ Done |
| **Web UI** | VA design system + 18 routes, real API on /jobs /parts /ncrs | ✅ Done |
| **Phase 5** | Gage & Calibration API, Planning (Gantt), MQTT pipeline, Dashboard KPIs | ⏳ Planned |
| **Phase 6** | Multi-factory, MySQL→PostgreSQL migration tool, Docker polish, docs site | ⏳ Planned |

---

## License

[MIT License](LICENSE) — all dependencies are MIT or Apache 2.0. No commercial library dependencies.

---

*Built with [Claude Code](https://claude.ai/code)*
