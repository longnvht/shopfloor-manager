# Dimension Sheet

**Route:** `/dimsheet`  
**Roles:** All authenticated users (inline edit: Engineer, Manager; approve: Lead Engineer, Manager, Administrator)

---

## Overview

The Dimension Sheet gives a bird's-eye view of **all inspection dimensions** for a part across its entire routing — in one paginated table, without navigating into individual operations.

---

## Layout

### Left panel — Part list (280px)
- Search box (sticky header) — type to filter by part number
- Each item shows: `partNumber` (mono bold), `description`, routing code + OP count, creation date
- Click a part to load its dimension sheet on the right
- Pagination when total parts > 20

### Right panel — Dimension sheet

**Header:**
- Part number (large monospace) + **"Drawing Rev {code}"** badge (selected `PartRev`)
- Part description

**KPI strip (5 cards):**
| KPI | Value |
|---|---|
| Total Dimensions | All dimensions in selected `RoutingRev` |
| Unique Balloons | Distinct `BalloonNumber` values |
| FAI Final | Dimensions with `IsFinal = true` |
| OPs with Dims | `{ops with ≥1 dim} / {total ops}` |
| Pending | Count of `status=Pending` (only shown when > 0) |

**Pending approval banner** (approver roles only, when pendingCount > 0):
- "N kích thước chờ duyệt" + "✓ Duyệt tất cả" + "✕ Từ chối tất cả" buttons

**Unified filter bar** (single row):
| Filter | Notes |
|---|---|
| Drawing Rev | VACombobox — select `PartRev` (type-to-search) |
| Routing Rev | VACombobox — cascades from Drawing Rev |
| OP | VACombobox — filter by operation number |
| Category | Chip selector: `LIN` · `ANG` · `THD` · `GEO` · `SFC` (with dim count) |
| FAI Final only | Checkbox |
| Pending only | Checkbox — shown only when pendingCount > 0 |
| Search balloon | Text input |
| Counter | `{filtered} / {total}` |
| Import Bulk | Button — approver roles only |

---

## Dimension Table

| Column | Notes |
|---|---|
| **Balloon** | Circle badge — red border if `IsCritical = true` |
| **OP** | Primary color badge with OP number |
| **Category** | Color-coded: LIN=brown, ANG=orange, THD=tan, GEO=dark brown, SFC=medium brown |
| **Nominal** | `DECIMAL(14,4)` |
| **Tol +** | Prefixed `+` |
| **Tol −** | Prefixed `−` |
| **Max** | Green text — `Nominal + TolerancePlus` |
| **Min** | Red text — `Nominal − ToleranceMinus` |
| **Unit** | `mm` (default) |
| **Final** | `●` if `IsFinal = true`, otherwise `—` |
| **Status** | `VABadge`: Pending (warn/yellow) · Approved (ok/green) · Rejected (err/red) |
| **Action** | ✎ inline edit; ✓✕ approve/reject (approver roles + pending rows only) |

Row background: yellow for Pending, red for Rejected.

`IsTextType = true` dimensions span 5 columns (Nominal→Min) showing `NominalText` instead of numeric fields — cannot be inline-edited.

---

## Dimension Status & Approval Workflow

```
Import bulk → status = Pending
    ↓ Lead Engineer / Manager / Administrator
Approved  ← or →  Rejected (with note)
    ↑                   ↓
    └── Engineer corrects Excel, re-imports
```

- **Only Approved dimensions** appear in Desktop MES FAI measurement
- **Batch approve/reject**: banner with "Duyệt tất cả" / "Từ chối tất cả" for the current RoutingRev
- **Per-row**: ✓✕ buttons visible for approver + pending row only
- **Reject with note**: `window.prompt()` for reason → stored in `review_note`
- Roles: `Lead Engineer`, `Manager`, `Administrator` (NOT QC Inspector)

---

## Inline Editing

Click **✎** on any numeric dimension row to edit in place:
1. Three inputs appear: **Nominal**, **Tol +**, **Tol −**
2. **Max** and **Min** preview update in real time
3. Click **✓** to save (`PUT /api/v1/dimensions/{id}`) or **✕** to cancel

Text-type dimensions (`IsTextType = true`) cannot be edited inline.

---

## Bulk Import

Button **"⬆⬆ Import Bulk"** (approver roles only) opens `ImportDimsheetDialog`:
1. Download Excel template (11 columns: BalloonNumber, Code, Description, Nominal, TolPlus, TolMinus, Unit, Category, OpNumber, IsFinal, IsCritical)
2. Fill and upload `.xlsx`
3. Results shown: created / skipped / errors per row

Imported dimensions start as `Pending`. Use the approval banner or per-row buttons to approve.

---

## Empty States

| State | Shown when |
|---|---|
| No routing active | Part exists but has no active `RoutingRev` |
| No dimensions | `RoutingRev` has OPs but none have dimensions yet |
| No match | Filter combination returns 0 results |

---

## Footnote

> A balloon number may appear in multiple operations (e.g. checked at both OP 10 and OP 30). The **Final** marker is the authoritative QC gate — only QC Inspectors may enter `IsFinal` measurements.

---

## API Endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/v1/routing-revs/{id}/dimensions` | All dimensions for a `RoutingRev` (with `status/reviewedBy/reviewedAt/reviewNote`) |
| `PUT` | `/api/v1/dimensions/{id}` | Update Nominal + tolerances |
| `PUT` | `/api/v1/dimensions/{id}/review` | Approve or reject a single dimension |
| `POST` | `/api/v1/routing-revs/{id}/dimensions/review-batch` | Batch approve/reject all pending dimensions |
| `POST` | `/api/v1/routing-revs/{id}/dimensions/import-bulk` | Bulk import from Excel (multipart/form-data) |
| `GET` | `/api/v1/routing-revs/{id}/dimensions/import-bulk/template` | Download Excel template |
