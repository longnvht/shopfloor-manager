using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.MasterData;

/// <summary>Nhóm phương pháp đo rộng (LIN/ANG/THD/GEO/SFC) — gắn trên GageType, dùng để filter gage
/// theo dimension khi không có GageTypeId cụ thể. Xem 06_dimensions_fai.md §3.6.</summary>
public record GageCategoryDto(int Id, string Code, string Name, string? Description, bool IsActive);

// ── Queries ───────────────────────────────────────────────────

public record GetGageCategoriesQuery(bool ActiveOnly = false) : IRequest<Result<List<GageCategoryDto>>>;

public class GetGageCategoriesQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetGageCategoriesQuery, Result<List<GageCategoryDto>>>
{
    public async Task<Result<List<GageCategoryDto>>> Handle(GetGageCategoriesQuery req, CancellationToken ct)
    {
        var query = db.GageCategories.AsQueryable();
        if (req.ActiveOnly)
            query = query.Where(c => c.IsActive);

        var items = await query.OrderBy(c => c.Code)
            .Select(c => new GageCategoryDto(c.Id, c.Code, c.Name, c.Description, c.IsActive))
            .ToListAsync(ct);
        return Result.Ok(items);
    }
}

// ── Commands ──────────────────────────────────────────────────

public record CreateGageCategoryCommand(string Code, string Name, string? Description, bool IsActive) : IRequest<Result<GageCategoryDto>>;

public class CreateGageCategoryCommandValidator : AbstractValidator<CreateGageCategoryCommand>
{
    public CreateGageCategoryCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateGageCategoryCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateGageCategoryCommand, Result<GageCategoryDto>>
{
    public async Task<Result<GageCategoryDto>> Handle(CreateGageCategoryCommand req, CancellationToken ct)
    {
        if (await db.GageCategories.AnyAsync(c => c.Code == req.Code, ct))
            return Result.Fail($"Gage Category '{req.Code}' đã tồn tại.");

        var cat = new GageCategory { Code = req.Code, Name = req.Name, Description = req.Description, IsActive = req.IsActive };
        db.GageCategories.Add(cat);
        await db.SaveChangesAsync(ct);
        return Result.Ok(new GageCategoryDto(cat.Id, cat.Code, cat.Name, cat.Description, cat.IsActive));
    }
}

public record UpdateGageCategoryCommand(int Id, string Code, string Name, string? Description, bool IsActive) : IRequest<Result<GageCategoryDto>>;

public class UpdateGageCategoryCommandValidator : AbstractValidator<UpdateGageCategoryCommand>
{
    public UpdateGageCategoryCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpdateGageCategoryCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdateGageCategoryCommand, Result<GageCategoryDto>>
{
    public async Task<Result<GageCategoryDto>> Handle(UpdateGageCategoryCommand req, CancellationToken ct)
    {
        var cat = await db.GageCategories.FindAsync([req.Id], ct);
        if (cat is null) return Result.Fail($"Không tìm thấy Gage Category ID {req.Id}.");

        if (await db.GageCategories.AnyAsync(c => c.Code == req.Code && c.Id != req.Id, ct))
            return Result.Fail($"Gage Category '{req.Code}' đã tồn tại.");

        cat.Code = req.Code;
        cat.Name = req.Name;
        cat.Description = req.Description;
        cat.IsActive = req.IsActive;
        await db.SaveChangesAsync(ct);
        return Result.Ok(new GageCategoryDto(cat.Id, cat.Code, cat.Name, cat.Description, cat.IsActive));
    }
}
