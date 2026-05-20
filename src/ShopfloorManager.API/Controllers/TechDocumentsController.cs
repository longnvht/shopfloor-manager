using System.Security.Claims;
using MediatR;
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
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "0");

    /// <summary>Lấy danh sách file kỹ thuật theo job/part/op.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TechDocDto>>), 200)]
    public async Task<IActionResult> GetDocuments([FromQuery] int? jobId, [FromQuery] int? partId, [FromQuery] int? partOpId)
    {
        var q = db.TechDocuments.Include(t => t.FileType).Include(t => t.Creator).AsQueryable();
        if (jobId.HasValue) q = q.Where(t => t.JobId == jobId.Value);
        if (partId.HasValue) q = q.Where(t => t.PartId == partId.Value);
        if (partOpId.HasValue) q = q.Where(t => t.PartOpId == partOpId.Value);

        var docs = await q.OrderByDescending(t => t.CreatedAt)
            .Select(t => new TechDocDto(t.Id, t.FileType.Code, t.FileType.Name,
                t.Description, t.Revision, t.Status.ToString(),
                t.Creator!.Name, t.CreatedAt))
            .ToListAsync();

        return Ok(ApiResponse<List<TechDocDto>>.Ok(docs));
    }

    /// <summary>Tạo record + nhận pre-signed URL để upload file lên MinIO.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrator,Manager,Engineer")]
    [ProducesResponseType(typeof(ApiResponse<UploadResponse>), 201)]
    public async Task<IActionResult> RequestUpload([FromBody] UploadRequest req)
    {
        var fileType = await db.FileTypes.FindAsync([req.FileTypeId]);
        if (fileType is null) return BadRequest(ApiResponse<UploadResponse>.Fail("FileType không hợp lệ."));

        var ext = Path.GetExtension(req.FileName);
        var objectKey = $"{fileType.Folder}/{req.JobId}/{DateTimeOffset.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";

        var doc = new TechDocument
        {
            FileTypeId = req.FileTypeId,
            JobId = req.JobId,
            PartId = req.PartId,
            PartOpId = req.PartOpId,
            StoragePath = objectKey,
            Description = req.Description,
            Revision = req.Revision,
            CreatedBy = UserId
        };
        db.TechDocuments.Add(doc);
        await db.SaveChangesAsync();

        var uploadUrl = await minio.GetUploadUrlAsync(objectKey);
        return StatusCode(201, ApiResponse<UploadResponse>.Ok(new UploadResponse(doc.Id, objectKey, uploadUrl)));
    }

    /// <summary>Lấy pre-signed URL để xem/download file.</summary>
    [HttpGet("{id:long}/download-url")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> GetDownloadUrl(long id)
    {
        var doc = await db.TechDocuments.FindAsync([id]);
        if (doc is null) return NotFound(ApiResponse<string>.Fail("Không tìm thấy tài liệu."));

        var url = await minio.GetDownloadUrlAsync(doc.StoragePath);
        return Ok(ApiResponse<string>.Ok(url));
    }

    /// <summary>Duyệt hoặc từ chối tài liệu (QC Inspector / Manager).</summary>
    [HttpPut("{id:long}/inspect")]
    [Authorize(Roles = "Administrator,Manager,QC Inspector")]
    [ProducesResponseType(typeof(ApiResponse<TechDocDto>), 200)]
    public async Task<IActionResult> Inspect(long id, [FromBody] InspectRequest req)
    {
        var doc = await db.TechDocuments
            .Include(t => t.FileType).Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (doc is null) return NotFound(ApiResponse<TechDocDto>.Fail("Không tìm thấy tài liệu."));

        doc.Status = req.Approve ? FileStatus.Approved : FileStatus.Rejected;
        doc.InspectorId = UserId;
        doc.InspectedAt = DateTimeOffset.UtcNow;
        doc.InspectNote = req.Note;
        await db.SaveChangesAsync();

        return Ok(ApiResponse<TechDocDto>.Ok(new TechDocDto(doc.Id, doc.FileType.Code, doc.FileType.Name,
            doc.Description, doc.Revision, doc.Status.ToString(), doc.Creator!.Name, doc.CreatedAt)));
    }
}

public record TechDocDto(long Id, string FileTypeCode, string FileTypeName,
    string? Description, string? Revision, string Status, string CreatedByName, DateTimeOffset CreatedAt);
public record UploadRequest(int FileTypeId, string FileName, int? JobId, int? PartId, int? PartOpId,
    string? Description, string? Revision);
public record UploadResponse(long DocumentId, string ObjectKey, string UploadUrl);
public record InspectRequest(bool Approve, string? Note);
