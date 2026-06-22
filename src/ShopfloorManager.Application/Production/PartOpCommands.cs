using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Production;

public record PartOpDto(
    int Id, int? RoutingRevId, int? JobId, bool ForJobOnly,
    string OpNumber, decimal? OpNumberSort,
    int? OpTypeId, string? OpTypeName,
    string? Description, string? Note,
    decimal? SetupTime, decimal? ProdTime,
    bool IsVisible, bool IsComplete,
    int DimCount, int DocCount,
    string? OpTypeCode = null);

// ── Queries ───────────────────────────────────────────────────

/// <summary>
/// Lấy danh sách PartOps của một Job:
/// = RoutingRev.PartOps (template) UNION Job.ForJobOnly OPs.
/// </summary>
public record GetJobOpsQuery(int JobId) : IRequest<Result<List<PartOpDto>>>;

public class GetJobOpsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetJobOpsQuery, Result<List<PartOpDto>>>
{
    public async Task<Result<List<PartOpDto>>> Handle(GetJobOpsQuery req, CancellationToken ct)
    {
        var job = await db.Jobs.FindAsync([req.JobId], ct);
        if (job is null) return Result.Fail($"Không tìm thấy Job ID {req.JobId}.");

        // Template OPs từ RoutingRev được snapshot trong Job
        var templateOps = await db.PartOps
            .Include(o => o.OpType)
            .Where(o => o.RoutingRevId == job.RoutingRevId && o.IsVisible)
            .ToListAsync(ct);

        // OPs riêng của Job này (ForJobOnly=true)
        var jobOps = await db.PartOps
            .Include(o => o.OpType)
            .Where(o => o.JobId == req.JobId && o.ForJobOnly && o.IsVisible)
            .ToListAsync(ct);

        var all = templateOps.Concat(jobOps).OrderBy(o => o.OpNumberSort ?? 0).ToList();
        var allIds = all.Select(o => o.Id).ToList();

        var dimCountsByOp = (await db.Dimensions
                .Where(d => allIds.Contains(d.PartOpId))
                .GroupBy(d => d.PartOpId)
                .Select(g => new { PartOpId = g.Key, Count = g.Count() })
                .ToListAsync(ct))
            .ToDictionary(x => x.PartOpId, x => x.Count);

        decimal EffectiveSort(PartOp p) => p.OpNumberSort ?? 9999m;

        var result = all.Select(o =>
        {
            var isInspectionOp = string.Equals(o.OpType?.Code, "INSP", StringComparison.OrdinalIgnoreCase);
            int dimCount;
            if (isInspectionOp)
            {
                var priorOpIds = all.Where(p => EffectiveSort(p) < EffectiveSort(o)).Select(p => p.Id).ToHashSet();
                dimCount = dimCountsByOp.Where(kv => priorOpIds.Contains(kv.Key)).Sum(kv => kv.Value);
            }
            else
            {
                dimCount = dimCountsByOp.GetValueOrDefault(o.Id, 0);
            }

            return new PartOpDto(o.Id, o.RoutingRevId, o.JobId, o.ForJobOnly,
                o.OpNumber, o.OpNumberSort, o.OpTypeId, o.OpType?.Name,
                o.Description, o.Note, o.SetupTime, o.ProdTime, o.IsVisible, o.IsComplete,
                dimCount, 0, o.OpType?.Code);
        }).ToList();

        return Result.Ok(result);
    }
}

public record GetRoutingRevOpsQuery(int RoutingRevId) : IRequest<Result<List<PartOpDto>>>;

public class GetRoutingRevOpsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetRoutingRevOpsQuery, Result<List<PartOpDto>>>
{
    public async Task<Result<List<PartOpDto>>> Handle(GetRoutingRevOpsQuery req, CancellationToken ct)
    {
        var items = await db.PartOps
            .Include(o => o.OpType)
            .Where(o => o.RoutingRevId == req.RoutingRevId && o.IsVisible)
            .OrderBy(o => o.OpNumberSort ?? 0)
            .Select(o => new PartOpDto(o.Id, o.RoutingRevId, o.JobId, o.ForJobOnly,
                o.OpNumber, o.OpNumberSort, o.OpTypeId, o.OpType != null ? o.OpType.Name : null,
                o.Description, o.Note, o.SetupTime, o.ProdTime, o.IsVisible, o.IsComplete,
                db.Dimensions.Count(d => d.PartOpId == o.Id),
                db.TechDocuments.Count(td => td.PartOpId == o.Id && td.DeletedAt == null),
                o.OpType != null ? o.OpType.Code : null))
            .ToListAsync(ct);
        return Result.Ok(items);
    }
}

// ── Commands ──────────────────────────────────────────────────

