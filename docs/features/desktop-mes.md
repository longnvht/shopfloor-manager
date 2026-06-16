# Desktop MES — WPF Touchscreen App

**Project:** `src/ShopfloorManager.Desktop`  
**Platform:** Windows 10/11 · .NET 9 · WPF  
**Roles:** Operator, Leader, Administrator (others: View Mode only)

---

## Overview

The Desktop MES is a separate WPF application installed on each CNC machine PC. It is designed for **10–15" touchscreens** — every interactive element is ≥ 56px tall. All data access goes through the shared REST API; there is **no direct database connection**.

---

## Login

![Desktop login screen](../screenshots/desktop-login.png)

Standard JWT login (`POST /api/v1/auth/login`). The token is held **in-memory only** — never written to disk.

After successful login, the app calls `GET /api/v1/machines/{code}/active-session` to determine the startup mode:

| Machine state at login | Mode |
|---|---|
| No active session | **Operation Mode** |
| Session owned by *this* user | **Operation Mode** (session resumed; WorkContext restored) |
| Session owned by *another* user — role is `Leader` or `Administrator` | **Operation Mode** (ForceFinish button visible) |
| Session owned by *another* user — role is `Operator` or other | **View Mode** (forced; cannot be overridden) |

---

## Dashboard

![Desktop MES dashboard](../screenshots/desktop-dashboard.png)

The main screen — no sidebar. All navigation happens from here.

**Four zones:**

| Zone | Content |
|---|---|
| Top bar | Logo · MODE toggle chip · clock · logout |
| Machine card | Availability %, quality %, uptime today, parts completed |
| Operator card | Check-in time, work time, idle time, parts produced |
| Work Info card | Current Job / OP / Serial + context-aware action button |
| Shortcuts grid | Role- and context-aware icon buttons |

**Work Info card — 5 exclusive states** (only one button visible at a time):

| State | Button | Condition |
|---|---|---|
| No job selected | **+ Chọn Job** | No work context |
| Job + OP selected, no serial | **Tiếp tục →** | `HasWork && !HasProduct` |
| Serial selected, not started | **▶ Bắt đầu** | `HasProduct && !IsWip` |
| Session in progress | **■ Kết thúc** + timer | Session running |
| Another user's session | **Kết thúc phiên [Name]** | Leader/Admin force-finish |

**Shortcuts — visibility:**

| Shortcut | Visible when |
|---|---|
| Chọn Job | Always |
| Chọn OP | `HasJob` |
| Chọn sản phẩm | `HasOp` |
| Xem bản vẽ / G-code / Hướng dẫn gá / Route Card | `HasOp` |
| **Bảng đo** | Operation Mode + `HasProduct` + session started |
| Tạo NCR | `HasProduct` + Operation Mode (QC/Engineer/Admin) |
| FAI Final | Operation Mode + `HasProduct` + all dims measured + ≥1 Fail |
| Cài đặt | Role = `Administrator` |

Shortcuts "Chọn Job/OP/Sản phẩm" are **disabled (opacity 40%)** while a session is in progress, preventing context switch mid-production. View Mode re-enables them.

---

## Job Selection

![Desktop job list](../screenshots/desktop-joblist.png)

Card grid of active production orders. Each card shows job number, part number, revision, quantity, and ship date. **Red date badge** = overdue. Supports text search and drag-to-scroll.

---

## Operation List

After selecting a job, the operator picks which operation they are performing. Each OP card shows: number, type, setup / run times, and document availability badges (`GCD`, `DRW`, `RTC`, `FXT`).

---

## Product Serial Selection

After selecting an operation, the operator picks a serial:

| Color | State | Meaning |
|---|---|---|
| Gray | Available | Ready to select |
| Amber | In Progress (this machine) | Current session |
| Orange | Locked | Being worked on at another machine |
| Green | Complete | FAI finished |

**Session constraints (server-enforced):**
- Only one active session per **product** across all machines
- Only one active session per **machine** at a time

---

## FAI Measurement Entry

The core feature. After pressing **Start**, the operator measures each dimension:

