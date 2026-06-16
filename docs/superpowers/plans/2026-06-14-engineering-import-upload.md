# Engineering Group — Excel Template Download + Bulk Upload Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add (1) downloadable Excel templates for the existing Operations/Dimensions import dialogs, and (2) a multi-file "Bulk Upload" feature on `/documents` that parses filenames using the naming convention `{PartNumber}-{PartRevCode}-{RoutingRevCode}-{OPNumber}-{FileTypeCode}[-{i}_{n}].ext` (and the Part-level / ForJobOnly variants), resolves each file against the database, flags duplicates/invalid/incomplete-segment files, and uploads all "Ready" files to MinIO via the existing pre-signed-URL flow.

**Architecture:** Backend adds two read-only template endpoints (`ExcelTemplateBuilder` + `OperationsController`) and one resolve endpoint (`ResolveBulkUploadQuery` + `TechDocumentsController`). Frontend adds a filename-parsing library (`lib/bulk-upload-parser.ts`), a shared `lib/doc-format.ts` (extracted from `documents/page.tsx`), template-download buttons in the two existing import dialogs, and a new `BulkUploadDialog` component wired into `/documents`.

**Tech Stack:** ASP.NET Core 9 / MediatR / FluentResults / ClosedXML (backend, no new packages); Next.js 16 / React 19 / TypeScript / next-intl / `@base-ui/react` (frontend, no new packages).

---

### Task 1: Backend — Excel template download endpoints

**Files:**
- Create: `src/ShopfloorManager.API/Common/ExcelTemplateBuilder.cs`
- Modify: `src/ShopfloorManager.API/Controllers/OperationsController.cs`

- [ ] **Step 1: Create `ExcelTemplateBuilder.cs`**

```csharp
using ClosedXML.Excel;

namespace ShopfloorManager.API.Common;

public static class ExcelTemplateBuilder
{
    public static byte[] BuildOpsTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Operations");

        string[] headers = ["OpNumber", "OpType", "Description", "SetupTime", "ProdTime"];
        for (var i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

        ws.Cell(2, 1).Value = "10";
        ws.Cell(2, 2).Value = "CNC";
        ws.Cell(2, 3).Value = "Phay mặt đầu";
        ws.Cell(2, 4).Value = 30;
        ws.Cell(2, 5).Value = 5;

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] BuildDimensionsTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Dimensions");

        string[] headers = ["BalloonNumber", "Code", "Description", "Nominal", "TolPlus", "TolMinus", "Unit", "Category"];
        for (var i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

        ws.Cell(2, 1).Value = "Ø1";
        ws.Cell(2, 2).Value = "D1";
        ws.Cell(2, 3).Value = "Đường kính ngoài";
        ws.Cell(2, 4).Value = 25.4;
        ws.Cell(2, 5).Value = 0.05;
        ws.Cell(2, 6).Value = 0.05;
        ws.Cell(2, 7).Value = "mm";
        ws.Cell(2, 8).Value = "LIN";

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }
}
```

- [ ] **Step 2: Add two `GET` endpoints to `OperationsController.cs`**

Find this code (end of the `ImportDimensions` action, followed by the class-closing brace and the `CreateOpRequest` record):

```csharp
        var result = await mediator.Send(new ImportDimensionsCommand(opId, importRows, UserId));
        return result.IsSuccess
            ? Ok(ApiResponse<ImportResultDto>.Ok(result.Value))
            : BadRequest(ApiResponse<ImportResultDto>.Fail(result.Errors));
    }
}

public record CreateOpRequest(
    int? RoutingRevId, int? JobId, string OpNumber, int? OpTypeId,
    string? Description, string? Note, decimal? SetupTime, decimal? ProdTime);
```

Replace with:

```csharp
        var result = await mediator.Send(new ImportDimensionsCommand(opId, importRows, UserId));
        return result.IsSuccess
            ? Ok(ApiResponse<ImportResultDto>.Ok(result.Value))
            : BadRequest(ApiResponse<ImportResultDto>.Fail(result.Errors));
    }

    /// <summary>Tải file Excel mẫu cho Import Operations.</summary>
    [HttpGet("import/template")]
    public IActionResult GetOpsImportTemplate()
    {
        var bytes = ExcelTemplateBuilder.BuildOpsTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "import-ops-template.xlsx");
    }

    /// <summary>Tải file Excel mẫu cho Import Dimensions.</summary>
    [HttpGet("dimensions/import/template")]
    public IActionResult GetDimensionsImportTemplate()
    {
        var bytes = ExcelTemplateBuilder.BuildDimensionsTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "import-dimensions-template.xlsx");
    }
}

public record CreateOpRequest(
    int? RoutingRevId, int? JobId, string OpNumber, int? OpTypeId,
    string? Description, string? Note, decimal? SetupTime, decimal? ProdTime);
```

`using ShopfloorManager.API.Common;` is already present at the top of `OperationsController.cs` — no new `using` needed. Routes `GET /api/v1/operations/import/template` and `GET /api/v1/operations/dimensions/import/template` do not conflict with the existing `POST /api/v1/operations/{opId:int}/dimensions/import`.

- [ ] **Step 3: Build**

Run: `dotnet build src/ShopfloorManager.sln`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add src/ShopfloorManager.API/Common/ExcelTemplateBuilder.cs src/ShopfloorManager.API/Controllers/OperationsController.cs
git commit -m "feat(api): add Excel template download endpoints for OP/Dimension import"
```

---

### Task 2: Backend — bulk-upload resolve-batch endpoint

**Files:**
- Create: `src/ShopfloorManager.Application/Production/ResolveBulkUploadQuery.cs`
- Modify: `src/ShopfloorManager.API/Controllers/TechDocumentsController.cs`

- [ ] **Step 1: Create `ResolveBulkUploadQuery.cs`**

```csharp
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Application.Production;

public record ResolveBatchItem(
    string FileName,
    string FileTypeCode,
    string? PartNumber,
    string? PartRevCode,
    string? RoutingRevCode,
    string? OpNumber,
    string? JobNumber,
    int? SegmentIndex,
    int? SegmentTotal,
    long? FileSizeBytes);

public record ResolveBatchResultDto(
    string FileName,
    string Status,
    string? Reason,
    int? FileTypeId,
    int? PartRevId,
    int? PartOpId,
    int? JobId,
    string? ResolvedPartNumber,
    string? ResolvedRevCode,
    string? ResolvedRoutingRevCode,
    string? ResolvedOpNumber,
    string? ResolvedJobNumber,
    List<string>? ExistingSegments);

public record ResolveBulkUploadQuery(List<ResolveBatchItem> Items, int RequesterId)
    : IRequest<Result<List<ResolveBatchResultDto>>>;

public class ResolveBulkUploadQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<ResolveBulkUploadQuery, Result<List<ResolveBatchResultDto>>>
{
    public async Task<Result<List<ResolveBatchResultDto>>> Handle(ResolveBulkUploadQuery req, CancellationToken ct)
    {
        var fileTypes = await db.FileTypes.Where(f => f.IsActive).ToListAsync(ct);
        var results = new List<ResolveBatchResultDto>();

        foreach (var item in req.Items)
        {
            var fileType = fileTypes.FirstOrDefault(f => f.Code == item.FileTypeCode);
            if (fileType == null)
            {
                results.Add(Invalid(item, "unrecognizedType"));
                continue;
            }

            ResolveBatchResultDto result = fileType switch
            {
                { IsJobNumber: true, IsOpNumber: true } => await ResolveJobOp(item, fileType, req.RequesterId, ct),
                { IsPartNumber: true, IsOpNumber: true } => await ResolveStandardOp(item, fileType, req.RequesterId, ct),
                _ => await ResolvePartLevel(item, fileType, req.RequesterId, ct),
            };

            results.Add(result);
        }

        return Result.Ok(results);
    }

