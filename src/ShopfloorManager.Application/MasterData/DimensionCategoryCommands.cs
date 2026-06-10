using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.MasterData;

public record DimensionCategoryDto(int Id, string Code, string Name, string? Description, bool IsActive);

// ── Queries ───────────────────────────────────────────────────

public record GetDimensionCategoriesQuery(bool ActiveOnly = false) : IRequest<Result<List<DimensionCategoryDto>>>;

public class GetDimensionCategoriesQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetDimensionCategoriesQuery, Result<List<DimensionCategoryDto>>>
{
    public async Task<Result<List<DimensionCategoryDto>>> Handle(GetDimensionCategoriesQuery req, CancellationToken ct)
    {
        var query = db.DimensionCategories.AsQueryable();
        if (req.ActiveOnly)
            query = query.Where(c => c.IsActive);

        var items = await query.OrderBy(c => c.Code)
            .Select(c => new DimensionCategoryDto(c.Id, c.Code, c.Name, c.Description, c.IsActive))
            .ToListAsync(ct);
        return Result.Ok(items);
    }
}

// ── Commands ──────────────────────────────────────────────────

public record CreateDimensionCategoryCommand(string Code, string Name, string? Description, bool IsActive) : IRequest<Result<DimensionCategoryDto>>;

public class CreateDimensionCategoryCommandValidator : AbstractValidator<CreateDimensionCategoryCommand>
{
    public CreateDimensionCategoryCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateDimensionCategoryCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateDimensionCategoryCommand, Result<DimensionCategoryDto>>
{
    public async Task<Result<DimensionCategoryDto>> Handle(CreateDimensionCategoryCommand req, CancellationToken ct)
    {
        if (await db.DimensionCategories.AnyAsync(c => c.Code == req.Code, ct))
            return Result.Fail($"Dimension Category '{req.Code}' đã tồn tại.");

        var cat = new DimensionCategory { Code = req.Code, Name = req.Name, Description = req.Description, IsActive = req.IsActive };
        db.DimensionCategories.Add(cat);
        await db.SaveChangesAsync(ct);
        return Result.Ok(new DimensionCategoryDto(cat.Id, cat.Code, cat.Name, cat.Description, cat.IsActive));
    }
}

public record UpdateDimensionCategoryCommand(int Id, string Code, string Name, string? Description, bool IsActive) : IRequest<Result<DimensionCategoryDto>>;

public class UpdateDimensionCategoryCommandValidator : AbstractValidator<UpdateDimensionCategoryCommand>
{
    public UpdateDimensionCategoryCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpdateDimensionCategoryCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdateDimensionCategoryCommand, Result<DimensionCategoryDto>>
{
    public async Task<Result<DimensionCategoryDto>> Handle(UpdateDimensionCategoryCommand req, CancellationToken ct)
    {
        var cat = await db.DimensionCategories.FindAsync([req.Id], ct);
        if (cat is null) return Result.Fail($"Không tìm thấy Dimension Category ID {req.Id}.");

        if (await db.DimensionCategories.AnyAsync(c => c.Code == req.Code && c.Id != req.Id, ct))
            return Result.Fail($"Dimension Category '{req.Code}' đã tồn tại.");

        cat.Code = req.Code;
        cat.Name = req.Name;
        cat.Description = req.Description;
        cat.IsActive = req.IsActive;
        await db.SaveChangesAsync(ct);
        return Result.Ok(new DimensionCategoryDto(cat.Id, cat.Code, cat.Name, cat.Description, cat.IsActive));
    }
}
