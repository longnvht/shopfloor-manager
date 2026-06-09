using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Application.Production;

// ── DTOs ─────────────────────────────────────────────────────

public record DimensionDto(
    long Id, int PartOpId,
    string BalloonNumber, decimal? BalloonSort, string? Code, string? Description,
    decimal? NominalValue, decimal? TolerancePlus, decimal? ToleranceMinus,
    decimal? MaxValue, decimal? MinValue, string Unit,
    bool IsTextType, string? NominalText,
    string? CategoryCode, bool IsCritical, bool IsFinal, int SortOrder);

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
            .Include(d => d.Category)
            .Where(d => d.PartOpId == req.PartOpId)
            .OrderBy(d => d.BalloonSort ?? 9999).ThenBy(d => d.BalloonNumber)
            .ToListAsync(ct);

        var products = await db.Products
            .Where(p => p.JobId == req.JobId)
            .OrderBy(p => p.SortOrder ?? p.Id).ToListAsync(ct);

        var dimIds = dims.Select(d => d.Id).ToList();
        var productIds = products.Select(p => p.Id).ToList();

        // Lấy MeasureValue mới nhất per (DimensionId, ProductId)
        var measures = await db.MeasureValues
            .Where(m => dimIds.Contains(m.DimensionId) && productIds.Contains(m.ProductId))
            .GroupBy(m => new { m.DimensionId, m.ProductId })
            .Select(g => g.OrderByDescending(m => m.MeasuredAt).First())
            .ToListAsync(ct);

        var dimDtos = dims.Select(d => new DimensionDto(
            d.Id, d.PartOpId, d.BalloonNumber, d.BalloonSort, d.Code, d.Description,
            d.NominalValue, d.TolerancePlus, d.ToleranceMinus, d.MaxValue, d.MinValue, d.Unit,
            d.IsTextType, d.NominalText, d.Category?.Code, d.IsCritical, d.IsFinal, d.SortOrder))
            .ToList();

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
    int PartOpId,
    string BalloonNumber,
    string? Code, string? Description,
    decimal? NominalValue, decimal? TolerancePlus, decimal? ToleranceMinus,
    string Unit,
    bool IsTextType, string? NominalText,
    int? CategoryId,
    bool IsCritical, bool IsFinal, int SortOrder,
    int? RequesterId)
    : IRequest<Result<DimensionDto>>;

public class CreateDimensionCommandValidator : AbstractValidator<CreateDimensionCommand>
{
    public CreateDimensionCommandValidator()
    {
        RuleFor(x => x.PartOpId).GreaterThan(0);
        RuleFor(x => x.BalloonNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.TolerancePlus).GreaterThanOrEqualTo(0).When(x => x.TolerancePlus.HasValue);
        RuleFor(x => x.ToleranceMinus).GreaterThanOrEqualTo(0).When(x => x.ToleranceMinus.HasValue);
        RuleFor(x => x).Must(x => x.IsTextType || x.NominalValue.HasValue)
            .WithMessage("Kích thước số phải có NominalValue.");
        RuleFor(x => x).Must(x => !x.IsTextType || !string.IsNullOrWhiteSpace(x.NominalText))
            .WithMessage("Kích thước text phải có NominalText.");
    }
}

