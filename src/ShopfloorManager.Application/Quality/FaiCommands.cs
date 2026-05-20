using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Application.Quality;

public record MeasureValueDto(
    long Id, long DimensionId, string DimensionCode, string? Description,
    decimal Nominal, decimal UpperLimit, decimal LowerLimit, string Unit,
    int ProductId, string SerialNumber,
    decimal Value, string Result, string? Note, DateTimeOffset MeasuredAt);

// ── Queries ───────────────────────────────────────────────────

public record GetFaiSheetQuery(int PartOpId, int JobId) : IRequest<Result<FaiSheetDto>>;

public record FaiSheetDto(
    int PartOpId, int JobId,
    IReadOnlyList<DimensionDto> Dimensions,
    IReadOnlyList<FaiRowDto> Rows);

public record FaiRowDto(
    string SerialNumber, int ProductId,
    IReadOnlyList<MeasureCellDto> Cells,
    bool AllPass);

public record MeasureCellDto(
    long? MeasureValueId, string DimensionCode,
    decimal? Value, string? Result);

public class GetFaiSheetQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetFaiSheetQuery, Result<FaiSheetDto>>
{
    public async Task<Result<FaiSheetDto>> Handle(GetFaiSheetQuery req, CancellationToken ct)
    {
        var dims = await db.Dimensions
            .Where(d => d.PartOpId == req.PartOpId)
            .OrderBy(d => d.SortOrder).ToListAsync(ct);

        var products = await db.Products
            .Where(p => p.JobId == req.JobId)
            .OrderBy(p => p.SortOrder ?? p.Id).ToListAsync(ct);

        var dimIds = dims.Select(d => d.Id).ToList();
        var productIds = products.Select(p => p.Id).ToList();

        var measures = await db.MeasureValues
            .Where(m => dimIds.Contains(m.DimensionId) && productIds.Contains(m.ProductId))
            .ToListAsync(ct);

        var dimDtos = dims.Select(d => new DimensionDto(d.Id, d.PartOpId, d.Code, d.Description,
            d.Nominal, d.UpperTol, d.LowerTol,
            d.Nominal + d.UpperTol, d.Nominal + d.LowerTol,
            d.Unit, d.IsCritical, d.SortOrder)).ToList();

        var rows = products.Select(p =>
        {
            var cells = dims.Select(d =>
            {
                var mv = measures.FirstOrDefault(m => m.DimensionId == d.Id && m.ProductId == p.Id);
                return new MeasureCellDto(mv?.Id, d.Code, mv?.Value, mv?.Result.ToString());
            }).ToList();
            return new FaiRowDto(p.SerialNumber, p.Id, cells, cells.All(c => c.Result == "Pass" || c.Value == null));
        }).ToList();

        return Result.Ok(new FaiSheetDto(req.PartOpId, req.JobId, dimDtos, rows));
    }
}

// ── Commands ──────────────────────────────────────────────────

public record SaveMeasureCommand(
    long DimensionId, int ProductId, decimal Value, string? Note, int RequesterId)
    : IRequest<Result<MeasureValueDto>>;

public class SaveMeasureCommandValidator : AbstractValidator<SaveMeasureCommand>
{
    public SaveMeasureCommandValidator()
    {
        RuleFor(x => x.DimensionId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
    }
}

public class SaveMeasureCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<SaveMeasureCommand, Result<MeasureValueDto>>
{
    public async Task<Result<MeasureValueDto>> Handle(SaveMeasureCommand req, CancellationToken ct)
    {
        var dim = await db.Dimensions.FindAsync([req.DimensionId], ct);
        if (dim is null) return Result.Fail("Dimension không tồn tại.");

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
                Value = req.Value, Result = result, Note = req.Note,
                MeasuredBy = req.RequesterId
            };
            db.MeasureValues.Add(existing);
        }
        await db.SaveChangesAsync(ct);

        var product = await db.Products.FindAsync([req.ProductId], ct);
        return Result.Ok(new MeasureValueDto(existing.Id, dim.Id, dim.Code, dim.Description,
            dim.Nominal, dim.Nominal + dim.UpperTol, dim.Nominal + dim.LowerTol, dim.Unit,
            req.ProductId, product?.SerialNumber ?? "",
            req.Value, result.ToString(), req.Note, existing.MeasuredAt));
    }
}
