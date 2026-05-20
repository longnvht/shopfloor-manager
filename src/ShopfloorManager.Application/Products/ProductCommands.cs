using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Products;

public record ProductDto(int Id, string SerialNumber, int JobId, bool IsComplete, int? SortOrder);

// ── Queries ───────────────────────────────────────────────────

public record GetProductsQuery(int JobId) : IRequest<Result<List<ProductDto>>>;

public class GetProductsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetProductsQuery, Result<List<ProductDto>>>
{
    public async Task<Result<List<ProductDto>>> Handle(GetProductsQuery req, CancellationToken ct)
    {
        var items = await db.Products
            .Where(p => p.JobId == req.JobId)
            .OrderBy(p => p.SortOrder ?? p.Id)
            .Select(p => new ProductDto(p.Id, p.SerialNumber, p.JobId, p.IsComplete, p.SortOrder))
            .ToListAsync(ct);
        return Result.Ok(items);
    }
}

// ── Commands ──────────────────────────────────────────────────

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
        var products = Enumerable.Range(existing + 1, req.Quantity)
            .Select(i => new Product
            {
                JobId = req.JobId,
                SerialNumber = i.ToString("D3"),
                SortOrder = i
            }).ToList();

        db.Products.AddRange(products);
        await db.SaveChangesAsync(ct);

        return Result.Ok(products
            .Select(p => new ProductDto(p.Id, p.SerialNumber, p.JobId, p.IsComplete, p.SortOrder))
            .ToList());
    }
}