    private async Task<ResolveBatchResultDto> ResolveJobOp(ResolveBatchItem item, FileType fileType, int requesterId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(item.JobNumber) || string.IsNullOrWhiteSpace(item.OpNumber))
            return Invalid(item, "missingInfo");

        var job = await db.Jobs.FirstOrDefaultAsync(j => j.JobNumber == item.JobNumber, ct);
        if (job == null) return Invalid(item, "missingInfo");

        var op = await db.PartOps.FirstOrDefaultAsync(
            o => o.JobId == job.Id && o.OpNumber == item.OpNumber && o.ForJobOnly, ct);
        if (op == null) return Invalid(item, "missingInfo");

        var existingSegments = await ExistingSegments(null, op.Id, fileType.Id, ct);

        return await BuildResult(item, fileType, null, op.Id, job.Id, null, null, null, op.OpNumber, job.JobNumber, existingSegments, requesterId, ct);
    }

    private async Task<ResolveBatchResultDto> ResolveStandardOp(ResolveBatchItem item, FileType fileType, int requesterId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(item.PartNumber) || string.IsNullOrWhiteSpace(item.PartRevCode)
            || string.IsNullOrWhiteSpace(item.RoutingRevCode) || string.IsNullOrWhiteSpace(item.OpNumber))
            return Invalid(item, "missingInfo");

        var part = await db.Parts.FirstOrDefaultAsync(p => p.PartNumber == item.PartNumber, ct);
        if (part == null) return Invalid(item, "missingInfo");

        var partRev = await db.PartRevs.FirstOrDefaultAsync(r => r.PartId == part.Id && r.RevCode == item.PartRevCode, ct);
        if (partRev == null) return Invalid(item, "missingInfo");

        var routingRev = await db.RoutingRevs
            .Include(rr => rr.Routing)
            .FirstOrDefaultAsync(rr => rr.Routing.PartRevId == partRev.Id && rr.RevCode == item.RoutingRevCode, ct);
        if (routingRev == null) return Invalid(item, "missingInfo");

        var op = await db.PartOps.FirstOrDefaultAsync(o => o.RoutingRevId == routingRev.Id && o.OpNumber == item.OpNumber, ct);
        if (op == null) return Invalid(item, "missingInfo");

        var existingSegments = await ExistingSegments(partRev.Id, op.Id, fileType.Id, ct);

        return await BuildResult(item, fileType, partRev.Id, op.Id, null, part.PartNumber, partRev.RevCode, routingRev.RevCode, op.OpNumber, null, existingSegments, requesterId, ct);
    }

    private async Task<ResolveBatchResultDto> ResolvePartLevel(ResolveBatchItem item, FileType fileType, int requesterId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(item.PartNumber) || string.IsNullOrWhiteSpace(item.PartRevCode))
            return Invalid(item, "missingInfo");

        var part = await db.Parts.FirstOrDefaultAsync(p => p.PartNumber == item.PartNumber, ct);
        if (part == null) return Invalid(item, "missingInfo");

        var partRev = await db.PartRevs.FirstOrDefaultAsync(r => r.PartId == part.Id && r.RevCode == item.PartRevCode, ct);
        if (partRev == null) return Invalid(item, "missingInfo");

        var existingSegments = await ExistingSegments(partRev.Id, null, fileType.Id, ct);

        return await BuildResult(item, fileType, partRev.Id, null, null, part.PartNumber, partRev.RevCode, null, null, null, existingSegments, requesterId, ct);
    }

    private async Task<List<string>> ExistingSegments(int? partRevId, int? partOpId, int fileTypeId, CancellationToken ct)
    {
        return await db.TechDocuments
            .Where(d => d.FileTypeId == fileTypeId
                && d.PartRevId == partRevId && d.PartOpId == partOpId
                && d.Status != FileStatus.Rejected
                && d.Segment != null)
            .Select(d => d.Segment!)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Áp dụng 2 rule đầu của "3 upload rules" (xem TechDocumentsController.RequestUpload) cho doc hiện có
    /// cùng (PartRevId, PartOpId, FileTypeId, Segment): Approved → Invalid; Pending bởi người khác → Invalid.
    /// Rejected / Pending-của-mình / không tồn tại → Ready (ghi đè khi upload, giống flow upload đơn lẻ).
    /// </summary>
    private async Task<ResolveBatchResultDto> BuildResult(
        ResolveBatchItem item, FileType fileType,
        int? partRevId, int? partOpId, int? jobId,
        string? resolvedPartNumber, string? resolvedRevCode, string? resolvedRoutingRevCode,
        string? resolvedOpNumber, string? resolvedJobNumber,
        List<string> existingSegments,
        int requesterId, CancellationToken ct)
    {
        if (item.SegmentTotal.HasValue && !fileType.IsSegment)
        {
            return new ResolveBatchResultDto(
                item.FileName, "Invalid", "unsupportedSegment", fileType.Id, partRevId, partOpId, jobId,
                resolvedPartNumber, resolvedRevCode, resolvedRoutingRevCode, resolvedOpNumber, resolvedJobNumber,
                existingSegments);
        }

        var segment = item.SegmentIndex.HasValue && item.SegmentTotal.HasValue
            ? $"{item.SegmentIndex}_{item.SegmentTotal}"
            : null;

        var existing = await db.TechDocuments.FirstOrDefaultAsync(d =>
            d.PartRevId == partRevId && d.PartOpId == partOpId && d.FileTypeId == fileType.Id
            && (segment == null || d.Segment == segment), ct);

        string status = "Ready";
        string? reason = null;
        if (existing is not null)
        {
            if (existing.Status == FileStatus.Approved)
            {
                status = "Invalid";
                reason = "alreadyApproved";
            }
            else if (existing.Status == FileStatus.Pending && existing.CreatedBy != requesterId)
            {
                status = "Invalid";
                reason = "pendingByOther";
            }
        }

        return new ResolveBatchResultDto(
            item.FileName, status, reason, fileType.Id, partRevId, partOpId, jobId,
            resolvedPartNumber, resolvedRevCode, resolvedRoutingRevCode, resolvedOpNumber, resolvedJobNumber,
            existingSegments);
    }

    private static ResolveBatchResultDto Invalid(ResolveBatchItem item, string reason) =>
        new(item.FileName, "Invalid", reason, null, null, null, null, null, null, null, null, null, null);
}
```

- [ ] **Step 2: Add `resolve-batch` endpoint to `TechDocumentsController.cs`**

Find this code (the top of the controller):

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/tech-documents")]
[Authorize]
public class TechDocumentsController(IShopfloorDbContext db, IMinioService minio) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
```

Replace with:

```csharp
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.API.Common;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/tech-documents")]
[Authorize]
public class TechDocumentsController(IShopfloorDbContext db, IMinioService minio, IMediator mediator) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
```

Now find the end of the `ToDto` method and the start of `TechDocDto` (the class-closing brace):

```csharp
            t.FileSizeBytes);
    }
}

public record TechDocDto(
    long Id, string FileTypeCode, string FileTypeName,
    int? PartRevId, int? PartOpId, int? JobId,
```

Replace with:

```csharp
            t.FileSizeBytes);
    }

    /// <summary>
    /// Nhận diện Part/Rev/RoutingRev/OP (hoặc Job/OP) + trạng thái cho từng file trong lô bulk-upload,
    /// dựa trên thông tin đã parse từ tên file (client-side).
    /// </summary>
    [HttpPost("resolve-batch")]
    [Authorize(Roles = "Administrator,Manager,Engineer")]
    public async Task<IActionResult> ResolveBatch([FromBody] List<ResolveBatchItem> items)
    {
        var result = await mediator.Send(new ResolveBulkUploadQuery(items, UserId));
        return result.IsSuccess
            ? Ok(ApiResponse<List<ResolveBatchResultDto>>.Ok(result.Value))
            : BadRequest(ApiResponse<List<ResolveBatchResultDto>>.Fail(result.Errors));
    }
}

public record TechDocDto(
    long Id, string FileTypeCode, string FileTypeName,
    int? PartRevId, int? PartOpId, int? JobId,
```

