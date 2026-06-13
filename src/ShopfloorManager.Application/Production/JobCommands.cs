using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;
using ShopfloorManager.Shared.Pagination;

namespace ShopfloorManager.Application.Production;

public record JobDto(
    int Id, string JobNumber,
    int PartId, int PartRevId, string PartNumber, string RevCode,
    int RoutingRevId, string RoutingRevCode,
    int? RunQty, int CompletedCount, DateOnly? ShipBy, bool IsComplete,
    DateTimeOffset CreatedAt);

public record JobDetailDto(
    int Id, string JobNumber,
    int PartId, int PartRevId, string PartNumber, string PartDescription, string RevCode,
    int RoutingRevId, string RoutingRevCode,
    int? RunQty, DateOnly? ShipBy, bool IsComplete, DateTimeOffset CreatedAt,
    IReadOnlyList<PartOpDto> Operations,
    IReadOnlyList<ProductDto> Products);

/// <summary>SessionStatus: "none" | "claimed" | "inprogress" — derive thêm "complete" từ IsComplete ở UI.</summary>
public record ProductDto(int Id, string SerialNumber, int JobId, bool IsComplete, int? SortOrder, string SessionStatus, string? ClaimedByName);

public record JobProgressDto(int TotalDim, int CompleteDim, int PassDim, int FailDim);

/// <summary>Map Product → ProductDto kèm trạng thái ProductionSession đang mở (claimed/inprogress).</summary>
public static class ProductDtoMapper
{
    public static async Task<List<ProductDto>> MapAsync(IShopfloorDbContext db, IReadOnlyList<Product> products, CancellationToken ct)
    {
        var productIds = products.Select(p => p.Id).ToList();
        var openSessions = await db.ProductionSessions
            .Include(s => s.ClaimedByUser)
            .Where(s => productIds.Contains(s.ProductId) && s.Status == SessionStatus.Open)
            .ToListAsync(ct);
        var sessionMap = openSessions.ToDictionary(s => s.ProductId);

        return products
            .OrderBy(p => p.SortOrder ?? p.Id)
            .Select(p =>
            {
                var sessionStatus = "none";
                string? claimedByName = null;
                if (sessionMap.TryGetValue(p.Id, out var sess))
                {
                    sessionStatus = sess.StartedAt.HasValue ? "inprogress" : "claimed";
                    claimedByName = sess.ClaimedByUser?.Name;
                }
                return new ProductDto(p.Id, p.SerialNumber, p.JobId, p.IsComplete, p.SortOrder, sessionStatus, claimedByName);
            })
            .ToList();
    }
}

// ── Queries ───────────────────────────────────────────────────

public record GetJobsQuery(int Page = 1, int PageSize = 20, string? Search = null, int? PartRevId = null)
    : IRequest<Result<PagedResult<JobDto>>>;

