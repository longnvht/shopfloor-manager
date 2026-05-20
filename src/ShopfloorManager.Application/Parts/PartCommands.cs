using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Shared.Pagination;

namespace ShopfloorManager.Application.Parts;

// ── Queries ───────────────────────────────────────────────────

public record GetPartsQuery(int Page = 1, int PageSize = 20, string? Search = null, bool? IsActive = null)
    : IRequest<Result<PagedResult<PartDto>>>;

public class GetPartsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetPartsQuery, Result<PagedResult<PartDto>>>
{
    public async Task<Result<PagedResult<PartDto>>> Handle(GetPartsQuery req, CancellationToken ct)
    {
        var q = db.Parts.AsQueryable();
        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(p => p.PartNumber.Contains(req.Search) || p.Description.Contains(req.Search));
        if (req.IsActive.HasValue) q = q.Where(p => p.IsActive == req.IsActive.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .Select(p => PartDto.From(p)).ToListAsync(ct);

        return Result.Ok(new PagedResult<PartDto>(items, req.Page, req.PageSize, total));
    }
}

public record GetPartByIdQuery(int Id) : IRequest<Result<PartDto>>;

public class GetPartByIdQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetPartByIdQuery, Result<PartDto>>
{
    public async Task<Result<PartDto>> Handle(GetPartByIdQuery req, CancellationToken ct)
    {
        var part = await db.Parts.FindAsync([req.Id], ct);
        return part is null ? Result.Fail($"Không tìm thấy Part ID {req.Id}.") : Result.Ok(PartDto.From(part));
    }
}

// ── Commands ──────────────────────────────────────────────────

public record CreatePartCommand(string PartNumber, string Description, string? Revision, int? RequesterId)
    : IRequest<Result<PartDto>>;

public class CreatePartCommandValidator : AbstractValidator<CreatePartCommand>
{
    public CreatePartCommandValidator()
    {
        RuleFor(x => x.PartNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Revision).MaximumLength(10).When(x => x.Revision is not null);
    }
}

public class CreatePartCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreatePartCommand, Result<PartDto>>
{
    public async Task<Result<PartDto>> Handle(CreatePartCommand req, CancellationToken ct)
    {
        if (await db.Parts.AnyAsync(p => p.PartNumber == req.PartNumber && p.Revision == req.Revision, ct))
            return Result.Fail($"Part '{req.PartNumber}' rev '{req.Revision}' đã tồn tại.");

        var part = new Part
        {
            PartNumber = req.PartNumber,
            Description = req.Description,
            Revision = req.Revision,
            CreatedBy = req.RequesterId,
            UpdatedBy = req.RequesterId
        };
        db.Parts.Add(part);
        await db.SaveChangesAsync(ct);
        return Result.Ok(PartDto.From(part));
    }
}

public record UpdatePartCommand(int Id, string Description, string? Revision, string? RoutingRevision, bool IsActive, int? RequesterId)
    : IRequest<Result<PartDto>>;

public class UpdatePartCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdatePartCommand, Result<PartDto>>
{
    public async Task<Result<PartDto>> Handle(UpdatePartCommand req, CancellationToken ct)
    {
        var part = await db.Parts.FindAsync([req.Id], ct);
        if (part is null) return Result.Fail($"Không tìm thấy Part ID {req.Id}.");

        part.Description = req.Description;
        part.Revision = req.Revision;
        part.RoutingRevision = req.RoutingRevision;
        part.IsActive = req.IsActive;
        part.UpdatedAt = DateTimeOffset.UtcNow;
        part.UpdatedBy = req.RequesterId;
        await db.SaveChangesAsync(ct);
        return Result.Ok(PartDto.From(part));
    }
}