- [ ] **Step 3: Build**

Run: `dotnet build src/ShopfloorManager.sln`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add src/ShopfloorManager.Application/Production/ResolveBulkUploadQuery.cs src/ShopfloorManager.API/Controllers/TechDocumentsController.cs
git commit -m "feat(api): add resolve-batch endpoint for bulk upload filename matching"
```

---

### Task 3: Frontend — `lib/doc-format.ts` + `api-client.ts` additions

**Files:**
- Create: `clients/web/lib/doc-format.ts`
- Modify: `clients/web/lib/api-client.ts`

- [ ] **Step 1: Create `doc-format.ts`**

```typescript
export const FILE_TYPE_COLORS: Record<string, string> = {
  DRW: '#6D3B1A', GCD: '#E65100', RTC: '#5D4037',
  FXT: '#A0522D', TLS: '#795548', THD: '#F57C00', CAM: '#8D6E63', CAD: '#6D4C41',
}

export function formatBytes(bytes: number | null): string {
  if (bytes == null) return '—'
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

export function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  a.click()
  URL.revokeObjectURL(url)
}
```

- [ ] **Step 2: Add `requestBlob` helper to `api-client.ts`**

Find this code:

```typescript
// FormData uploads — không set Content-Type, browser tự gắn multipart boundary
async function requestMultipart<T>(path: string, formData: FormData): Promise<ApiResponse<T>> {
  const token = getToken()
  const res = await fetch(`${API_URL}${path}`, {
    method: 'POST',
    body: formData,
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  })
  if (!res.ok && res.status !== 400 && res.status !== 401 && res.status !== 404) {
    throw new Error(`HTTP ${res.status}`)
  }
  return res.json()
}
```

Replace with:

```typescript
// FormData uploads — không set Content-Type, browser tự gắn multipart boundary
async function requestMultipart<T>(path: string, formData: FormData): Promise<ApiResponse<T>> {
  const token = getToken()
  const res = await fetch(`${API_URL}${path}`, {
    method: 'POST',
    body: formData,
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  })
  if (!res.ok && res.status !== 400 && res.status !== 401 && res.status !== 404) {
    throw new Error(`HTTP ${res.status}`)
  }
  return res.json()
}

// Binary downloads (Excel templates) — trả về Blob, không parse JSON
async function requestBlob(path: string): Promise<Blob> {
  const token = getToken()
  const res = await fetch(`${API_URL}${path}`, {
    headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) },
  })
  if (!res.ok) throw new Error(`HTTP ${res.status}`)
  return res.blob()
}
```

- [ ] **Step 3: Add template-download methods to `operations` namespace**

Find this code:

```typescript
    importOps: (routingRevId: number, file: File) => {
      const formData = new FormData()
      formData.append('file', file)
      formData.append('routingRevId', String(routingRevId))
      return requestMultipart<ImportResultDto>('/api/v1/operations/import', formData)
    },
    importDimensions: (opId: number, file: File) => {
      const formData = new FormData()
      formData.append('file', file)
      return requestMultipart<ImportResultDto>(`/api/v1/operations/${opId}/dimensions/import`, formData)
    },
  },
```

Replace with:

```typescript
    importOps: (routingRevId: number, file: File) => {
      const formData = new FormData()
      formData.append('file', file)
      formData.append('routingRevId', String(routingRevId))
      return requestMultipart<ImportResultDto>('/api/v1/operations/import', formData)
    },
    importDimensions: (opId: number, file: File) => {
      const formData = new FormData()
      formData.append('file', file)
      return requestMultipart<ImportResultDto>(`/api/v1/operations/${opId}/dimensions/import`, formData)
    },
    importOpsTemplate: () => requestBlob('/api/v1/operations/import/template'),
    importDimsTemplate: () => requestBlob('/api/v1/operations/dimensions/import/template'),
  },
```

- [ ] **Step 4: Add `resolveBatch` method to `techDocuments` namespace**

Find this code:

```typescript
    downloadUrl: (id: number) => request<string>(`/api/v1/tech-documents/${id}/download-url`),
    fileTypes: () => request<FileTypeDto[]>('/api/v1/tech-documents/file-types'),
    create: (body: UploadDocBody) =>
      request<UploadResponseDto>('/api/v1/tech-documents', { method: 'POST', body: JSON.stringify(body) }),
  },
```

Replace with:

```typescript
    downloadUrl: (id: number) => request<string>(`/api/v1/tech-documents/${id}/download-url`),
    fileTypes: () => request<FileTypeDto[]>('/api/v1/tech-documents/file-types'),
    create: (body: UploadDocBody) =>
      request<UploadResponseDto>('/api/v1/tech-documents', { method: 'POST', body: JSON.stringify(body) }),
    resolveBatch: (items: ResolveBatchItem[]) =>
      request<ResolveBatchResultDto[]>('/api/v1/tech-documents/resolve-batch', { method: 'POST', body: JSON.stringify(items) }),
  },
```

- [ ] **Step 5: Add new types near `UploadResponseDto`**

Find this code:

```typescript
export type UploadResponseDto = { documentId: number; objectKey: string; uploadUrl: string }
```

Replace with:

```typescript
export type UploadResponseDto = { documentId: number; objectKey: string; uploadUrl: string }

export type ResolveBatchItem = {
  fileName: string; fileTypeCode: string
  partNumber: string | null; partRevCode: string | null; routingRevCode: string | null
  opNumber: string | null; jobNumber: string | null
  segmentIndex: number | null; segmentTotal: number | null
  fileSizeBytes: number | null
}

export type ResolveBatchResultDto = {
  fileName: string
  status: 'Ready' | 'Invalid'
  reason: string | null
  fileTypeId: number | null
  partRevId: number | null; partOpId: number | null; jobId: number | null
  resolvedPartNumber: string | null; resolvedRevCode: string | null
  resolvedRoutingRevCode: string | null; resolvedOpNumber: string | null; resolvedJobNumber: string | null
  existingSegments: string[] | null
}
```

- [ ] **Step 6: Type-check**

Run: `cd clients/web && npx tsc --noEmit`
Expected: 0 errors

- [ ] **Step 7: Commit**

```bash
git add clients/web/lib/doc-format.ts clients/web/lib/api-client.ts
git commit -m "feat(web): add doc-format helpers + bulk-upload API client methods"
```

---

### Task 4: Frontend — refactor `documents/page.tsx` to use `doc-format.ts`

**Files:**
- Modify: `clients/web/app/(main)/documents/page.tsx`

- [ ] **Step 1: Remove local `FILE_TYPE_COLORS` and `formatBytes`, import from `doc-format.ts`**

Find this code (top of the file):

```typescript
import { api, type TechDocListDto, type FileTypeDto } from '@/lib/api-client'
import { VATopbar, VAKpi, VACard, VABtn, VABadge, VACombobox, type VAComboboxOption } from '@/components/va'
import { va, type VaBadgeKind } from '@/lib/va-tokens'

const FILE_TYPE_COLORS: Record<string, string> = {
  DRW: '#6D3B1A', GCD: '#E65100', RTC: '#5D4037',
  FXT: '#A0522D', TLS: '#795548', THD: '#F57C00', CAM: '#8D6E63', CAD: '#6D4C41',
}

const STATUS_KIND: Record<string, VaBadgeKind> = {
  Pending: 'warn', Approved: 'ok', Rejected: 'err',
}
```

Replace with:

```typescript
import { api, type TechDocListDto, type FileTypeDto } from '@/lib/api-client'
import { VATopbar, VAKpi, VACard, VABtn, VABadge, VACombobox, type VAComboboxOption } from '@/components/va'
import { va, type VaBadgeKind } from '@/lib/va-tokens'
import { FILE_TYPE_COLORS, formatBytes } from '@/lib/doc-format'