public class GetJobsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetJobsQuery, Result<PagedResult<JobDto>>>
{
    public async Task<Result<PagedResult<JobDto>>> Handle(GetJobsQuery req, CancellationToken ct)
    {
        var q = db.Jobs
            .Include(j => j.PartRev).ThenInclude(r => r.Part)
            .Include(j => j.RoutingRev)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(j => j.JobNumber.Contains(req.Search)
                           || j.PartRev.Part.PartNumber.Contains(req.Search));
        if (req.PartRevId.HasValue)
            q = q.Where(j => j.PartRevId == req.PartRevId.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(j => j.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .Select(j => new JobDto(j.Id, j.JobNumber,
                j.PartRev.PartId, j.PartRevId, j.PartRev.Part.PartNumber, j.PartRev.RevCode,
                j.RoutingRevId, j.RoutingRev.RevCode,
                j.RunQty, j.Products.Count(p => p.IsComplete),
                j.ShipBy, j.IsComplete, j.CreatedAt))
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
            .Include(j => j.PartRev).ThenInclude(r => r.Part)
            .Include(j => j.RoutingRev)
            .Include(j => j.Products)
            .FirstOrDefaultAsync(j => j.Id == req.Id, ct);

        if (job is null) return Result.Fail($"Không tìm thấy Job ID {req.Id}.");

        // Template OPs từ RoutingRev (snapshot)
        var templateOps = await db.PartOps
            .Include(o => o.OpType)
            .Where(o => o.RoutingRevId == job.RoutingRevId && o.IsVisible)
            .ToListAsync(ct);

        // ForJobOnly OPs của job này
        var jobOps = await db.PartOps
            .Include(o => o.OpType)
            .Where(o => o.JobId == job.Id && o.ForJobOnly && o.IsVisible)
            .ToListAsync(ct);

        var ops = templateOps.Concat(jobOps)
            .OrderBy(o => o.OpNumberSort ?? 0)
            .Select(o => new PartOpDto(o.Id, o.RoutingRevId, o.JobId, o.ForJobOnly,
                o.OpNumber, o.OpNumberSort, o.OpTypeId, o.OpType?.Name,
                o.Description, o.Note, o.SetupTime, o.ProdTime, o.IsVisible, o.IsComplete, 0, 0))
            .ToList();

        var products = await ProductDtoMapper.MapAsync(db, job.Products.ToList(), ct);

        return Result.Ok(new JobDetailDto(
            job.Id, job.JobNumber,
            job.PartRev.PartId, job.PartRevId, job.PartRev.Part.PartNumber, job.PartRev.Part.Description, job.PartRev.RevCode,
            job.RoutingRevId, job.RoutingRev.RevCode,
            job.RunQty, job.ShipBy, job.IsComplete, job.CreatedAt,
            ops, products));
    }
}

// ── Job Progress ──────────────────────────────────────────────

public record GetJobProgressQuery(int Id) : IRequest<Result<JobProgressDto>>;

public class GetJobProgressQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetJobProgressQuery, Result<JobProgressDto>>
{
    public async Task<Result<JobProgressDto>> Handle(GetJobProgressQuery req, CancellationToken ct)
    {
        var job = await db.Jobs.FindAsync([req.Id], ct);
        if (job is null) return Result.Fail($"Không tìm thấy Job ID {req.Id}.");

        var productIds = await db.Products.Where(p => p.JobId == job.Id).Select(p => p.Id).ToListAsync(ct);

        var opIds = await db.PartOps
            .Where(o => (o.RoutingRevId == job.RoutingRevId && o.IsVisible)
                     || (o.JobId == job.Id && o.ForJobOnly && o.IsVisible))
            .Select(o => o.Id)
            .ToListAsync(ct);

        var dimIds = await db.Dimensions
            .Where(d => opIds.Contains(d.PartOpId))
            .Select(d => d.Id)
            .ToListAsync(ct);

        var totalDim = dimIds.Count * productIds.Count;

        var measures = await db.MeasureValues
            .Where(m => dimIds.Contains(m.DimensionId) && productIds.Contains(m.ProductId))
            .Select(m => new { m.DimensionId, m.ProductId, m.Result, m.MeasuredAt })
            .ToListAsync(ct);

        var latest = measures
            .GroupBy(m => new { m.DimensionId, m.ProductId })
            .Select(g => g.OrderByDescending(m => m.MeasuredAt).First())
            .ToList();

        var completeDim = latest.Count;
        var passDim = latest.Count(m => m.Result == MeasureResult.Pass);
        var failDim = latest.Count(m => m.Result == MeasureResult.Fail);

        return Result.Ok(new JobProgressDto(totalDim, completeDim, passDim, failDim));
    }
}

// ── Commands ──────────────────────────────────────────────────

public record CreateJobCommand(string JobNumber, int PartRevId, int RoutingRevId, int? RunQty, DateOnly? ShipBy, int? RequesterId)
    : IRequest<Result<JobDto>>;

public class CreateJobCommandValidator : AbstractValidator<CreateJobCommand>
{
    public CreateJobCommandValidator()
    {
        RuleFor(x => x.JobNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.PartRevId).GreaterThan(0);
        RuleFor(x => x.RoutingRevId).GreaterThan(0);
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

        var partRev = await db.PartRevs.Include(r => r.Part)
            .FirstOrDefaultAsync(r => r.Id == req.PartRevId, ct);
        if (partRev is null) return Result.Fail($"Không tìm thấy PartRev ID {req.PartRevId}.");

        var routingRev = await db.RoutingRevs.FindAsync([req.RoutingRevId], ct);
        if (routingRev is null) return Result.Fail($"Không tìm thấy RoutingRev ID {req.RoutingRevId}.");

        var job = new Job
        {
            JobNumber = req.JobNumber,
            PartRevId = req.PartRevId,
            RoutingRevId = req.RoutingRevId,
            RunQty = req.RunQty, ShipBy = req.ShipBy,
            CreatedBy = req.RequesterId
        };
        db.Jobs.Add(job);

        // Auto-create Products theo RunQty (theo 03_job_management.md rule 3.2)
        if (req.RunQty.HasValue && req.RunQty.Value > 0)
        {
            var products = Enumerable.Range(1, req.RunQty.Value).Select(i => new Product
            {
                Job = job,
                SerialNumber = i.ToString("D2"),
                SortOrder = i
            });
            db.Products.AddRange(products);
        }

        await db.SaveChangesAsync(ct);

        return Result.Ok(new JobDto(job.Id, job.JobNumber,
            partRev.PartId, partRev.Id, partRev.Part.PartNumber, partRev.RevCode,
            routingRev.Id, routingRev.RevCode,
            job.RunQty, 0, job.ShipBy, job.IsComplete, job.CreatedAt));
    }
}

public record GenerateProductsCommand(int JobId, int Quantity) : IRequest<Result<List<ProductDto>>>;

public class GenerateProductsCommandValidator : AbstractValidator<GenerateProductsCommand>
{
    public GenerateProductsCommandValidator()
    {
        RuleFor(x => x.JobId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 9999);
    }
}

public class GenerateProductsCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<GenerateProductsCommand, Result<List<ProductDto>>>
{
    public async Task<Result<List<ProductDto>>> Handle(GenerateProductsCommand req, CancellationToken ct)
    {
        var job = await db.Jobs.FindAsync([req.JobId], ct);
        if (job is null) return Result.Fail($"Không tìm thấy Job ID {req.JobId}.");

        var existing = await db.Products.CountAsync(p => p.JobId == req.JobId, ct);
        var products = Enumerable.Range(existing + 1, req.Quantity).Select(i => new Product
        {
            JobId = req.JobId,
            SerialNumber = i.ToString("D3"),
            SortOrder = i
        }).ToList();

        db.Products.AddRange(products);
        await db.SaveChangesAsync(ct);

        return Result.Ok(products
            .Select(p => new ProductDto(p.Id, p.SerialNumber, p.JobId, p.IsComplete, p.SortOrder, "none", null))
            .ToList());
    }
}