```
┌─────────────────────────────────┬─────────────────────────┐
│  DIMENSION CARDS (55%)          │  INPUT PANEL (45%)      │
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

**Rules:**
- **Numeric** — NumPad → Confirm → Pass/Fail calculated automatically
- **Text** (`IsTextType`) — PASS/FAIL buttons, auto-save immediately
- **Final** (`IsFinal`) — visible but disabled for Operators; QC Inspector only
- **Already measured** (`IsInputLocked`) — card shows previous value; input disabled with amber notice

Auto-advance: after confirming, focus moves to the next unmeasured dimension.

On **Fail** → NCR dialog opens immediately (reason + department required before continuing).

---

## FAI Final Mode

When all dimensions have been measured and at least one has failed, a **"FAI Final"** shortcut appears. This mode:
- Loads only **failed** dimensions
- Title bar turns dark red `#B71C1C`
- Each value saved with `IsFinal = true`

Used by QC Inspector to perform a final re-check after rework.

---

## Operation Mode vs View Mode

| | Operation Mode | View Mode |
|---|---|---|
| WorkContext slot | `CurrentJob/Op/Product` | `ViewJob/Op/Product` (independent) |
| Navigation writes to | CurrentJob… | ViewJob… |
| Session operations | Start / Stop available | Disabled |
| FAI shortcut | Visible | Hidden |
| Toggle chip | Brown "VIEW" | Orange "VIEW MODE" |

**Forced View Mode** — Operator role logs in while another user has an active session → View Mode only; chip is dimmed (cannot toggle). Leader/Administrator can force-finish the other session and take over.

**Dual context** — the two slots are completely independent. Toggling mode does not clear either slot. View context persists until logout.

---

## Virtual Keyboard

Two floating keyboard windows (both `WS_EX_NOACTIVATE` — focused TextBox never loses focus):

- **NumPad** — numeric input, `±`, `.`, backspace. Floats near the active field.
- **QWERTY** — full keyboard with CapsLock toggle and numeric panel. Used for search, NCR descriptions.

Both support **drag** via a handle strip (uses `ReleaseCapture` + `WM_NCLBUTTONDOWN` — avoids `DragMove()` which would steal focus).

---

## Document Viewer

Accessible from operation shortcuts (Drawing, G-code, Route Card, Fixture):

- **G-code** — `RichTextBox` with syntax highlighting. Token colors: N=gray, G=blue, M=purple, X/Y/Z/I/J/K=orange, F/S=green, T/H/D=teal, comments=gray. Renders up to 5,000 lines.
- **PDF** — native rendering via Microsoft WebView2 (Edge PDF engine; zoom/pan built-in). Documents fetched from MinIO via pre-signed URL.

Only `Status = Approved` documents are shown to operators.

---

## Real-time Notifications (SignalR)

After login, the app connects to `/hub/shopfloor` and joins groups by role:

| Event | Handler |
|---|---|
| `ncr-created` | Red banner on Dashboard for 8 seconds (QC Inspector) |
| `job-status-changed` | Refresh job list |
| `measure-submitted` | Update FAI progress |
| `document-approved` | Refresh document availability |

---

## Per-Machine Configuration (`local.json`)

Each CNC machine PC has a `local.json` (gitignored) that overrides `appsettings.json`:

```json
{
  "ApiBaseUrl": "http://192.168.1.100:5066",
  "MachineCode": "CNC-LINE1-03",
  "MachineName": "MAZAK QTN-350 #3",
  "Language": "vi"
}
```

Editable from the **Settings page** (Administrator role) without touching the file directly. API URL changes require app restart; other fields apply immediately.

---

## Building & Deploying

```bash
# Self-contained publish (Windows only)
dotnet publish src/ShopfloorManager.Desktop \
  -c Release -r win-x64 --self-contained \
  -o publish/desktop

# Copy publish/desktop/ to each CNC machine PC
# Edit local.json on each machine
```

No installer required — copy the folder and run `ShopfloorManager.Desktop.exe`.