const STATUS_KIND: Record<string, VaBadgeKind> = {
  Pending: 'warn', Approved: 'ok', Rejected: 'err',
}
```

- [ ] **Step 2: Remove the local `formatBytes` function**

Find this code:

```typescript
function formatBytes(bytes: number | null): string {
  if (bytes == null) return '—'
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

const uniq = <T,>(arr: T[]) => [...new Set(arr)]
```

Replace with:

```typescript
const uniq = <T,>(arr: T[]) => [...new Set(arr)]
```

- [ ] **Step 3: Type-check**

Run: `cd clients/web && npx tsc --noEmit`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add "clients/web/app/(main)/documents/page.tsx"
git commit -m "refactor(web): extract FILE_TYPE_COLORS/formatBytes to lib/doc-format"
```

---

### Task 5: Frontend — template-download buttons in Import dialogs + i18n keys

**Files:**
- Modify: `clients/web/components/parts/import-ops-dialog.tsx`
- Modify: `clients/web/components/parts/import-dimensions-dialog.tsx`
- Modify: `clients/web/messages/vi.json`
- Modify: `clients/web/messages/en.json`

- [ ] **Step 1: Add `"template"` key to `parts.importOps` in `vi.json`**

Find this code:

```json
        "importOps": {
          "trigger": "Import OP",
          "title": "Import Operations từ Excel",
          "description": "File Excel (.xlsx) với cột: OpNumber, OpType, Description, Setup, Prod. Dòng đầu là header.",
          "fileLabel": "Chọn file",
```

Replace with:

```json
        "importOps": {
          "trigger": "Import OP",
          "title": "Import Operations từ Excel",
          "description": "File Excel (.xlsx) với cột: OpNumber, OpType, Description, Setup, Prod. Dòng đầu là header.",
          "template": "⬇ Tải file mẫu",
          "fileLabel": "Chọn file",
```

- [ ] **Step 2: Add `"template"` key to `parts.importDims` in `vi.json`**

Find this code:

```json
        "importDims": {
          "trigger": "⤓ Dims",
          "title": "Import Dimensions — OP {op}",
          "description": "File Excel (.xlsx) với cột: BalloonNumber, Code, Description, Nominal, TolPlus, TolMinus, Unit, Category. Dòng đầu là header.",
          "fileLabel": "Chọn file",
```

Replace with:

```json
        "importDims": {
          "trigger": "⤓ Dims",
          "title": "Import Dimensions — OP {op}",
          "description": "File Excel (.xlsx) với cột: BalloonNumber, Code, Description, Nominal, TolPlus, TolMinus, Unit, Category. Dòng đầu là header.",
          "template": "⬇ Tải file mẫu",
          "fileLabel": "Chọn file",
```

- [ ] **Step 3: Add `"template"` key to `parts.importOps` in `en.json`**

Find this code:

```json
        "importOps": {
          "trigger": "Import OP",
          "title": "Import Operations from Excel",
          "description": "Excel file (.xlsx) with columns: OpNumber, OpType, Description, Setup, Prod. First row is header.",
          "fileLabel": "Choose file",
```

Replace with:

```json
        "importOps": {
          "trigger": "Import OP",
          "title": "Import Operations from Excel",
          "description": "Excel file (.xlsx) with columns: OpNumber, OpType, Description, Setup, Prod. First row is header.",
          "template": "⬇ Download template",
          "fileLabel": "Choose file",
```

- [ ] **Step 4: Add `"template"` key to `parts.importDims` in `en.json`**

Find this code:

```json
        "importDims": {
          "trigger": "⤓ Dims",
          "title": "Import Dimensions — OP {op}",
          "description": "Excel file (.xlsx) with columns: BalloonNumber, Code, Description, Nominal, TolPlus, TolMinus, Unit, Category. First row is header.",
          "fileLabel": "Choose file",
```

Replace with:

```json
        "importDims": {
          "trigger": "⤓ Dims",
          "title": "Import Dimensions — OP {op}",
          "description": "Excel file (.xlsx) with columns: BalloonNumber, Code, Description, Nominal, TolPlus, TolMinus, Unit, Category. First row is header.",
          "template": "⬇ Download template",
          "fileLabel": "Choose file",
```

> Note: if the exact `en.json` wording above (`"Excel file (.xlsx) with columns: ..."`) does not match the file verbatim, locate the `parts.importOps`/`parts.importDims` blocks by their `"trigger"`/`"title"` keys (they are structurally identical to the `vi.json` blocks shown in Steps 1–2) and insert `"template": "⬇ Download template",` as the line immediately after `"description"` in each block.

- [ ] **Step 5: Add download button + handler to `import-ops-dialog.tsx`**

Find this code:

```typescript
'use client'

import { useState } from 'react'
import { useTranslations } from 'next-intl'
import { api, type ImportResultDto } from '@/lib/api-client'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

type Props = { open: boolean; routingRevId: number; onClose: () => void; onImported: () => void }
```

Replace with:

```typescript
'use client'

import { useState } from 'react'
import { useTranslations } from 'next-intl'
import { api, type ImportResultDto } from '@/lib/api-client'
import { downloadBlob } from '@/lib/doc-format'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

type Props = { open: boolean; routingRevId: number; onClose: () => void; onImported: () => void }

async function downloadTemplate() {
  const blob = await api.operations.importOpsTemplate()
  downloadBlob(blob, 'import-ops-template.xlsx')
}
```

Now find this code (inside the `{!result && (...)}` block):

```tsx
          {!result && (
            <>
              <input
                type="file" accept=".xlsx,.xls"
                onChange={e => setFile(e.target.files?.[0] ?? null)}
                className="text-sm"
              />
              {error && <p className="text-sm text-destructive">{error}</p>}
```

Replace with:

```tsx
          {!result && (
            <>
              <Button type="button" variant="link" className="h-auto p-0 text-sm" onClick={downloadTemplate}>
                {t('template')}
              </Button>
              <input
                type="file" accept=".xlsx,.xls"
                onChange={e => setFile(e.target.files?.[0] ?? null)}
                className="text-sm"
              />
              {error && <p className="text-sm text-destructive">{error}</p>}
```

- [ ] **Step 6: Add download button + handler to `import-dimensions-dialog.tsx`**

Read `clients/web/components/parts/import-dimensions-dialog.tsx` first — it mirrors `import-ops-dialog.tsx` (same imports, same `{!result && (...)}` structure, props `{ open: boolean; partOpId: number; opNumber: string; onClose: () => void; onImported: () => void }`, `useTranslations('parts.importDims')`).

Apply the same two edits as Step 5, with these differences:
- Import `downloadBlob` from `@/lib/doc-format` (same as Step 5).
- New `downloadTemplate` function uses `api.operations.importDimsTemplate()` and filename `'import-dimensions-template.xlsx'`:

```typescript
async function downloadTemplate() {
  const blob = await api.operations.importDimsTemplate()
  downloadBlob(blob, 'import-dimensions-template.xlsx')
}
```

- Insert the same `<Button type="button" variant="link" className="h-auto p-0 text-sm" onClick={downloadTemplate}>{t('template')}</Button>` as the first child inside the `{!result && (...)}` block, before the `<input type="file" .../>`.

- [ ] **Step 7: Type-check**

Run: `cd clients/web && npx tsc --noEmit`
Expected: 0 errors

- [ ] **Step 8: Commit**

```bash
git add clients/web/components/parts/import-ops-dialog.tsx clients/web/components/parts/import-dimensions-dialog.tsx clients/web/messages/vi.json clients/web/messages/en.json
git commit -m "feat(web): add template download buttons to OP/Dimension import dialogs"
```

---

### Task 6: Frontend — `lib/bulk-upload-parser.ts` (filename parsing + client-side checks)

**Files:**
- Create: `clients/web/lib/bulk-upload-parser.ts`

- [ ] **Step 1: Create `bulk-upload-parser.ts`**

```typescript
import type { FileTypeDto, ResolveBatchItem, ResolveBatchResultDto } from './api-client'

export const SEGMENT_RE = /^(\d+)_(\d+)$/

export type ParsedFile = {
  fileName: string
  ext: string
  fileTypeCode: string | null
  partNumber: string | null
  partRevCode: string | null
  routingRevCode: string | null
  opNumber: string | null
  jobNumber: string | null
  segmentIndex: number | null
  segmentTotal: number | null
}

/**
 * Parse tên file theo quy ước:
 *   Standard OP:   {PartNumber}-{PartRevCode}-{RoutingRevCode}-{OPNumber}-{FileTypeCode}[-{i}_{n}].ext
 *   Part-level:    {PartNumber}-{PartRevCode}-{FileTypeCode}.ext
 *   ForJobOnly OP: {JobNumber}-{OPNumber}-{FileTypeCode}.ext
 * Token cuối (FileTypeCode) được nhận diện qua danh sách FileTypeDto — các cờ
 * isJobNumber/isPartNumber/isOpNumber quyết định cấu trúc các token còn lại.
 * PartNumber/JobNumber có thể tự chứa "-" — phần còn lại được join lại bằng "-".
 */
export function parseFileName(fileName: string, fileTypes: FileTypeDto[]): ParsedFile {
  const dot = fileName.lastIndexOf('.')
  const ext = dot >= 0 ? fileName.slice(dot) : ''
  const base = dot >= 0 ? fileName.slice(0, dot) : fileName
  const tokens = base.split('-')

  let segmentIndex: number | null = null
  let segmentTotal: number | null = null
  const segMatch = tokens.length > 0 ? SEGMENT_RE.exec(tokens[tokens.length - 1]) : null
  if (segMatch) {
    segmentIndex = parseInt(segMatch[1], 10)
    segmentTotal = parseInt(segMatch[2], 10)
    tokens.pop()
  }

  const typeToken = tokens.pop()
  const fileType = fileTypes.find(ft => ft.code.toUpperCase() === (typeToken ?? '').toUpperCase())

  const base_result: ParsedFile = {
    fileName, ext,
    fileTypeCode: fileType?.code ?? null,
    partNumber: null, partRevCode: null, routingRevCode: null,
    opNumber: null, jobNumber: null, segmentIndex, segmentTotal,
  }

  if (!fileType) return base_result

  if (fileType.isJobNumber && fileType.isOpNumber) {
    const opNumber = tokens.pop() ?? null
    const jobNumber = tokens.length > 0 ? tokens.join('-') : null
    return { ...base_result, opNumber, jobNumber }
  }

  if (fileType.isPartNumber && fileType.isOpNumber) {
    const opNumber = tokens.pop() ?? null
    const routingRevCode = tokens.pop() ?? null
    const partRevCode = tokens.pop() ?? null
    const partNumber = tokens.length > 0 ? tokens.join('-') : null
    return { ...base_result, opNumber, routingRevCode, partRevCode, partNumber }
  }

  // Part-level (DRW/CAD): {PartNumber}-{PartRevCode}-{FileTypeCode}
  const partRevCode = tokens.pop() ?? null
  const partNumber = tokens.length > 0 ? tokens.join('-') : null
  return { ...base_result, partRevCode, partNumber }
}

export function toResolveBatchItem(p: ParsedFile, fileSizeBytes: number): ResolveBatchItem {
  return {
    fileName: p.fileName,
    fileTypeCode: p.fileTypeCode ?? '',
    partNumber: p.partNumber,
    partRevCode: p.partRevCode,
    routingRevCode: p.routingRevCode,
    opNumber: p.opNumber,
    jobNumber: p.jobNumber,
    segmentIndex: p.segmentIndex,
    segmentTotal: p.segmentTotal,
    fileSizeBytes,
  }
}

/** Token segment gốc (vd "1_3") dùng để lưu vào TechDocument.Segment khi upload. */
export function segmentToken(p: ParsedFile): string | null {
  return p.segmentIndex != null && p.segmentTotal != null ? `${p.segmentIndex}_${p.segmentTotal}` : null
}

export type BatchStatus = 'Ready' | 'Duplicate' | 'Invalid' | 'SegmentIncomplete' | 'Uploading' | 'Success' | 'Error'

export type BatchRow = {
  file: File
  fileName: string
  parsed: ParsedFile
  resolve: ResolveBatchResultDto | null
  status: BatchStatus
  reason: string | null
}

export function buildBatchRows(files: File[], fileTypes: FileTypeDto[]): BatchRow[] {
  return files.map(file => ({
    file,
    fileName: file.name,
    parsed: parseFileName(file.name, fileTypes),
    resolve: null,
    status: 'Invalid',
    reason: null,
  }))
}

export function mergeResolveResults(rows: BatchRow[], results: ResolveBatchResultDto[]): BatchRow[] {
  return rows.map((row, i) => {
    const resolve = results[i] ?? null
    return {
      ...row,
      resolve,
      status: (resolve?.status as BatchStatus) ?? 'Invalid',
      reason: resolve?.reason ?? null,
    }
  })
}

function dedupKey(resolve: ResolveBatchResultDto, parsed: ParsedFile): string {
  return [resolve.fileTypeId, resolve.partRevId, resolve.partOpId, resolve.jobId, parsed.segmentIndex ?? ''].join(':')
}

function segmentGroupKey(resolve: ResolveBatchResultDto): string {
  return [resolve.fileTypeId, resolve.partRevId, resolve.partOpId, resolve.jobId].join(':')
}

/**
 * Áp dụng kiểm tra phía client lên các row đang "Ready":
 * 1. Trùng lặp trong cùng lô (cùng fileType+Part/OP/Job+segment) → "Duplicate" (giữ row đầu tiên).
 * 2. Segment thiếu — group theo fileType+Part/OP/Job, kiểm tra đủ 1..segmentTotal
 *    (tính cả existingSegments đã có trên server) → nếu thiếu, toàn bộ group → "SegmentIncomplete".
 */
export function applyClientChecks(rows: BatchRow[]): BatchRow[] {
  const result = rows.map(r => ({ ...r }))

  const seen = new Set<string>()
  for (const r of result) {
    if (r.status !== 'Ready' || !r.resolve) continue
    const key = dedupKey(r.resolve, r.parsed)
    if (seen.has(key)) {
      r.status = 'Duplicate'
      r.reason = 'duplicateOf'
    } else {
      seen.add(key)
    }
  }

  const groups = new Map<string, BatchRow[]>()
  for (const r of result) {
    if (r.status !== 'Ready' || !r.resolve || !r.parsed.segmentTotal) continue
    const key = segmentGroupKey(r.resolve)
    const arr = groups.get(key) ?? []
    arr.push(r)
    groups.set(key, arr)
  }

  for (const groupRows of groups.values()) {
    const total = groupRows[0].parsed.segmentTotal!
    const present = new Set<number>()
    for (const r of groupRows) if (r.parsed.segmentIndex != null) present.add(r.parsed.segmentIndex)
    for (const seg of groupRows[0].resolve!.existingSegments ?? []) {
      const m = SEGMENT_RE.exec(seg)
      if (m) present.add(parseInt(m[1], 10))
    }

    const complete = Array.from({ length: total }, (_, i) => i + 1).every(n => present.has(n))
    if (!complete) {
      for (const r of groupRows) {
        r.status = 'SegmentIncomplete'
        r.reason = 'segmentMissing'
      }
    }
  }

  return result
}

/** Mô tả ngắn Part/Rev/Routing/OP hoặc Job/OP đã nhận diện được, dùng cho cột "Nhận diện". */
export function describeMatch(r: ResolveBatchResultDto): string {
  if (r.resolvedJobNumber) {
    return `${r.resolvedJobNumber} · OP ${r.resolvedOpNumber}`
  }
  if (r.resolvedPartNumber) {
    const parts = [r.resolvedPartNumber]
    if (r.resolvedRevCode) parts.push(`Rev ${r.resolvedRevCode}`)
    if (r.resolvedRoutingRevCode) parts.push(r.resolvedRoutingRevCode)
    if (r.resolvedOpNumber) parts.push(`OP ${r.resolvedOpNumber}`)
    return parts.join(' · ')
  }
  return '—'
}
```

- [ ] **Step 2: Type-check**

Run: `cd clients/web && npx tsc --noEmit`
Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git add clients/web/lib/bulk-upload-parser.ts
git commit -m "feat(web): add bulk-upload filename parser + client-side validation"
```

---

### Task 7: Frontend — i18n `documents.bulkUpload` namespace (vi + en)

**Files:**
- Modify: `clients/web/messages/vi.json`
- Modify: `clients/web/messages/en.json`

- [ ] **Step 1: Add `bulkUpload` block to `documents` namespace in `vi.json`**

Find this code (the end of the `documents` namespace):

```json
        "status": {
          "Pending": "Chờ duyệt",
          "Approved": "Đã duyệt",
          "Rejected": "Từ chối"
        }
      },