public class CreateDimensionCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateDimensionCommand, Result<DimensionDto>>
{
    public async Task<Result<DimensionDto>> Handle(CreateDimensionCommand req, CancellationToken ct)
    {
        if (await db.Dimensions.AnyAsync(d => d.PartOpId == req.PartOpId && d.BalloonNumber == req.BalloonNumber, ct))
            return Result.Fail($"Balloon '{req.BalloonNumber}' đã tồn tại trong OP này.");

        // Parse BalloonSort từ BalloonNumber ("1"→1.0, "2A"→2.0, "10"→10.0)
        decimal? balloonSort = null;
        var numPart = new string(req.BalloonNumber.TakeWhile(char.IsDigit).ToArray());
        if (decimal.TryParse(numPart, out var bs)) balloonSort = bs;

        // Compute MaxValue / MinValue từ Nominal ± Tolerance
        decimal? maxValue = req.NominalValue.HasValue && req.TolerancePlus.HasValue
            ? req.NominalValue + req.TolerancePlus : null;
        decimal? minValue = req.NominalValue.HasValue && req.ToleranceMinus.HasValue
            ? req.NominalValue - req.ToleranceMinus : null;

        var dim = new Dimension
        {
            PartOpId = req.PartOpId,
            BalloonNumber = req.BalloonNumber, BalloonSort = balloonSort,
            Code = req.Code, Description = req.Description,
            NominalValue = req.NominalValue, TolerancePlus = req.TolerancePlus, ToleranceMinus = req.ToleranceMinus,
            MaxValue = maxValue, MinValue = minValue, Unit = req.Unit,
            IsTextType = req.IsTextType, NominalText = req.NominalText,
            CategoryId = req.CategoryId, IsCritical = req.IsCritical, IsFinal = req.IsFinal,
            SortOrder = req.SortOrder, CreatedBy = req.RequesterId
        };
        db.Dimensions.Add(dim);
        await db.SaveChangesAsync(ct);

        var cat = req.CategoryId.HasValue
            ? await db.DimensionCategories.FindAsync([req.CategoryId.Value], ct) : null;

        return Result.Ok(new DimensionDto(dim.Id, dim.PartOpId,
            dim.BalloonNumber, dim.BalloonSort, dim.Code, dim.Description,
            dim.NominalValue, dim.TolerancePlus, dim.ToleranceMinus, dim.MaxValue, dim.MinValue, dim.Unit,
            dim.IsTextType, dim.NominalText, cat?.Code, dim.IsCritical, dim.IsFinal, dim.SortOrder));
    }
}

// ── MeasureValues ─────────────────────────────────────────────

public record SaveMeasureCommand(
    long DimensionId, int ProductId, decimal? Value,
    bool? ManualResult,    // Dùng cho text dimension — true=Pass, false=Fail
    bool IsFinal,          // true khi re-inspect sau rework (FAI Final)
    int? FinalOpId,
    string? Note, int RequesterId)
    : IRequest<Result<MeasureValueDto>>;

public record MeasureValueDto(
    long Id, long DimensionId, string BalloonNumber,
    int ProductId, string SerialNumber, int PartOpId,
    decimal? Value, string Result, string? Note, DateTimeOffset MeasuredAt);

public class SaveMeasureCommandHandler(IShopfloorDbContext db, IRealtimeNotifier realtime)
    : IRequestHandler<SaveMeasureCommand, Result<MeasureValueDto>>
{
    public async Task<Result<MeasureValueDto>> Handle(SaveMeasureCommand req, CancellationToken ct)
    {
        var dim = await db.Dimensions.Include(d => d.Category)
            .FirstOrDefaultAsync(d => d.Id == req.DimensionId, ct);
        if (dim is null) return Result.Fail("Dimension không tồn tại.");

        var product = await db.Products.FindAsync([req.ProductId], ct);
        if (product is null) return Result.Fail("Product không tồn tại.");

        MeasureResult result;
        if (dim.IsTextType)
        {
            result = req.ManualResult == true ? MeasureResult.Pass : MeasureResult.Fail;
        }
        else
        {
            if (!req.Value.HasValue) return Result.Fail("Cần nhập giá trị đo cho kích thước số.");
            if (!dim.MaxValue.HasValue || !dim.MinValue.HasValue)
                return Result.Fail("Dimension chưa có MaxValue/MinValue — cần cập nhật dung sai.");
            result = req.Value >= dim.MinValue && req.Value <= dim.MaxValue
                ? MeasureResult.Pass : MeasureResult.Fail;
        }

        // KHÔNG upsert — tạo record mới mỗi lần đo (giữ lịch sử)
        var mv = new MeasureValue
        {
            DimensionId = req.DimensionId, ProductId = req.ProductId,
            PartOpId = dim.PartOpId,
            Value = req.Value, Result = result, Note = req.Note,
            IsFinal = req.IsFinal, FinalOpId = req.FinalOpId,
            MeasuredBy = req.RequesterId
        };
        db.MeasureValues.Add(mv);
        await db.SaveChangesAsync(ct);

        var dto = new MeasureValueDto(mv.Id, dim.Id, dim.BalloonNumber,
            req.ProductId, product.SerialNumber, dim.PartOpId,
            req.Value, result.ToString(), req.Note, mv.MeasuredAt);

        await realtime.NotifyMeasureSubmittedAsync(dto, ct);
        return Result.Ok(dto);
    }
}
