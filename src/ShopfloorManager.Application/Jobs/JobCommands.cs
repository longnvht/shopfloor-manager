using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Shared.Pagination;

namespace ShopfloorManager.Application.Jobs;

// ── Queries ───────────────────────────────────────────────────

public record GetJobsQuery(int Page = 1, int PageSize = 20, string? Search = null, int? PartId = null)
    : IRequest<Result<PagedResult<JobDto>>>;

public class GetJobsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetJobsQuery, Result<PagedResult<JobDto>>>
{
    public async Task<Result<PagedResult<JobDto>>> Handle(GetJobsQuery req, CancellationToken ct)
    {
        var q = db.Jobs.Include(j => j.Part).AsQueryable();
        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(j => j.JobNumber.Contains(req.Search) || j.Part.PartNumber.Contains(req.Search));
        if (req.PartId.HasValue) q = q.Where(j => j.PartId == req.PartId.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(j => j.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .Select(j => new JobDto(j.Id, j.JobNumber, j.PartId, j.Part.PartNumber,
                j.Part.Description, j.Part.Revision, j.RunQty, j.ShipBy, j.CreatedAt))
            .ToListAsync(ct);

        return Result.Ok(new PagedResult<JobDto>(items, req.Page, req.PageSize, total));
    }
}

public record GetJobByIdQuery(int Id) : IRequest<Result<JobDetailDto>>;

public class GetJobByIdQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetJobByIdQuery, Result<JobDetailDto>>
{
    public async Task<Result<JobDetailDto>> Handle(GetJobByIdQuery req, CancellationToken ct)
    {
        var job = await db.Jobs
            .Include(j => j.Part)
            .Include(j => j.PartOps).ThenInclude(o => o.OpType)
            .Include(j => j.Products)
            .FirstOrDefaultAsync(j => j.Id == req.Id, ct);

        if (job is null) return Result.Fail($"Không tìm thấy Job ID {req.Id}.");

        var ops = job.PartOps
            .Where(o => o.IsVisible)
            .OrderBy(o => o.OpNumberSort ?? 0)
            .Select(o => new PartOpSummary(o.Id, o.OpNumber, o.OpType?.Name, o.Description, o.IsComplete))
            .ToList();

        var products = job.Products
            .OrderBy(p => p.SortOrder ?? p.Id)
            .Select(p => new ProductSummary(p.Id, p.SerialNumber, p.IsComplete))
            .ToList();

        return Result.Ok(new JobDetailDto(
            job.Id, job.JobNumber, job.PartId, job.Part.PartNumber,
            job.Part.Description, job.Part.Revision,
            job.RunQty, job.ShipBy, job.CreatedAt, ops, products));
    }
}

// ── Commands ──────────────────────────────────────────────────

public record CreateJobCommand(string JobNumber, int PartId, int? RunQty, DateOnly? ShipBy)
    : IRequest<Result<JobDto>>;

public class CreateJobCommandValidator : AbstractValidator<CreateJobCommand>
{
    public CreateJobCommandValidator()
    {
        RuleFor(x => x.JobNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.PartId).GreaterThan(0);
        RuleFor(x => x.RunQty).GreaterThan(0).When(x => x.RunQty.HasValue);
    }
}

public class CreateJobCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateJobCommand, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(CreateJobCommand req, CancellationToken ct)
    {
        if (await db.Jobs.AnyAsync(j => j.JobNumber == req.JobNumber, ct))
            return Result.Fail($"Job number '{req.JobNumber}' đã tồn tại.");

        var part = await db.Parts.FindAsync([req.PartId], ct);
        if (part is null) return Result.Fail($"Không tìm thấy Part ID {req.PartId}.");

        var job = new Job { JobNumber = req.JobNumber, PartId = req.PartId, RunQty = req.RunQty, ShipBy = req.ShipBy };
        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);

        return Result.Ok(new JobDto(job.Id, job.JobNumber, job.PartId,
            part.PartNumber, part.Description, part.Revision, job.RunQty, job.ShipBy, job.CreatedAt));
    }
}

public record UpdateJobCommand(int Id, int? RunQty, DateOnly? ShipBy) : IRequest<Result<JobDto>>;

public class UpdateJobCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdateJobCommand, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(UpdateJobCommand req, CancellationToken ct)
    {
        var job = await db.Jobs.Include(j => j.Part).FirstOrDefaultAsync(j => j.Id == req.Id, ct);
        if (job is null) return Result.Fail($"Không tìm thấy Job ID {req.Id}.");

        job.RunQty = req.RunQty;
        job.ShipBy = req.ShipBy;
        await db.SaveChangesAsync(ct);

        return Result.Ok(new JobDto(job.Id, job.JobNumber, job.PartId,
            job.Part.PartNumber, job.Part.Description, job.Part.Revision,
            job.RunQty, job.ShipBy, job.CreatedAt));
    }
}
