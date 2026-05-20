using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Application.Production;

public record DimensionDto(
    long Id, int PartOpId, string BalloonNumber, string? Code, string? Description,
    decimal Nominal, decimal UpperTol, decimal LowerTol,
    decimal UpperLimit, decimal LowerLimit, string Unit, bool IsCritical, int SortOrder);

// ── FAI Sheet ─────────────────────────────────────────────────

public record GetFaiSheetQuery(int JobId, int PartOpId) : IRequest<Result<FaiSheetDto>>;

public record FaiSheetDto(
    int JobId, int PartOpId, string OpNumber,
    IReadOnlyList<DimensionDto> Dimensions,
    IReadOnlyList<FaiRowDto> Rows);

public record FaiRowDto(
    string SerialNumber, int ProductId,
    IReadOnlyList<FaiCellDto> Cells, bool AllPass);

public record FaiCellDto(
    long? MeasureValueId, string BalloonNumber,
    decimal? Value, string? Result);

public class GetFaiSheetQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetFaiSheetQuery, Result<FaiSheetDto>>
{
    public async Task<Result<FaiSheetDto>> Handle(GetFaiSheetQuery req, CancellationToken ct)
    {
        var op = await db.PartOps.FindAsync([req.PartOpId], ct);
        if (op is null) return Result.Fail("PartOp không tồn tại.");

        var dims = await db.Dimensions
            .Where(d => d.PartOpId == req.PartOpId)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.BalloonNumber)
            .ToListAsync(ct);

        var products = await db.Products
            .Where(p => p.JobId == req.JobId)
            .OrderBy(p => p.SortOrder ?? p.Id).ToListAsync(ct);

        var dimIds = dims.Select(d => d.Id).ToList();
        var productIds = products.Select(p => p.Id).ToList();

        var measures = await db.MeasureValues
            .Where(m => dimIds.Contains(m.DimensionId) && productIds.Contains(m.ProductId))
            .ToListAsync(ct);

        var dimDtos = dims.Select(d => new DimensionDto(d.Id, d.PartOpId,
            d.BalloonNumber, d.Code, d.Description,
            d.Nominal, d.UpperTol, d.LowerTol,
            d.Nominal + d.UpperTol, d.Nominal + d.LowerTol,
            d.Unit, d.IsCritical, d.SortOrder)).ToList();

        var rows = products.Select(p =>
        {
            var cells = dims.Select(d =>
            {
                var mv = measures.FirstOrDefault(m => m.DimensionId == d.Id && m.ProductId == p.Id);
                return new FaiCellDto(mv?.Id, d.BalloonNumber, mv?.Value, mv?.Result.ToString());
            }).ToList();
            bool allPass = cells.All(c => c.Result == "Pass" || c.Value == null);
            return new FaiRowDto(p.SerialNumber, p.Id, cells, allPass);
        }).ToList();

        return Result.Ok(new FaiSheetDto(req.JobId, req.PartOpId, op.OpNumber, dimDtos, rows));
    }
}

// ── Dimensions ────────────────────────────────────────────────

public record CreateDimensionCommand(
    int PartOpId, string BalloonNumber, string? Code, string? Description,
    decimal Nominal, decimal UpperTol, decimal LowerTol,
    string Unit, bool IsCritical, int SortOrder, int? RequesterId)
    : IRequest<Result<DimensionDto>>;

public class CreateDimensionCommandValidator : AbstractValidator<CreateDimensionCommand>
{
    public CreateDimensionCommandValidator()
    {
        RuleFor(x => x.PartOpId).GreaterThan(0);
        RuleFor(x => x.BalloonNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.UpperTol).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LowerTol).LessThanOrEqualTo(0);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
    }
}

public class CreateDimensionCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateDimensionCommand, Result<DimensionDto>>
{
    public async Task<Result<DimensionDto>> Handle(CreateDimensionCommand req, CancellationToken ct)
    {
        if (await db.Dimensions.AnyAsync(d => d.PartOpId == req.PartOpId && d.BalloonNumber == req.BalloonNumber, ct))
            return Result.Fail($"Dimension '{req.BalloonNumber}' đã tồn tại trong OP này.");

        var dim = new Dimension
        {
            PartOpId = req.PartOpId, BalloonNumber = req.BalloonNumber, Code = req.Code,
            Description = req.Description, Nominal = req.Nominal,
            UpperTol = req.UpperTol, LowerTol = req.LowerTol,
            Unit = req.Unit, IsCritical = req.IsCritical, SortOrder = req.SortOrder,
            CreatedBy = req.RequesterId
        };
        db.Dimensions.Add(dim);
        await db.SaveChangesAsync(ct);

        return Result.Ok(new DimensionDto(dim.Id, dim.PartOpId, dim.BalloonNumber, dim.Code,
            dim.Description, dim.Nominal, dim.UpperTol, dim.LowerTol,
            dim.Nominal + dim.UpperTol, dim.Nominal + dim.LowerTol,
            dim.Unit, dim.IsCritical, dim.SortOrder));
    }
}

// ── MeasureValues ─────────────────────────────────────────────

public record SaveMeasureCommand(long DimensionId, int ProductId, decimal Value, string? Note, int RequesterId)
    : IRequest<Result<MeasureValueDto>>;

public record MeasureValueDto(
    long Id, long DimensionId, string BalloonNumber,
    int ProductId, string SerialNumber, int PartOpId,
    decimal Value, string Result, string? Note, DateTimeOffset MeasuredAt);

public class SaveMeasureCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<SaveMeasureCommand, Result<MeasureValueDto>>
{
    public async Task<Result<MeasureValueDto>> Handle(SaveMeasureCommand req, CancellationToken ct)
    {
        var dim = await db.Dimensions.FindAsync([req.DimensionId], ct);
        if (dim is null) return Result.Fail("Dimension không tồn tại.");

        var product = await db.Products.FindAsync([req.ProductId], ct);
        if (product is null) return Result.Fail("Product không tồn tại.");

        var upper = dim.Nominal + dim.UpperTol;
        var lower = dim.Nominal + dim.LowerTol;
        var result = req.Value >= lower && req.Value <= upper ? MeasureResult.Pass : MeasureResult.Fail;

        var existing = await db.MeasureValues
            .FirstOrDefaultAsync(m => m.DimensionId == req.DimensionId && m.ProductId == req.ProductId, ct);

        if (existing is not null)
        {
            existing.Value = req.Value;
            existing.Result = result;
            existing.Note = req.Note;
            existing.MeasuredBy = req.RequesterId;
            existing.MeasuredAt = DateTimeOffset.UtcNow;
        }
        else
        {
            existing = new MeasureValue
            {
                DimensionId = req.DimensionId, ProductId = req.ProductId,
                PartOpId = dim.PartOpId,
                Value = req.Value, Result = result, Note = req.Note,
                MeasuredBy = req.RequesterId
            };
            db.MeasureValues.Add(existing);
        }
        await db.SaveChangesAsync(ct);

        return Result.Ok(new MeasureValueDto(existing.Id, dim.Id, dim.BalloonNumber,
            req.ProductId, product.SerialNumber, dim.PartOpId,
            req.Value, result.ToString(), req.Note, existing.MeasuredAt));
    }
}
