# ERP Integration

**Route:** `/jobs` → "🔗 Import from ERP" button  
**Roles:** Administrator, Manager, Engineer, Planner (connection management: Administrator only)

---

## Overview

Shopfloor Manager can pull production orders directly from an ERP system via **OData v4**, creating Jobs, Parts, Revisions, Routings, and Operations in one step — without any Excel export/import cycle.

The connector architecture is extensible: new ERP types can be added by implementing `IErpConnector`.

---

## Supported Connectors

| Type | Protocol | Auth | Notes |
|---|---|---|---|
| `Mock` | In-memory (10 hardcoded rows) | None | For testing without a real ERP |
| `Epicor` | OData v4 (`Erp.BO.JobEntrySvc`) | HTTP Basic | Epicor Kinetic / E10 |
| `Odoo` | *(planned)* | | |
| `SAP` | *(planned)* | | |

---

## Connection Setup

Connections are configured by an Administrator via the API (Swagger UI or `curl`):

```bash
POST /api/v1/erp/connections
{
  "name": "Epicor Production",
  "erpType": "Epicor",
  "baseUrl": "https://erp.factory.local",
  "company": "MAIN",
  "username": "api_user",
  "password": "secret",
  "isActive": true
}
```

Credentials are stored in the `erp_connections` table. Acceptable for factory intranet deployments (not public internet).

---

## 3-Step Import Dialog

### Step 1 — Filter

![ERP import step 1](../screenshots/web-jobs-erp-import.png)

- Select a saved ERP connection from the dropdown
- **"Test Connection"** button — verifies the link in real time (green / red badge)
- Optional filters: **Date From**, **Date To**, **PO Number**
- Mock connections show a warning: "this is a test connector — data is not from a real ERP"

### Step 2 — Preview

![ERP import step 2](../screenshots/web-jobs-erp-preview.png)

- **3 KPI cards**: distinct Jobs / Parts / Operations in the fetched result
- **Warnings banner** (orange): unknown OpType codes, Mock connector notice
- **Preview table** (read-only, 9 columns):
  - Job Number, Part Number, Revision, Qty, Ship By, OP, Op Type, Setup (min), Prod (min)

### Step 3 — Result

- **7 counter cards** (highlighted in accent color when > 0):
  - Parts Created, Part Revisions Created, Operations Created, Operations Updated, Jobs Created, Jobs Updated, Products Created
- Row-level error list for skipped rows

---

## Data Mapping

### Epicor OData → Shopfloor Manager

| Epicor field | Mapped to | Notes |
|---|---|---|
| `JobNum` | `JobNumber` | |
| `PartNum` | `PartNumber` | |
| `RevisionNum` | `Revision` | |
| `ProdQty` | `RunQty` | |
| `ReqDueDate` | `ShipBy` | |
| `PONum` | `PONumber` | |
| `AssemblySeq` | `POLine` | |
| `OprSeq` | `OpNumber` | |
| `OpCode` | `OpTypeCode` | Matched case-insensitive to `OpTypes.Code` |
| `OpDesc` | `OpDescription` | |
| `EstSetHours × 60` | `SetupTime` | Epicor stores hours; Shopfloor uses minutes |
| `ProdStandard × 60` | `ProdTime` | Same conversion |

### Import Rules (reuses Bulk Excel Import logic)

- Same `JobNumber` in multiple rows → all belong to that job (grouped + transactional)
- Part + PartRev resolved or created from `PartNumber` + `Revision`
- Active `RoutingRev` is upserted (never creates a new RoutingRev on re-import)
- `RunQty` increase → new Products created; decrease → warning, no deletion
- Unrecognized `OpTypeCode` → warning + `OpTypeId = null`; import continues

---

## Architecture

```
ErpController.Import()
        │
        ▼
GetErpPreviewQuery   ── IErpConnectorFactory.Create(erpType, …)
        │                       │
        │              ┌────────┴─────────┐
        │              │                  │
        │        EpicorConnector    MockErpConnector
        │        (OData v4 HTTP)    (in-memory)
        │
        ▼  (rows mapped to ImportJobBatchRow)
ImportJobBatchCommand
        │
        ▼
  Same handler as Excel Bulk Import
  (no duplicated logic)
```

The ERP import endpoint re-uses `ImportJobBatchCommandHandler` — the same handler that processes Excel uploads. This keeps all import logic in one place.

---

## API Endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/v1/erp/connections` | List all ERP connections |
| `POST` | `/api/v1/erp/connections` | Create connection (Admin only) |
| `PUT` | `/api/v1/erp/connections/{id}` | Update connection (Admin only) |
| `POST` | `/api/v1/erp/connections/{id}/test` | Test connectivity |
| `POST` | `/api/v1/erp/preview` | Fetch + preview rows from ERP |
| `POST` | `/api/v1/erp/import` | Import rows from ERP into database |

---

## Adding a New Connector

1. Implement `IErpConnector` in `src/ShopfloorManager.Infrastructure/Erp/`:

```csharp
public interface IErpConnector
{
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
    Task<ErpPreviewResult> FetchPreviewAsync(ErpImportFilter filter, CancellationToken ct = default);
}
```

2. Register the new type in `ErpConnectorFactory.Create()`:

```csharp
"ODOO" => new OdooConnector(baseUrl, company, username, password),
```

No other changes needed — the controller, handler, and UI are connector-agnostic.