```

Replace with:

```json
        "status": {
          "Pending": "Chờ duyệt",
          "Approved": "Đã duyệt",
          "Rejected": "Từ chối"
        },
        "bulkUpload": {
          "trigger": "⬆⬆ Upload nhiều file",
          "title": "Upload nhiều file",
          "hint": "Chọn nhiều file hoặc một thư mục — hệ thống tự nhận diện Part/OP/Rev từ tên file theo quy ước đặt tên.",
          "selectFiles": "Chọn file...",
          "selectFolder": "Chọn thư mục...",
          "resolving": "Đang nhận diện file...",
          "errorResolve": "Lỗi nhận diện file",
          "table": {
            "headers": {
              "file": "Tên file",
              "type": "Loại",
              "detected": "Nhận diện",
              "size": "Kích thước",
              "status": "Trạng thái"
            }
          },
          "status": {
            "Ready": "Sẵn sàng",
            "Duplicate": "Trùng lặp",
            "Invalid": "Không hợp lệ",
            "SegmentIncomplete": "Thiếu segment",
            "Uploading": "Đang upload...",
            "Success": "Thành công",
            "Error": "Lỗi"
          },
          "reason": {
            "unrecognizedType": "Không nhận diện được loại file",
            "missingInfo": "Thiếu Part/OP/Rev tương ứng trong hệ thống",
            "unsupportedSegment": "Loại file này không hỗ trợ segment",
            "duplicateOf": "Trùng với file khác trong lô upload",
            "segmentMissing": "Thiếu một hoặc nhiều segment trong nhóm",
            "alreadyApproved": "File đã được approve — không thể ghi đè",
            "pendingByOther": "Đang chờ duyệt bởi người khác"
          },
          "readyCount": "{count}/{total} file sẵn sàng upload",
          "uploadButton": "Upload {count} file",
          "close": "Đóng",
          "cancel": "Huỷ"
        }
      },
