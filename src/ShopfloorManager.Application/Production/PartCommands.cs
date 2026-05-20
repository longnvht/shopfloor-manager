using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Shared.Pagination;

namespace ShopfloorManager.Application.Production;

// ── DTOs ─────────────────────────────────────────────────────

public record PartDto(int Id, string PartNumber, string Description, DateTimeOffset CreatedAt);

public record PartRevDto(
    int Id, int PartId, string PartNumber, string RevCode, string? Description,
    bool IsActive, bool IsReleased, DateTimeOffset CreatedAt);

public record RoutingDto(int Id, int PartRevId, string Name, string? Description, bool IsActive);

public record RoutingRevDto(
    int Id, int RoutingId, string RevCode, string? ChangeNote,
    bool IsActive, bool IsReleased, DateTimeOffset CreatedAt,
    int OpCount);

// ── Parts ─────────────────────────────────────────────────────

public record GetPartsQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<Result<PagedResult<PartDto>>>;

public class GetPartsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetPartsQuery, Result<PagedResult<PartDto>>>
{
    public async Task<Result<PagedResult<PartDto>>> Handle(GetPartsQuery req, CancellationToken ct)
    {
        var q = db.Parts.AsQueryable();
        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(p => p.PartNumber.Contains(req.Search) || p.Description.Contains(req.Search));

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(p => p.PartNumber)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .Select(p => new PartDto(p.Id, p.PartNumber, p.Description, p.CreatedAt))
            .ToListAsync(ct);

        return Result.Ok(new PagedResult<PartDto>(items, req.Page, req.PageSize, total));
    }
}

public record CreatePartCommand(string PartNumber, string Description, string RevCode, int? RequesterId)
    : IRequest<Result<PartRevDto>>;

