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

    /// <summary>
    /// Danh sách tài liệu theo context:
    ///   ?partRevId=  → Part-level docs (DRW, CAD)
    ///   ?partOpId=   → OP-level docs (GCD, RTC, FXT...) cho standard hoặc ForJobOnly OP
    ///   ?jobId=      → tất cả docs của job (ForJobOnly OPs)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDocuments(
        [FromQuery] int? partRevId,
        [FromQuery] int? partOpId,
        [FromQuery] int? jobId,
        [FromQuery] string? status)
    {
        var q = db.TechDocuments
            .Include(t => t.FileType)
            .Include(t => t.Creator)
            .AsQueryable();

        if (partRevId.HasValue) q = q.Where(t => t.PartRevId == partRevId.Value);
        if (partOpId.HasValue)  q = q.Where(t => t.PartOpId  == partOpId.Value);
        if (jobId.HasValue)     q = q.Where(t => t.JobId     == jobId.Value);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<FileStatus>(status, true, out var fs))
            q = q.Where(t => t.Status == fs);

        var docs = await q
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<List<TechDocDto>>.Ok(
            docs.Select(t => new TechDocDto(
                t.Id, t.FileType.Code, t.FileType.Name,
                t.PartRevId, t.PartOpId, t.JobId,
                t.Description, t.Revision, t.Code, t.Segment, t.MachineType,
                t.Status.ToString(), t.Creator.Name, t.CreatedAt)).ToList()));
    }

    /// <summary>
    /// Yêu cầu upload — validate upload rules, build MinIO path, trả về pre-signed URL.
    ///
    /// Upload rules:
    ///   Rule 1: BLOCK nếu Status=Approved
    ///   Rule 2: BLOCK nếu Status=Pending + CreatedBy ≠ current user
    ///   Rule 3: Nếu Status=Rejected → rename file cũ, allow upload, reset Pending
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Administrator,Manager,Engineer")]
    public async Task<IActionResult> RequestUpload([FromBody] UploadRequest req)
    {
        var fileType = await db.FileTypes.FindAsync([req.FileTypeId]);
        if (fileType is null)
            return BadRequest(ApiResponse<UploadResponse>.Fail("FileType không hợp lệ."));

        // Validate: phải có ít nhất partRevId hoặc partOpId
        if (!req.PartRevId.HasValue && !req.PartOpId.HasValue)
            return BadRequest(ApiResponse<UploadResponse>.Fail("Cần truyền partRevId (Part-level) hoặc partOpId (OP-level)."));

        // Build path context
        string? partNumber = null, revCode = null, opNumber = null, jobNumber = null;

        if (req.PartRevId.HasValue)
        {
            var rev = await db.PartRevs
                .Include(r => r.Part)
                .FirstOrDefaultAsync(r => r.Id == req.PartRevId.Value);
            if (rev is not null) { partNumber = rev.Part.PartNumber; revCode = rev.RevCode; }
        }

        if (req.PartOpId.HasValue)
        {
            var op = await db.PartOps
                .Include(o => o.RoutingRev).ThenInclude(r => r!.Routing)
                    .ThenInclude(rt => rt.PartRev).ThenInclude(pr => pr.Part)
                .FirstOrDefaultAsync(o => o.Id == req.PartOpId.Value);

            if (op is not null)
            {
                opNumber = op.OpNumber;
                if (op.RoutingRev is not null)
                {
                    partNumber ??= op.RoutingRev.Routing.PartRev.Part.PartNumber;
                    revCode    ??= op.RoutingRev.Routing.PartRev.RevCode;
                }
            }
        }

        if (req.JobId.HasValue)
        {
            var job = await db.Jobs.FindAsync([req.JobId.Value]);
            jobNumber = job?.JobNumber;
        }

        var objectKey = MinioPathBuilder.BuildObjectKey(
            folder:       fileType.Folder ?? fileType.Code.ToLower(),
            fileName:     req.FileName,
            partNumber:   partNumber,
            revCode:      revCode,
            opNumber:     opNumber,
            jobNumber:    jobNumber,
            isPartNumber: fileType.IsPartNumber,
            isRevision:   fileType.IsRevision,
            isOpNumber:   fileType.IsOpNumber,
            isJobNumber:  fileType.IsJobNumber && jobNumber is not null);

        // Tìm existing doc cùng context
        var existing = await db.TechDocuments.FirstOrDefaultAsync(t =>
            t.PartRevId  == req.PartRevId  &&
            t.PartOpId   == req.PartOpId   &&
            t.FileTypeId == req.FileTypeId &&
            (req.Segment == null || t.Segment == req.Segment));

        if (existing is not null)
        {
            if (existing.Status == FileStatus.Approved)
                return Conflict(ApiResponse<UploadResponse>.Fail(
                    "File đã được approve. Không thể cập nhật — hãy tạo RoutingRev mới."));

            if (existing.Status == FileStatus.Pending && existing.CreatedBy != UserId)
                return Conflict(ApiResponse<UploadResponse>.Fail(
                    "File đang chờ duyệt bởi người khác."));

            if (existing.Status == FileStatus.Rejected)
            {
                var dir     = existing.StoragePath.Contains('/') ? existing.StoragePath[..existing.StoragePath.LastIndexOf('/')] : "";
                var oldName = System.IO.Path.GetFileName(existing.StoragePath);
                var rejectedKey = (dir.Length > 0 ? dir + "/" : "") + "Rejected_" + oldName;
                await minio.RenameAsync(existing.StoragePath, rejectedKey);
            }

            existing.StoragePath = objectKey;
            existing.Description = req.Description;
            existing.Revision    = req.Revision;
            existing.Code        = req.Code;
            existing.Segment     = req.Segment;
            existing.MachineType = req.MachineType;
            existing.Status      = FileStatus.Pending;
            existing.InspectorId = null;
            existing.InspectedAt = null;
            existing.InspectNote = null;
            existing.CreatedBy   = UserId;
            existing.CreatedAt   = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            var url = await minio.GetUploadUrlAsync(objectKey);
            return Ok(ApiResponse<UploadResponse>.Ok(new UploadResponse(existing.Id, objectKey, url)));
        }

        var doc = new TechDocument
        {
            FileTypeId  = req.FileTypeId,
            PartRevId   = req.PartRevId,
            PartOpId    = req.PartOpId,
            JobId       = req.JobId,
            StoragePath = objectKey,
            Description = req.Description,
            Revision    = req.Revision,
            Code        = req.Code,
            Segment     = req.Segment,
            MachineType = req.MachineType,
            CreatedBy   = UserId
        };
        db.TechDocuments.Add(doc);
        await db.SaveChangesAsync();

        var uploadUrl = await minio.GetUploadUrlAsync(objectKey);
        return StatusCode(201, ApiResponse<UploadResponse>.Ok(
            new UploadResponse(doc.Id, objectKey, uploadUrl)));
    }

    /// <summary>Pre-signed URL để download/xem file.</summary>
    [HttpGet("{id:long}/download-url")]
    public async Task<IActionResult> GetDownloadUrl(long id)
    {
        var doc = await db.TechDocuments.FindAsync([id]);
        if (doc is null) return NotFound(ApiResponse<string>.Fail("Không tìm thấy tài liệu."));
        var url = await minio.GetDownloadUrlAsync(doc.StoragePath);
        return Ok(ApiResponse<string>.Ok(url));
    }

    /// <summary>Inspector duyệt hoặc từ chối tài liệu.</summary>
    [HttpPut("{id:long}/inspect")]
    [Authorize(Roles = "Administrator,Manager,QC Inspector")]
    public async Task<IActionResult> Inspect(long id, [FromBody] InspectRequest req)
    {
        var doc = await db.TechDocuments
            .Include(t => t.FileType)
            .Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (doc is null) return NotFound(ApiResponse<TechDocDto>.Fail("Không tìm thấy tài liệu."));
        if (doc.Status == FileStatus.Approved)
            return Conflict(ApiResponse<TechDocDto>.Fail("Tài liệu đã được approve."));

        doc.Status      = req.Approve ? FileStatus.Approved : FileStatus.Rejected;
        doc.InspectorId = UserId;
        doc.InspectedAt = DateTimeOffset.UtcNow;
        doc.InspectNote = req.Note;
        await db.SaveChangesAsync();

        return Ok(ApiResponse<TechDocDto>.Ok(new TechDocDto(
            doc.Id, doc.FileType.Code, doc.FileType.Name,
            doc.PartRevId, doc.PartOpId, doc.JobId,
            doc.Description, doc.Revision, doc.Code, doc.Segment, doc.MachineType,
            doc.Status.ToString(), doc.Creator.Name, doc.CreatedAt)));
    }

    /// <summary>
    /// GET /api/v1/tech-documents/pending — danh sách chờ Inspector duyệt.
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Administrator,Manager,QC Inspector")]
    public async Task<IActionResult> GetPending()
    {
        var docs = await db.TechDocuments
            .Include(t => t.FileType)
            .Include(t => t.Creator)
            .Where(t => t.Status == FileStatus.Pending)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<List<TechDocDto>>.Ok(
            docs.Select(t => new TechDocDto(
                t.Id, t.FileType.Code, t.FileType.Name,
                t.PartRevId, t.PartOpId, t.JobId,
                t.Description, t.Revision, t.Code, t.Segment, t.MachineType,
                t.Status.ToString(), t.Creator.Name, t.CreatedAt)).ToList()));
    }
}

public record TechDocDto(
    long Id, string FileTypeCode, string FileTypeName,
    int? PartRevId, int? PartOpId, int? JobId,
    string? Description, string? Revision,
    string? Code, string? Segment, string? MachineType,
    string Status, string CreatedByName, DateTimeOffset CreatedAt);

public record UploadRequest(
    int FileTypeId,
    string FileName,
    int? PartRevId,
    int? PartOpId,
    int? JobId,
    string? Description,
    string? Revision,
    string? Code,
    string? Segment,
    string? MachineType);

public record UploadResponse(long DocumentId, string ObjectKey, string UploadUrl);
public record InspectRequest(bool Approve, string? Note);