```

- [ ] **Step 2: Add `bulkUpload` block to `documents` namespace in `en.json`**

Find this code (the end of the `documents` namespace — structurally identical to `vi.json`, English values):

```json
        "status": {
          "Pending": "Pending",
          "Approved": "Approved",
          "Rejected": "Rejected"
        }
      },
```

Replace with:

```json
        "status": {
          "Pending": "Pending",
          "Approved": "Approved",
          "Rejected": "Rejected"
        },
        "bulkUpload": {
          "trigger": "⬆⬆ Bulk upload",
          "title": "Bulk upload",
          "hint": "Select multiple files or a folder — the system auto-detects Part/OP/Rev from file names based on the naming convention.",
          "selectFiles": "Select files...",
          "selectFolder": "Select folder...",
          "resolving": "Resolving files...",
          "errorResolve": "Error resolving files",
          "table": {
            "headers": {
              "file": "File name",
              "type": "Type",
              "detected": "Detected",
              "size": "Size",
              "status": "Status"
            }
          },
          "status": {
            "Ready": "Ready",
            "Duplicate": "Duplicate",
            "Invalid": "Invalid",
            "SegmentIncomplete": "Segment incomplete",
            "Uploading": "Uploading...",
            "Success": "Success",
            "Error": "Error"
          },
          "reason": {
            "unrecognizedType": "File type not recognized",
            "missingInfo": "Matching Part/OP/Rev not found",
            "unsupportedSegment": "This file type does not support segments",
            "duplicateOf": "Duplicate of another file in this batch",
            "segmentMissing": "One or more segments missing from this group",
            "alreadyApproved": "File already approved — cannot overwrite",
            "pendingByOther": "Pending approval by another user"
          },
          "readyCount": "{count}/{total} files ready to upload",
          "uploadButton": "Upload {count} files",
          "close": "Close",
          "cancel": "Cancel"
        }
      },
```

> Note: if the exact `en.json` `status` block wording above does not match the file verbatim, locate the end of the `documents` namespace (the `status` object containing `Pending`/`Approved`/`Rejected` keys, immediately followed by the `documents` namespace's closing `}`) and apply the same edit — change that `status` block's trailing `}` to `},` and insert the `bulkUpload` block before the `documents` namespace closes.

- [ ] **Step 3: Validate JSON**

Run: `cd clients/web && node -e "JSON.parse(require('fs').readFileSync('messages/vi.json','utf8')); JSON.parse(require('fs').readFileSync('messages/en.json','utf8')); console.log('OK')"`
Expected: `OK`

- [ ] **Step 4: Commit**

```bash
git add clients/web/messages/vi.json clients/web/messages/en.json
git commit -m "feat(web): add i18n for documents bulk-upload dialog"
```

---

### Task 8: Frontend — `BulkUploadDialog` component

**Files:**
- Create: `clients/web/components/documents/bulk-upload-dialog.tsx`

- [ ] **Step 1: Create `bulk-upload-dialog.tsx`**

```tsx
'use client'

import { useState } from 'react'
import { useTranslations } from 'next-intl'
import { api, type FileTypeDto, type UploadDocBody } from '@/lib/api-client'
import {
  buildBatchRows, mergeResolveResults, applyClientChecks, toResolveBatchItem,
  segmentToken, describeMatch, type BatchRow, type BatchStatus,
} from '@/lib/bulk-upload-parser'
import { formatBytes } from '@/lib/doc-format'
import { va, type VaBadgeKind } from '@/lib/va-tokens'
import { VABtn, VABadge } from '@/components/va'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

type Props = { open: boolean; onClose: () => void; onDone: () => void }

const STATUS_KIND: Record<BatchStatus, VaBadgeKind> = {
  Ready: 'ok',
  Duplicate: 'warn',
  Invalid: 'err',
  SegmentIncomplete: 'warn',
  Uploading: 'primary',
  Success: 'ok',
  Error: 'err',
}