public class CreatePartCommandValidator : AbstractValidator<CreatePartCommand>
{
    public CreatePartCommandValidator()
    {
        RuleFor(x => x.PartNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
        RuleFor(x => x.RevCode).NotEmpty().MaximumLength(10);
    }
}

public class CreatePartCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreatePartCommand, Result<PartRevDto>>
{
    public async Task<Result<PartRevDto>> Handle(CreatePartCommand req, CancellationToken ct)
    {
        if (await db.Parts.AnyAsync(p => p.PartNumber == req.PartNumber, ct))
            return Result.Fail($"PartNumber '{req.PartNumber}' đã tồn tại.");

        var part = new Part { PartNumber = req.PartNumber, Description = req.Description, CreatedBy = req.RequesterId };
        db.Parts.Add(part);

        var rev = new PartRev { Part = part, RevCode = req.RevCode, IsActive = true, CreatedBy = req.RequesterId };
        db.PartRevs.Add(rev);

        // Tạo Routing mặc định + RoutingRev R1
        var routing = new Routing { PartRev = rev, Name = "Standard", CreatedBy = req.RequesterId };
        db.Routings.Add(routing);

        var routingRev = new RoutingRev { Routing = routing, RevCode = "R1", IsActive = true, CreatedBy = req.RequesterId };
        db.RoutingRevs.Add(routingRev);

        await db.SaveChangesAsync(ct);

        return Result.Ok(new PartRevDto(rev.Id, part.Id, part.PartNumber, rev.RevCode,
            rev.Description, rev.IsActive, rev.IsReleased, rev.CreatedAt));
    }
}

// ── PartRevs ──────────────────────────────────────────────────

public record GetPartRevsQuery(int PartId) : IRequest<Result<List<PartRevDto>>>;

public class GetPartRevsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetPartRevsQuery, Result<List<PartRevDto>>>
{
    public async Task<Result<List<PartRevDto>>> Handle(GetPartRevsQuery req, CancellationToken ct)
    {
        var items = await db.PartRevs
            .Include(r => r.Part)
            .Where(r => r.PartId == req.PartId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new PartRevDto(r.Id, r.PartId, r.Part.PartNumber, r.RevCode,
                r.Description, r.IsActive, r.IsReleased, r.CreatedAt))
            .ToListAsync(ct);
        return Result.Ok(items);
    }
}

public record AddPartRevCommand(int PartId, string RevCode, string? Description, int? RequesterId)
    : IRequest<Result<PartRevDto>>;

public class AddPartRevCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<AddPartRevCommand, Result<PartRevDto>>
{
    public async Task<Result<PartRevDto>> Handle(AddPartRevCommand req, CancellationToken ct)
    {
        var part = await db.Parts.FindAsync([req.PartId], ct);
        if (part is null) return Result.Fail($"Không tìm thấy Part ID {req.PartId}.");

        if (await db.PartRevs.AnyAsync(r => r.PartId == req.PartId && r.RevCode == req.RevCode, ct))
            return Result.Fail($"Rev '{req.RevCode}' đã tồn tại cho Part này.");

        var rev = new PartRev
        {
            PartId = req.PartId, RevCode = req.RevCode,
            Description = req.Description, IsActive = false, CreatedBy = req.RequesterId
        };
        db.PartRevs.Add(rev);

        // Tạo Routing + RoutingRev R1 mặc định cho Rev mới
        var routing = new Routing { PartRev = rev, Name = "Standard", CreatedBy = req.RequesterId };
        db.Routings.Add(routing);
        db.RoutingRevs.Add(new RoutingRev { Routing = routing, RevCode = "R1", IsActive = true, CreatedBy = req.RequesterId });

        await db.SaveChangesAsync(ct);

        return Result.Ok(new PartRevDto(rev.Id, part.Id, part.PartNumber, rev.RevCode,
            rev.Description, rev.IsActive, rev.IsReleased, rev.CreatedAt));
    }
}

// ── RoutingRevs ───────────────────────────────────────────────

public record GetRoutingRevsQuery(int RoutingId) : IRequest<Result<List<RoutingRevDto>>>;

public class GetRoutingRevsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetRoutingRevsQuery, Result<List<RoutingRevDto>>>
{
    public async Task<Result<List<RoutingRevDto>>> Handle(GetRoutingRevsQuery req, CancellationToken ct)
    {
        var items = await db.RoutingRevs
            .Include(r => r.PartOps)
            .Where(r => r.RoutingId == req.RoutingId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RoutingRevDto(r.Id, r.RoutingId, r.RevCode, r.ChangeNote,
                r.IsActive, r.IsReleased, r.CreatedAt, r.PartOps.Count))
            .ToListAsync(ct);
        return Result.Ok(items);
    }
}

/// <summary>
/// Tạo RoutingRev mới — copy toàn bộ PartOps từ RoutingRev đang active.
/// </summary>
public record AddRoutingRevCommand(int RoutingId, string RevCode, string? ChangeNote, int? RequesterId)
    : IRequest<Result<RoutingRevDto>>;

public class AddRoutingRevCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<AddRoutingRevCommand, Result<RoutingRevDto>>
{
    public async Task<Result<RoutingRevDto>> Handle(AddRoutingRevCommand req, CancellationToken ct)
    {
        if (await db.RoutingRevs.AnyAsync(r => r.RoutingId == req.RoutingId && r.RevCode == req.RevCode, ct))
            return Result.Fail($"RoutingRev '{req.RevCode}' đã tồn tại.");

        // Lấy RoutingRev đang active để copy OPs
        var sourceRev = await db.RoutingRevs
            .Include(r => r.PartOps)
            .FirstOrDefaultAsync(r => r.RoutingId == req.RoutingId && r.IsActive, ct);

        var newRev = new RoutingRev
        {
            RoutingId = req.RoutingId, RevCode = req.RevCode,
            ChangeNote = req.ChangeNote, IsActive = false, CreatedBy = req.RequesterId
        };
        db.RoutingRevs.Add(newRev);

        // Copy PartOps từ rev cũ
        if (sourceRev is not null)
        {
            foreach (var op in sourceRev.PartOps)
            {
                db.PartOps.Add(new PartOp
                {
                    RoutingRev = newRev,
                    OpNumber = op.OpNumber, OpNumberSort = op.OpNumberSort,
                    OpTypeId = op.OpTypeId, Description = op.Description,
                    Note = op.Note, SetupTime = op.SetupTime, ProdTime = op.ProdTime,
                    IsVisible = op.IsVisible, CreatedBy = req.RequesterId
                });
            }
        }

        await db.SaveChangesAsync(ct);

        return Result.Ok(new RoutingRevDto(newRev.Id, newRev.RoutingId, newRev.RevCode,
            newRev.ChangeNote, newRev.IsActive, newRev.IsReleased, newRev.CreatedAt,
            sourceRev?.PartOps.Count ?? 0));
    }
}
