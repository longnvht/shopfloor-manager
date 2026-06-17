# FAI — First Article Inspection & Measurement

**Routes:** `/fai` · `/jobs/{id}/fai`  
**Roles:** All authenticated users (enter values: Operator, Leader, QC Inspector, Engineer)

---

## Overview

FAI (First Article Inspection) is the measurement tracking module. Every dimension defined on a `PartOp` must be measured for every product serial. Results are recorded as `MeasureValue` records — one per measurement event (full history preserved).

**3 independent measurement stages:**

| Stage | Enum | Who | When |
|---|---|---|---|
| **Inprocess FAI** | `0` | Operator (own product) / Leader (any) | After production session ends |
| **QC Inline** | `1` | QC Inspector | Random sampling during production |
| **QC Final** | `2` | QC Inspector only | When product is fully machined, before shipping |

Each stage is isolated: `(DimensionId, ProductId, MeasureStage)` can only be entered once per stage.

---

## `/fai` — Top-Level View

The standalone FAI view lets QC engineers browse any job/operation without navigating through the Jobs module.

### Workflow
1. Select a **Job** (search by job number or part number)
2. Select an **Operation** from the job's routing
3. The **measurement matrix** loads automatically

---

## Measurement Matrix

The FAI matrix is a table where:
- **Rows** = Product serials (001, 002, … RunQty)
- **Columns** = Dimensions (sorted by `BalloonSort`, then `BalloonNumber`)

Each cell shows the latest measured value for that `(serial × dimension)` combination:
- **Green** = Pass (`MinValue ≤ MeasuredValue ≤ MaxValue`)
- **Red** = Fail
- **Gray / empty** = Not yet measured

Column headers show:
- Balloon label (e.g. `Ø5`, `L2`)
- Nominal ± tolerance
- Category badge (`LIN`, `ANG`, `THD`, `GEO`, `SFC`)
- `★` marker if `IsFinal = true`

**Only `Approved` dimensions appear in the matrix** — dimensions with `status=Pending` or `status=Rejected` are excluded until approved by Lead Engineer/Manager/Administrator.

---

## Entering Measurements (Web)

Click any cell in the matrix to enter or update a measurement:
- **Numeric dimensions**: enter a value → Pass/Fail is calculated automatically against `MinValue`/`MaxValue`
- **Text dimensions** (`IsTextType = true`): select PASS or FAIL directly
- **Final dimensions** (`IsFinal = true`): only QC Inspector role can enter these

Each submission creates a **new `MeasureValue` record** with the appropriate `MeasureStage`. The matrix always shows the most recent measurement for the selected stage.

---

## Measurement Entry at the Machine (Desktop MES)

The Desktop WPF app provides a touch-optimized FAI entry screen at each CNC machine. See [Desktop MES documentation](desktop-mes.md) for details on the measurement workflow, NumPad input, and auto-advance behavior.

**Current Desktop state**: uses `MeasureStage=InprocessFAI` (default). QC Inline and QC Final stages on Desktop are planned (Nhóm 4 — Phase 4b).

---

## Pass / Fail Rules

```
Numeric:  Pass  if  (Nominal − ToleranceMinus) ≤ Value ≤ (Nominal + TolerancePlus)
          Fail  otherwise

Text:     Pass / Fail set explicitly by operator (no numeric bounds)

Final:    Same rules but only QC Inspector can enter; Operators see the cell grayed out
```

---

## QC Final Progress

`GET /api/v1/products/{productId}/qcfinal-progress` returns:

```json
{
  "totalDim": 45,
  "completeDim": 12,
  "passDim": 10,
  "failDim": 2
}
```

- `totalDim` = total `IsFinal` dimensions across the job's RoutingRev
- `completeDim` = how many have a `MeasureStage=QCFinal` entry
- Used by Web UI to show QC Final readiness per product

---

## SPC — Statistical Process Control

`MathNet.Numerics` calculates process capability indices from measurement history per dimension:

| Index | Formula |
|---|---|
| **Cp** | `(USL − LSL) / (6σ)` |
| **Cpk** | `min((USL − x̄) / (3σ), (x̄ − LSL) / (3σ))` |

Where `USL = MaxValue`, `LSL = MinValue`, `x̄ = mean`, `σ = sample standard deviation`.

Results are available via `GET /api/v1/fai/spc?dimensionId=&jobId=`.

---

## API Endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/v1/fai` | FAI sheet (`FaiSheetDto`) for a job+operation |
| `POST` | `/api/v1/fai/measure` | Submit a measurement value (includes `measureStage: 0/1/2`) |
| `GET` | `/api/v1/fai/spc` | Cpk/Cp for a dimension |
| `GET` | `/api/v1/products/{id}/qcfinal-progress` | QC Final progress for a product |
| `GET` | `/api/v1/routing-revs/{id}/dimensions` | All dimensions for a routing rev (used by Dim Sheet) |

---

## Planned — Phase C (Web UI)

**FAI stat strip** on `/fai` and `/jobs/{id}/fai`:
- KPI strip above the matrix: Inspector (last person to measure), Pass/Fail/Pending counts per stage, Pass rate%
- Stage filter selector (InprocessFAI / QCInline / QCFinal / All)
- 100% client-side aggregation from `FaiSheetDto` — no new API needed

## Planned — Phase J (Web UI)

**"Balloon Detail" panel** — click a cell in `FaiMatrix`:
- Shows: Nominal/Tolerance/Category, measure history per stage (table), distribution chart (Apache ECharts)
- "Open NCR for this cell" button when cell is Fail
- **Requires new API**: `GET /api/v1/dimensions/{id}/measure-history?productId=&stage=`

## Planned — Nhóm 4 (Desktop MES — Phase 4b)

- FAI role-split: Operator → `InprocessFAI` only (own product); QC Inspector → `QCInline`
- Leader override: can measure on behalf of any Operator
- FAI Final utility (hidden from Operator/Leader): loads all dims, checks `isFullyMachined`, uses `MeasureStage=QCFinal`
- Gage selection: choose gage before entering value, filtered by dimension category
- QC Inline Rate config in `/master`