public record CreatePartOpCommand(
    int? RoutingRevId, int? JobId,
    string OpNumber, int? OpTypeId,
    string? Description, string? Note,
    decimal? SetupTime, decimal? ProdTime,
    int? RequesterId)
    : IRequest<Result<PartOpDto>>;

public class CreatePartOpCommandValidator : AbstractValidator<CreatePartOpCommand>
{
    public CreatePartOpCommandValidator()
    {
        RuleFor(x => x.OpNumber).NotEmpty().MaximumLength(10);
        RuleFor(x => x).Must(x => x.RoutingRevId.HasValue || x.JobId.HasValue)
            .WithMessage("Phải chỉ định RoutingRevId (template) hoặc JobId (ForJobOnly).");
    }
}

public class CreatePartOpCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreatePartOpCommand, Result<PartOpDto>>
{
    public async Task<Result<PartOpDto>> Handle(CreatePartOpCommand req, CancellationToken ct)
    {
        decimal.TryParse(req.OpNumber, out var sort);

        var op = new PartOp
        {
            RoutingRevId = req.RoutingRevId,
            JobId = req.JobId,
            ForJobOnly = req.JobId.HasValue && !req.RoutingRevId.HasValue,
            OpNumber = req.OpNumber, OpNumberSort = sort,
            OpTypeId = req.OpTypeId,
            Description = req.Description, Note = req.Note,
            SetupTime = req.SetupTime, ProdTime = req.ProdTime,
            CreatedBy = req.RequesterId
        };
        db.PartOps.Add(op);
        await db.SaveChangesAsync(ct);

        var opType = req.OpTypeId.HasValue
            ? await db.OpTypes.FindAsync([req.OpTypeId.Value], ct) : null;

        return Result.Ok(new PartOpDto(op.Id, op.RoutingRevId, op.JobId, op.ForJobOnly,
            op.OpNumber, op.OpNumberSort, op.OpTypeId, opType?.Name,
            op.Description, op.Note, op.SetupTime, op.ProdTime, op.IsVisible, op.IsComplete, 0, 0));
    }
}

// ── Import Operations từ Excel ───────────────────────────────

public record ImportOpRow(string OpNumber, string? OpTypeCode, string? Description, decimal? SetupTime, decimal? ProdTime);

public record ImportOpsCommand(int RoutingRevId, List<ImportOpRow> Rows, int? RequesterId)
    : IRequest<Result<ImportResultDto>>;

public class ImportOpsCommandValidator : AbstractValidator<ImportOpsCommand>
{
    public ImportOpsCommandValidator()
    {
        RuleFor(x => x.RoutingRevId).GreaterThan(0);
        RuleFor(x => x.Rows).NotEmpty();
    }
}

public class ImportOpsCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<ImportOpsCommand, Result<ImportResultDto>>
{
    public async Task<Result<ImportResultDto>> Handle(ImportOpsCommand req, CancellationToken ct)
    {
        var existingOps = await db.PartOps
            .Where(o => o.RoutingRevId == req.RoutingRevId)
            .ToDictionaryAsync(o => o.OpNumber, ct);

        var opTypes = await db.OpTypes.ToListAsync(ct);

        var errors = new List<ImportRowError>();
        int created = 0, updated = 0;

        for (var i = 0; i < req.Rows.Count; i++)
        {
            var row = req.Rows[i];
            var rowNumber = i + 2; // dòng 1 là header

            if (string.IsNullOrWhiteSpace(row.OpNumber))
            {
                errors.Add(new ImportRowError(rowNumber, "Thiếu OpNumber."));
                continue;
            }

            int? opTypeId = null;
            if (!string.IsNullOrWhiteSpace(row.OpTypeCode))
            {
                var opType = opTypes.FirstOrDefault(t => string.Equals(t.Code, row.OpTypeCode, StringComparison.OrdinalIgnoreCase));
                if (opType is null)
                    errors.Add(new ImportRowError(rowNumber, $"Không tìm thấy OpType '{row.OpTypeCode}'."));
                else
                    opTypeId = opType.Id;
            }

            if (existingOps.TryGetValue(row.OpNumber, out var existing))
            {
                existing.Description = row.Description;
                existing.OpTypeId = opTypeId;
                existing.SetupTime = row.SetupTime;
                existing.ProdTime = row.ProdTime;
                updated++;
            }
            else
            {
                decimal.TryParse(row.OpNumber, out var sort);
                db.PartOps.Add(new PartOp
                {
                    RoutingRevId = req.RoutingRevId,
                    OpNumber = row.OpNumber, OpNumberSort = sort,
                    OpTypeId = opTypeId,
                    Description = row.Description,
                    SetupTime = row.SetupTime, ProdTime = row.ProdTime,
                    CreatedBy = req.RequesterId
                });
                created++;
            }
        }

        await db.SaveChangesAsync(ct);

        return Result.Ok(new ImportResultDto(created, updated, 0, errors));
    }
}
