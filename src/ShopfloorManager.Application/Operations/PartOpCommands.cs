using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Operations;

public record PartOpDto(
    int Id, string OpNumber, decimal? OpNumberSort,
    int? PartId, int? JobId, int? OpTypeId, string? OpTypeName,
    string? Description, string? Note,
    decimal? SetupTime, decimal? ProdTime,
    bool IsVisible, bool IsComplete, DateTimeOffset CreatedAt);

// ── Queries ───────────────────────────────────────────────────

public record GetPartOpsQuery(int? PartId = null, int? JobId = null) : IRequest<Result<List<PartOpDto>>>;

public class GetPartOpsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetPartOpsQuery, Result<List<PartOpDto>>>
{
    public async Task<Result<List<PartOpDto>>> Handle(GetPartOpsQuery req, CancellationToken ct)
    {
        var q = db.PartOps.Include(o => o.OpType).AsQueryable();
        if (req.PartId.HasValue) q = q.Where(o => o.PartId == req.PartId.Value);
        if (req.JobId.HasValue) q = q.Where(o => o.JobId == req.JobId.Value || o.JobId == null);

        var items = await q.Where(o => o.IsVisible)
            .OrderBy(o => o.OpNumberSort ?? 0)
            .Select(o => new PartOpDto(o.Id, o.OpNumber, o.OpNumberSort,
                o.PartId, o.JobId, o.OpTypeId, o.OpType!.Name,
                o.Description, o.Note, o.SetupTime, o.ProdTime,
                o.IsVisible, o.IsComplete, o.CreatedAt))
            .ToListAsync(ct);

        return Result.Ok(items);
    }
}

// ── Commands ──────────────────────────────────────────────────

public record CreatePartOpCommand(
    string OpNumber, int? PartId, int? JobId, int? OpTypeId,
    string? Description, string? Note,
    decimal? SetupTime, decimal? ProdTime, int? RequesterId)
    : IRequest<Result<PartOpDto>>;

public class CreatePartOpCommandValidator : AbstractValidator<CreatePartOpCommand>
{
    public CreatePartOpCommandValidator()
    {
        RuleFor(x => x.OpNumber).NotEmpty().MaximumLength(10);
        RuleFor(x => x).Must(x => x.PartId.HasValue || x.JobId.HasValue)
            .WithMessage("Phải chỉ định PartId hoặc JobId.");
    }
}

public class CreatePartOpCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreatePartOpCommand, Result<PartOpDto>>
{
    public async Task<Result<PartOpDto>> Handle(CreatePartOpCommand req, CancellationToken ct)
    {
        // Parse sort order from op number (e.g. "10" → 10.0, "10.1" → 10.1)
        decimal.TryParse(req.OpNumber, out var sort);

        var op = new PartOp
        {
            OpNumber = req.OpNumber,
            OpNumberSort = sort,
            PartId = req.PartId,
            JobId = req.JobId,
            OpTypeId = req.OpTypeId,
            Description = req.Description,
            Note = req.Note,
            SetupTime = req.SetupTime,
            ProdTime = req.ProdTime,
            IsForJobOnly = req.JobId.HasValue,
            CreatedBy = req.RequesterId
        };
        db.PartOps.Add(op);
        await db.SaveChangesAsync(ct);

        var opType = req.OpTypeId.HasValue
            ? await db.OpTypes.FindAsync([req.OpTypeId.Value], ct)
            : null;

        return Result.Ok(new PartOpDto(op.Id, op.OpNumber, op.OpNumberSort,
            op.PartId, op.JobId, op.OpTypeId, opType?.Name,
            op.Description, op.Note, op.SetupTime, op.ProdTime,
            op.IsVisible, op.IsComplete, op.CreatedAt));
    }
}
