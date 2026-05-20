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
    bool IsVisible, bool IsComplete);

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

        var all = templateOps.Concat(jobOps)
            .OrderBy(o => o.OpNumberSort ?? 0)
            .Select(o => new PartOpDto(o.Id, o.RoutingRevId, o.JobId, o.ForJobOnly,
                o.OpNumber, o.OpNumberSort, o.OpTypeId, o.OpType != null ? o.OpType.Name : null,
                o.Description, o.Note, o.SetupTime, o.ProdTime, o.IsVisible, o.IsComplete))
            .ToList();

        return Result.Ok(all);
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
                o.Description, o.Note, o.SetupTime, o.ProdTime, o.IsVisible, o.IsComplete))
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
            op.Description, op.Note, op.SetupTime, op.ProdTime, op.IsVisible, op.IsComplete));
    }
}
