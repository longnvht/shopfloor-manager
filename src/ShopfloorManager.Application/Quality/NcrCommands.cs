using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;
using ShopfloorManager.Shared.Pagination;

namespace ShopfloorManager.Application.Quality;

public record NcrDto(
    long Id, string NcrNumber, int JobId, string JobNumber,
    string Description, string Status,
    string RaisedBy, DateTimeOffset RaisedAt,
    string? ClosedBy, DateTimeOffset? ClosedAt);

public record NcrDetailDto(NcrDto Ncr, IReadOnlyList<NcrLogDto> Logs);

public record NcrLogDto(int Id, string Action, string? Note, string ActionBy, DateTimeOffset ActionAt);

// ── Queries ───────────────────────────────────────────────────

public record GetNcrsQuery(int Page = 1, int PageSize = 20, string? Status = null, int? JobId = null)
    : IRequest<Result<PagedResult<NcrDto>>>;

public class GetNcrsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetNcrsQuery, Result<PagedResult<NcrDto>>>
{
    public async Task<Result<PagedResult<NcrDto>>> Handle(GetNcrsQuery req, CancellationToken ct)
    {
        var q = db.Ncrs
            .Include(n => n.Job).Include(n => n.Raiser).Include(n => n.Closer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Status) && Enum.TryParse<NcrStatus>(req.Status, out var status))
            q = q.Where(n => n.Status == status);
        if (req.JobId.HasValue) q = q.Where(n => n.JobId == req.JobId.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(n => n.RaisedAt)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .Select(n => new NcrDto(n.Id, n.NcrNumber, n.JobId, n.Job.JobNumber,
                n.Description, n.Status.ToString(),
                n.Raiser.Name, n.RaisedAt,
                n.Closer != null ? n.Closer.Name : null, n.ClosedAt))
            .ToListAsync(ct);

        return Result.Ok(new PagedResult<NcrDto>(items, req.Page, req.PageSize, total));
    }
}

public record GetNcrByIdQuery(long Id) : IRequest<Result<NcrDetailDto>>;

public class GetNcrByIdQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetNcrByIdQuery, Result<NcrDetailDto>>
{
    public async Task<Result<NcrDetailDto>> Handle(GetNcrByIdQuery req, CancellationToken ct)
    {
        var ncr = await db.Ncrs
            .Include(n => n.Job).Include(n => n.Raiser).Include(n => n.Closer)
            .Include(n => n.Logs).ThenInclude(l => l.Actor)
            .FirstOrDefaultAsync(n => n.Id == req.Id, ct);

        if (ncr is null) return Result.Fail($"Không tìm thấy NCR ID {req.Id}.");

        var dto = new NcrDto(ncr.Id, ncr.NcrNumber, ncr.JobId, ncr.Job.JobNumber,
            ncr.Description, ncr.Status.ToString(),
            ncr.Raiser.Name, ncr.RaisedAt,
            ncr.Closer?.Name, ncr.ClosedAt);

        var logs = ncr.Logs.OrderBy(l => l.ActionAt)
            .Select(l => new NcrLogDto(l.Id, l.Action.ToString(), l.Note, l.Actor.Name, l.ActionAt))
            .ToList();

        return Result.Ok(new NcrDetailDto(dto, logs));
    }
}

// ── Commands ──────────────────────────────────────────────────

public record CreateNcrCommand(int JobId, int? ProductId, int? PartOpId, string Description, int RequesterId)
    : IRequest<Result<NcrDto>>;

public class CreateNcrCommandValidator : AbstractValidator<CreateNcrCommand>
{
    public CreateNcrCommandValidator()
    {
        RuleFor(x => x.JobId).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
    }
}

public class CreateNcrCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateNcrCommand, Result<NcrDto>>
{
    public async Task<Result<NcrDto>> Handle(CreateNcrCommand req, CancellationToken ct)
    {
        var job = await db.Jobs.FindAsync([req.JobId], ct);
        if (job is null) return Result.Fail($"Không tìm thấy Job ID {req.JobId}.");

        // NCR-2026-0001 format
        var year = DateTimeOffset.UtcNow.Year;
        var count = await db.Ncrs.CountAsync(ct) + 1;
        var number = $"NCR-{year}-{count:D4}";

        var ncr = new Ncr
        {
            NcrNumber = number,
            JobId = req.JobId,
            ProductId = req.ProductId,
            PartOpId = req.PartOpId,
            Description = req.Description,
            RaisedBy = req.RequesterId
        };
        db.Ncrs.Add(ncr);

        db.NcrLogs.Add(new NcrLog
        {
            Ncr = ncr, Action = NcrAction.Pending, Note = "NCR được tạo.",
            ActionBy = req.RequesterId
        });

        await db.SaveChangesAsync(ct);

        var raiser = await db.Users.FindAsync([req.RequesterId], ct);
        return Result.Ok(new NcrDto(ncr.Id, ncr.NcrNumber, ncr.JobId, job.JobNumber,
            ncr.Description, ncr.Status.ToString(),
            raiser?.Name ?? "", ncr.RaisedAt, null, null));
    }
}

public record AddNcrActionCommand(long NcrId, string Action, string? Note, int RequesterId)
    : IRequest<Result<NcrLogDto>>;

public class AddNcrActionCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<AddNcrActionCommand, Result<NcrLogDto>>
{
    public async Task<Result<NcrLogDto>> Handle(AddNcrActionCommand req, CancellationToken ct)
    {
        var ncr = await db.Ncrs.FindAsync([req.NcrId], ct);
        if (ncr is null) return Result.Fail($"Không tìm thấy NCR ID {req.NcrId}.");
        if (ncr.Status == NcrStatus.Closed) return Result.Fail("NCR đã đóng.");

        if (!Enum.TryParse<NcrAction>(req.Action, true, out var action))
            return Result.Fail($"Action không hợp lệ: {req.Action}. Dùng: Approve, Rework, Reject.");

        if (action == NcrAction.Approve || action == NcrAction.Reject)
        {
            ncr.Status = NcrStatus.Closed;
            ncr.ClosedBy = req.RequesterId;
            ncr.ClosedAt = DateTimeOffset.UtcNow;
        }

        var log = new NcrLog { NcrId = req.NcrId, Action = action, Note = req.Note, ActionBy = req.RequesterId };
        db.NcrLogs.Add(log);
        await db.SaveChangesAsync(ct);

        var actor = await db.Users.FindAsync([req.RequesterId], ct);
        return Result.Ok(new NcrLogDto(log.Id, log.Action.ToString(), log.Note, actor?.Name ?? "", log.ActionAt));
    }
}
