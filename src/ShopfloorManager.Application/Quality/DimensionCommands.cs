using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Quality;

public record DimensionDto(
    long Id, int PartOpId, string Code, string? Description,
    decimal Nominal, decimal UpperTol, decimal LowerTol,
    decimal UpperLimit, decimal LowerLimit,
    string Unit, bool IsCritical, int SortOrder);

// ── Queries ───────────────────────────────────────────────────

public record GetDimensionsQuery(int PartOpId) : IRequest<Result<List<DimensionDto>>>;

public class GetDimensionsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetDimensionsQuery, Result<List<DimensionDto>>>
{
    public async Task<Result<List<DimensionDto>>> Handle(GetDimensionsQuery req, CancellationToken ct)
    {
        var items = await db.Dimensions
            .Where(d => d.PartOpId == req.PartOpId)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Code)
            .Select(d => new DimensionDto(d.Id, d.PartOpId, d.Code, d.Description,
                d.Nominal, d.UpperTol, d.LowerTol,
                d.Nominal + d.UpperTol, d.Nominal + d.LowerTol,
                d.Unit, d.IsCritical, d.SortOrder))
            .ToListAsync(ct);
        return Result.Ok(items);
    }
}

// ── Commands ──────────────────────────────────────────────────

public record CreateDimensionCommand(
    int PartOpId, string Code, string? Description,
    decimal Nominal, decimal UpperTol, decimal LowerTol,
    string Unit, bool IsCritical, int SortOrder, int? RequesterId)
    : IRequest<Result<DimensionDto>>;

public class CreateDimensionCommandValidator : AbstractValidator<CreateDimensionCommand>
{
    public CreateDimensionCommandValidator()
    {
        RuleFor(x => x.PartOpId).GreaterThan(0);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.UpperTol).GreaterThanOrEqualTo(0).WithMessage("UpperTol phải >= 0");
        RuleFor(x => x.LowerTol).LessThanOrEqualTo(0).WithMessage("LowerTol phải <= 0");
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
    }
}

public class CreateDimensionCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateDimensionCommand, Result<DimensionDto>>
{
    public async Task<Result<DimensionDto>> Handle(CreateDimensionCommand req, CancellationToken ct)
    {
        if (await db.Dimensions.AnyAsync(d => d.PartOpId == req.PartOpId && d.Code == req.Code, ct))
            return Result.Fail($"Dimension '{req.Code}' đã tồn tại trong operation này.");

        var dim = new Dimension
        {
            PartOpId = req.PartOpId, Code = req.Code, Description = req.Description,
            Nominal = req.Nominal, UpperTol = req.UpperTol, LowerTol = req.LowerTol,
            Unit = req.Unit, IsCritical = req.IsCritical, SortOrder = req.SortOrder,
            CreatedBy = req.RequesterId
        };
        db.Dimensions.Add(dim);
        await db.SaveChangesAsync(ct);

        return Result.Ok(new DimensionDto(dim.Id, dim.PartOpId, dim.Code, dim.Description,
            dim.Nominal, dim.UpperTol, dim.LowerTol,
            dim.Nominal + dim.UpperTol, dim.Nominal + dim.LowerTol,
            dim.Unit, dim.IsCritical, dim.SortOrder));
    }
}

public record UpdateDimensionCommand(
    long Id, string? Description, decimal Nominal, decimal UpperTol, decimal LowerTol,
    string Unit, bool IsCritical, int SortOrder)
    : IRequest<Result<DimensionDto>>;

public class UpdateDimensionCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdateDimensionCommand, Result<DimensionDto>>
{
    public async Task<Result<DimensionDto>> Handle(UpdateDimensionCommand req, CancellationToken ct)
    {
        var dim = await db.Dimensions.FindAsync([req.Id], ct);
        if (dim is null) return Result.Fail($"Không tìm thấy Dimension ID {req.Id}.");

        dim.Description = req.Description;
        dim.Nominal = req.Nominal;
        dim.UpperTol = req.UpperTol;
        dim.LowerTol = req.LowerTol;
        dim.Unit = req.Unit;
        dim.IsCritical = req.IsCritical;
        dim.SortOrder = req.SortOrder;
        await db.SaveChangesAsync(ct);

        return Result.Ok(new DimensionDto(dim.Id, dim.PartOpId, dim.Code, dim.Description,
            dim.Nominal, dim.UpperTol, dim.LowerTol,
            dim.Nominal + dim.UpperTol, dim.Nominal + dim.LowerTol,
            dim.Unit, dim.IsCritical, dim.SortOrder));
    }
}