export function BulkUploadDialog({ open, onClose, onDone }: Props) {
  const t = useTranslations('documents.bulkUpload')
  const [rows, setRows] = useState<BatchRow[]>([])
  const [resolving, setResolving] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fileTypes, setFileTypes] = useState<FileTypeDto[] | null>(null)

  if (!open) return null

  function close() {
    setRows([]); setError(null); setResolving(false); setUploading(false)
    onClose()
  }

  async function ensureFileTypes(): Promise<FileTypeDto[]> {
    if (fileTypes) return fileTypes
    const res = await api.techDocuments.fileTypes()
    const list = res.success && res.data ? res.data : []
    setFileTypes(list)
    return list
  }

  async function handleFilesSelected(fileList: FileList | null) {
    if (!fileList || fileList.length === 0) return
    setError(null)
    setResolving(true)
    try {
      const files = Array.from(fileList)
      const types = await ensureFileTypes()
      const built = buildBatchRows(files, types)
      const items = built.map(r => toResolveBatchItem(r.parsed, r.file.size))
      const res = await api.techDocuments.resolveBatch(items)
      if (!res.success || !res.data) {
        setError(res.error ?? t('errorResolve'))
        setRows(built)
        return
      }
      const merged = mergeResolveResults(built, res.data)
      setRows(applyClientChecks(merged))
    } finally {
      setResolving(false)
    }
  }

  const readyCount = rows.filter(r => r.status === 'Ready').length

  async function handleUploadAll() {
    setUploading(true)
    for (let i = 0; i < rows.length; i++) {
      if (rows[i].status !== 'Ready') continue
      setRows(prev => prev.map((r, idx) => idx === i ? { ...r, status: 'Uploading' } : r))

      const row = rows[i]
      const resolve = row.resolve!
      const body: UploadDocBody = {
        fileTypeId: resolve.fileTypeId!,
        fileName: row.fileName,
        partRevId: resolve.partRevId,
        partOpId: resolve.partOpId,
        jobId: resolve.jobId,
        description: null,
        revision: null,
        code: null,
        segment: segmentToken(row.parsed),
        machineType: null,
        fileSizeBytes: row.file.size,
      }

      try {
        const res = await api.techDocuments.create(body)
        if (!res.success || !res.data) throw new Error(res.error ?? 'create failed')
        await fetch(res.data.uploadUrl, { method: 'PUT', body: row.file })
        setRows(prev => prev.map((r, idx) => idx === i ? { ...r, status: 'Success' } : r))
      } catch {
        setRows(prev => prev.map((r, idx) => idx === i ? { ...r, status: 'Error' } : r))
      }
    }
    setUploading(false)
    onDone()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="w-full max-w-4xl max-h-[85vh] flex flex-col">
        <CardHeader><CardTitle>{t('title')}</CardTitle></CardHeader>
        <CardContent className="space-y-4 overflow-y-auto flex-1">
          <p className="text-sm text-muted-foreground">{t('hint')}</p>

          <div className="flex gap-2">
            <label className="inline-flex">
              <input
                type="file" multiple className="hidden"
                onChange={e => handleFilesSelected(e.target.files)}
              />
              <span className="cursor-pointer"><VABtn kind="ghost">{t('selectFiles')}</VABtn></span>
            </label>
            <label className="inline-flex">
              <input
                type="file" multiple className="hidden"
                ref={el => {
                  if (el) (el as HTMLInputElement & { webkitdirectory: boolean }).webkitdirectory = true
                }}
                onChange={e => handleFilesSelected(e.target.files)}
              />
              <span className="cursor-pointer"><VABtn kind="ghost">{t('selectFolder')}</VABtn></span>
            </label>
          </div>

          {resolving && <p className="text-sm" style={{ color: va.text2 }}>{t('resolving')}</p>}
          {error && <p className="text-sm text-destructive">{error}</p>}

          {rows.length > 0 && (
            <div className="va-scroll" style={{ overflow: 'auto', maxHeight: 360, border: `1px solid ${va.border}`, borderRadius: 8 }}>
              <table style={{ width: '100%', fontSize: 12.5, borderCollapse: 'collapse' }}>
                <thead>
                  <tr>
                    <th style={{ position: 'sticky', top: 0, background: va.surface2, padding: '8px 10px', textAlign: 'left', borderBottom: `1px solid ${va.border}` }}>{t('table.headers.file')}</th>
                    <th style={{ position: 'sticky', top: 0, background: va.surface2, padding: '8px 10px', textAlign: 'left', borderBottom: `1px solid ${va.border}` }}>{t('table.headers.type')}</th>
                    <th style={{ position: 'sticky', top: 0, background: va.surface2, padding: '8px 10px', textAlign: 'left', borderBottom: `1px solid ${va.border}` }}>{t('table.headers.detected')}</th>
                    <th style={{ position: 'sticky', top: 0, background: va.surface2, padding: '8px 10px', textAlign: 'right', borderBottom: `1px solid ${va.border}` }}>{t('table.headers.size')}</th>
                    <th style={{ position: 'sticky', top: 0, background: va.surface2, padding: '8px 10px', textAlign: 'left', borderBottom: `1px solid ${va.border}` }}>{t('table.headers.status')}</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((r, i) => (
                    <tr key={i}>
                      <td style={{ padding: '7px 10px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontSize: 11.5 }}>{r.fileName}</td>
                      <td style={{ padding: '7px 10px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontSize: 11.5 }}>{r.parsed.fileTypeCode ?? '—'}</td>
                      <td style={{ padding: '7px 10px', borderBottom: `1px solid ${va.separator}`, fontSize: 11.5, color: va.text2 }}>
                        {r.resolve ? describeMatch(r.resolve) : '—'}
                      </td>
                      <td style={{ padding: '7px 10px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontSize: 11.5, textAlign: 'right', color: va.text2 }}>
                        {formatBytes(r.file.size)}
                      </td>
                      <td style={{ padding: '7px 10px', borderBottom: `1px solid ${va.separator}` }}>
                        <VABadge kind={STATUS_KIND[r.status]}>{t(`status.${r.status}`)}</VABadge>
                        {r.reason && <div style={{ fontSize: 10.5, color: va.text3, marginTop: 2 }}>{t(`reason.${r.reason}`)}</div>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {rows.length > 0 && (
            <p className="text-sm" style={{ color: va.text2 }}>{t('readyCount', { count: readyCount, total: rows.length })}</p>
          )}
        </CardContent>
        <div className="flex gap-2 justify-end p-4 pt-0">
          <VABtn kind="ghost" onClick={close}>{rows.some(r => r.status === 'Success') ? t('close') : t('cancel')}</VABtn>
          <VABtn kind="primary" disabled={readyCount === 0 || uploading || resolving} onClick={handleUploadAll}>
            {t('uploadButton', { count: readyCount })}
          </VABtn>
        </div>
      </Card>
    </div>
  )
}
```

- [ ] **Step 2: Type-check**

Run: `cd clients/web && npx tsc --noEmit`
Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git add clients/web/components/documents/bulk-upload-dialog.tsx
git commit -m "feat(web): add BulkUploadDialog component"
```

---

### Task 9: Frontend — wire `BulkUploadDialog` into `/documents`

**Files:**
- Modify: `clients/web/app/(main)/documents/page.tsx`

- [ ] **Step 1: Import `BulkUploadDialog`**

Find this code:

```typescript
import { api, type TechDocListDto, type FileTypeDto } from '@/lib/api-client'
import { VATopbar, VAKpi, VACard, VABtn, VABadge, VACombobox, type VAComboboxOption } from '@/components/va'
import { va, type VaBadgeKind } from '@/lib/va-tokens'
import { FILE_TYPE_COLORS, formatBytes } from '@/lib/doc-format'
```

Replace with:

```typescript
import { api, type TechDocListDto, type FileTypeDto } from '@/lib/api-client'
import { VATopbar, VAKpi, VACard, VABtn, VABadge, VACombobox, type VAComboboxOption } from '@/components/va'
import { va, type VaBadgeKind } from '@/lib/va-tokens'
import { FILE_TYPE_COLORS, formatBytes } from '@/lib/doc-format'
import { BulkUploadDialog } from '@/components/documents/bulk-upload-dialog'
```

- [ ] **Step 2: Add `bulkOpen` state**

Find this code:

```typescript
  const [uploadForm, setUploadForm] = useState<{ fileTypeId: string; file: File | null; revision: string; description: string; machineType: string } | null>(null)
  const [uploading, setUploading] = useState(false)
```

Replace with:

```typescript
  const [uploadForm, setUploadForm] = useState<{ fileTypeId: string; file: File | null; revision: string; description: string; machineType: string } | null>(null)
  const [uploading, setUploading] = useState(false)
  const [bulkOpen, setBulkOpen] = useState(false)
```

- [ ] **Step 3: Add trigger button to topbar `right` prop**

Find this code:

```tsx
      <VATopbar title={t('title')} breadcrumb={t('breadcrumb')}
        right={
          <div style={{ display: 'flex', gap: 8 }}>
            {backHref && (
              <Link href={backHref}>
                <VABtn kind="ghost">{t('backLink')}</VABtn>
              </Link>
            )}
            <VABtn kind="ghost" onClick={() => setFStatus('Pending')}>{t('queueButton', { count: pending })}</VABtn>
            {hasContext && (
              <VABtn kind="primary" onClick={() => setUploadForm({ fileTypeId: '', file: null, revision: '', description: '', machineType: '' })}>
                {t('uploadButton')}
              </VABtn>
            )}
          </div>
        } />
```

Replace with:

```tsx
      <VATopbar title={t('title')} breadcrumb={t('breadcrumb')}
        right={
          <div style={{ display: 'flex', gap: 8 }}>
            {backHref && (
              <Link href={backHref}>
                <VABtn kind="ghost">{t('backLink')}</VABtn>
              </Link>
            )}
            <VABtn kind="ghost" onClick={() => setFStatus('Pending')}>{t('queueButton', { count: pending })}</VABtn>
            <VABtn kind="ghost" onClick={() => setBulkOpen(true)}>{t('bulkUpload.trigger')}</VABtn>
            {hasContext && (
              <VABtn kind="primary" onClick={() => setUploadForm({ fileTypeId: '', file: null, revision: '', description: '', machineType: '' })}>
                {t('uploadButton')}
              </VABtn>
            )}
          </div>
        } />
```

- [ ] **Step 4: Render `BulkUploadDialog` at the end of the component**

Find this code (the final lines of the file):

```tsx
        </VACard>
      </div>
    </div>
  )
}
```

Replace with:

```tsx
        </VACard>
      </div>

      <BulkUploadDialog open={bulkOpen} onClose={() => setBulkOpen(false)} onDone={load} />
    </div>
  )
}
```

- [ ] **Step 5: Type-check**

Run: `cd clients/web && npx tsc --noEmit`
Expected: 0 errors

- [ ] **Step 6: Commit**

```bash
git add "clients/web/app/(main)/documents/page.tsx"
git commit -m "feat(web): wire BulkUploadDialog into /documents"
```

---

### Task 10: Final verification + CLAUDE.md update

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Full backend build**

Run: `dotnet build src/ShopfloorManager.sln`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 2: Restart the API process**

The new `GET /api/v1/operations/import/template`, `GET /api/v1/operations/dimensions/import/template`, and `POST /api/v1/tech-documents/resolve-batch` routes only become live after the running `dotnet run` process is restarted (per the existing "API process phải restart sau khi thêm route mới" lesson in `CLAUDE.md`).

```bash
# Stop the currently running API process, then:
cd src && dotnet run --project ShopfloorManager.API
```

- [ ] **Step 3: Full frontend type-check**

Run: `cd clients/web && npx tsc --noEmit`
Expected: 0 errors

- [ ] **Step 4: Browser test — template downloads**

With `npm run dev` running (http://localhost:3000):
1. Go to `/parts`, open a Routing Rev, click "+" to open "Import OP" dialog → click "⬇ Tải file mẫu" → verify `import-ops-template.xlsx` downloads with columns `OpNumber, OpType, Description, SetupTime, ProdTime` and one sample row.
2. From `/parts/[id]/operations`, open "⤓ Dims" import dialog → click "⬇ Tải file mẫu" → verify `import-dimensions-template.xlsx` downloads with columns `BalloonNumber, Code, Description, Nominal, TolPlus, TolMinus, Unit, Category` and one sample row.

- [ ] **Step 5: Browser test — bulk upload happy path**

1. Go to `/documents` → click "⬆⬆ Upload nhiều file".
2. Pick an existing Part/Rev/RoutingRev/OP combination from the doc list (e.g. part `00210155402`, rev `E`, an OP with a known `OpNumber` and the routing rev code shown in `/parts`).
3. Prepare 2-3 local test files named per convention, e.g. `00210155402-E-R1-10-TLS.txt`, `00210155402-E-R1-10-GCD-1_2.nc`, `00210155402-E-R1-10-GCD-2_2.nc`, and one deliberately bad file `UNKNOWNPART-E-R1-10-TLS.txt`.
4. Select all 4 files via "Chọn file...".
5. Verify the table shows: the 2 segment files as "Sẵn sàng" (Ready) — segment group complete (1_2 + 2_2); the standalone TLS file as "Sẵn sàng"; the `UNKNOWNPART...` file as "Không hợp lệ" (Invalid) with reason "Thiếu Part/OP/Rev tương ứng trong hệ thống".
6. Verify the "Nhận diện" column shows `00210155402 · Rev E · R1 · OP 10` for the 3 recognized files.
7. Verify footer shows `3/4 file sẵn sàng upload`.
8. Click "Upload 3 file" → verify each Ready row transitions to "Đang upload..." then "Thành công"; after completion, close the dialog and confirm the 3 new docs appear in the `/documents` table with correct Part/Rev/Routing/OP columns and file sizes.

- [ ] **Step 6: Browser test — duplicate + segment-incomplete detection**

1. Re-open "⬆⬆ Upload nhiều file" and select: `00210155402-E-R1-10-TLS.txt` twice (same name twice — simulate by copying the file with the identical name into the selection), plus a single segment file `00210155402-E-R1-10-GCD-1_3.nc` (no `2_3`/`3_3`).
2. Verify: the second copy of the TLS file is marked "Trùng lặp" (Duplicate) with reason "Trùng với file khác trong lô upload"; the lone `1_3` segment file is marked "Thiếu segment" (SegmentIncomplete) with reason "Thiếu một hoặc nhiều segment trong nhóm".
3. Verify "Upload" button is disabled or excludes these rows (only counts "Ready" rows).

- [ ] **Step 7: i18n check**

Switch language via `VALangSwitcher` (EN) and repeat a quick pass over `/documents` — verify the bulk-upload trigger button, dialog title/hint/buttons, table headers, and status/reason badges all render in English with no missing-key fallbacks (no raw `documents.bulkUpload.*` strings visible).

- [ ] **Step 8: Update `CLAUDE.md`**

Add a new dated section under "Project Status" (after the most recent "Web UI" entries), summarizing:
- New endpoints: `GET /api/v1/operations/import/template`, `GET /api/v1/operations/dimensions/import/template`, `POST /api/v1/tech-documents/resolve-batch`
- `ExcelTemplateBuilder` (API/Common) + `ResolveBulkUploadQuery`/`ResolveBulkUploadQueryHandler` (Application/Production)
- New frontend files: `lib/doc-format.ts`, `lib/bulk-upload-parser.ts`, `components/documents/bulk-upload-dialog.tsx`
- Filename naming convention used for bulk-upload parsing (Standard OP / Part-level / ForJobOnly OP forms)
- Verification results from Steps 4-7 (browser test outcomes, part numbers used)

- [ ] **Step 9: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: log Excel template download + bulk upload implementation"
```

---

## Out of Scope (per spec, not implemented in this plan)

- Server-side persistence of "rejected/invalid file" logs across sessions (the upload log is in-memory/dialog-scoped only)
- Drag-and-drop file selection (only `<input type="file">` pickers)
- Editing/overriding the auto-detected Part/Rev/Routing/OP per file before upload
- Resumable/retryable uploads for files that fail mid-batch (user must re-select and re-run)
