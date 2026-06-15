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
