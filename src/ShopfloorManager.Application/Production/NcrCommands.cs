using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;
using ShopfloorManager.Shared.Pagination;

namespace ShopfloorManager.Application.Production;

// ── DTOs ─────────────────────────────────────────────────────

public record NcrDto(
    long Id, string NcrNumber,
    int JobId, string JobNumber,
    int? ProductId, string? SerialNumber,
    int? PartOpId, string? OpNumber,
    int? ReasonId, string? ReasonName,
    int? DepartmentId, string? MachineCode,
    string Description, string Status,
    string RaisedBy, DateTimeOffset RaisedAt,
    string? ClosedBy, DateTimeOffset? ClosedAt);

public record NcrDetailDto(NcrDto Ncr, IReadOnlyList<NcrLogDto> Logs);

public record NcrLogDto(int Id, string Action, string? Note, string ActionBy, DateTimeOffset ActionAt);

// ── Queries ───────────────────────────────────────────────────

public record GetNcrsQuery(
    int Page = 1, int PageSize = 20,
    string? Status = null, int? JobId = null)
    : IRequest<Result<PagedResult<NcrDto>>>;

public class GetNcrsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetNcrsQuery, Result<PagedResult<NcrDto>>>
{
    public async Task<Result<PagedResult<NcrDto>>> Handle(GetNcrsQuery req, CancellationToken ct)
    {
        var q = db.Ncrs
            .Include(n => n.Job)
            .Include(n => n.Product)
            .Include(n => n.PartOp)
            .Include(n => n.Reason)
            .Include(n => n.Raiser)
            .Include(n => n.Closer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Status) && Enum.TryParse<NcrStatus>(req.Status, true, out var s))
            q = q.Where(n => n.Status == s);
        if (req.JobId.HasValue)
            q = q.Where(n => n.JobId == req.JobId.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(n => n.RaisedAt)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .Select(n => new NcrDto(
                n.Id, n.NcrNumber,
                n.JobId, n.Job.JobNumber,
                n.ProductId, n.Product != null ? n.Product.SerialNumber : null,
                n.PartOpId, n.PartOp != null ? n.PartOp.OpNumber : null,
                n.ReasonId, n.Reason != null ? n.Reason.Name : null,
                n.DepartmentId, n.MachineCode,
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
            .Include(n => n.Job)
            .Include(n => n.Product)
            .Include(n => n.PartOp)
            .Include(n => n.Reason)
            .Include(n => n.Raiser)
            .Include(n => n.Closer)
            .Include(n => n.Logs).ThenInclude(l => l.Actor)
            .FirstOrDefaultAsync(n => n.Id == req.Id, ct);

        if (ncr is null) return Result.Fail($"Không tìm thấy NCR ID {req.Id}.");

        var dto = new NcrDto(
            ncr.Id, ncr.NcrNumber,
            ncr.JobId, ncr.Job.JobNumber,
            ncr.ProductId, ncr.Product?.SerialNumber,
            ncr.PartOpId, ncr.PartOp?.OpNumber,
            ncr.ReasonId, ncr.Reason?.Name,
            ncr.DepartmentId, ncr.MachineCode,
            ncr.Description, ncr.Status.ToString(),
            ncr.Raiser.Name, ncr.RaisedAt,
            ncr.Closer?.Name, ncr.ClosedAt);

        var logs = ncr.Logs
            .OrderBy(l => l.ActionAt)
            .Select(l => new NcrLogDto(l.Id, l.Action.ToString(), l.Note, l.Actor.Name, l.ActionAt))
            .ToList();

        return Result.Ok(new NcrDetailDto(dto, logs));
    }
}

// ── Commands ──────────────────────────────────────────────────

public record CreateNcrCommand(
    int JobId, int? ProductId, int? PartOpId,
    int? ReasonId, int? DepartmentId, string? MachineCode,
    string Description, int RequesterId)
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

        // NCR-{YY}-{NNNN} — 2 chữ số năm, reset mỗi năm (theo 07_ncr.md)
        var now = DateTimeOffset.UtcNow;
        var yearCode = now.Year % 100;
        var sequence = await db.Ncrs.CountAsync(n => n.YearCode == yearCode, ct) + 1;
        var number = $"NCR-{yearCode:D2}-{sequence:D4}";

        var ncr = new Ncr
        {
            NcrNumber = number, YearCode = yearCode, Sequence = sequence,
            JobId = req.JobId, ProductId = req.ProductId, PartOpId = req.PartOpId,
            ReasonId = req.ReasonId, DepartmentId = req.DepartmentId, MachineCode = req.MachineCode,
            Description = req.Description, RaisedBy = req.RequesterId
        };
        db.Ncrs.Add(ncr);

        db.NcrLogs.Add(new NcrLog
        {
            Ncr = ncr, Action = NcrAction.Pending,
            Note = "NCR được tạo.", ActionBy = req.RequesterId
        });

        await db.SaveChangesAsync(ct);

        var raiser = await db.Users.FindAsync([req.RequesterId], ct);
        var reason = ncr.ReasonId.HasValue
            ? await db.NcrReasons.FindAsync([ncr.ReasonId.Value], ct) : null;
        return Result.Ok(new NcrDto(
            ncr.Id, ncr.NcrNumber,
            ncr.JobId, job.JobNumber,
            ncr.ProductId, null, ncr.PartOpId, null,
            ncr.ReasonId, reason?.Name,
            ncr.DepartmentId, ncr.MachineCode,
            ncr.Description, ncr.Status.ToString(),
            raiser?.Name ?? "", ncr.RaisedAt, null, null));
    }
}

public record AddNcrActionCommand(long NcrId, string Action, string? Note, int RequesterId)
    : IRequest<Result<NcrLogDto>>;

public class AddNcrActionCommandValidator : AbstractValidator<AddNcrActionCommand>
{
    public AddNcrActionCommandValidator()
    {
        RuleFor(x => x.Action).NotEmpty()
            .Must(a => Enum.TryParse<NcrAction>(a, true, out _))
            .WithMessage("Action không hợp lệ. Dùng: Approve, Rework, Reject.");
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public class AddNcrActionCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<AddNcrActionCommand, Result<NcrLogDto>>
{
    public async Task<Result<NcrLogDto>> Handle(AddNcrActionCommand req, CancellationToken ct)
    {
        var ncr = await db.Ncrs.FindAsync([req.NcrId], ct);
        if (ncr is null) return Result.Fail($"Không tìm thấy NCR ID {req.NcrId}.");
        if (ncr.Status == NcrStatus.Closed) return Result.Fail("NCR đã đóng.");

        Enum.TryParse<NcrAction>(req.Action, true, out var action);

        if (action is NcrAction.Approve or NcrAction.Reject)
        {
            ncr.Status = NcrStatus.Closed;
            ncr.ClosedBy = req.RequesterId;
            ncr.ClosedAt = DateTimeOffset.UtcNow;
        }

        var log = new NcrLog
        {
            NcrId = req.NcrId, Action = action,
            Note = req.Note, ActionBy = req.RequesterId
        };
        db.NcrLogs.Add(log);
        await db.SaveChangesAsync(ct);

        var actor = await db.Users.FindAsync([req.RequesterId], ct);
        return Result.Ok(new NcrLogDto(log.Id, log.Action.ToString(), log.Note, actor?.Name ?? "", log.ActionAt));
    }
}
