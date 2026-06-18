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
    string? CategoryCode, bool IsCritical, bool IsFinal, int SortOrder,
    // Approval workflow
    string Status = "Approved", int? ReviewedBy = null, DateTimeOffset? ReviewedAt = null, string? ReviewNote = null,
    // OP gốc sở hữu dimension — chỉ set khi xem qua OP INS (xem GetFaiSheetQueryHandler)
    string? OpNumber = null);

// ── FAI Sheet ─────────────────────────────────────────────────

public record GetFaiSheetQuery(int JobId, int? PartOpId) : IRequest<Result<FaiSheetDto>>;

public record FaiSheetDto(
    int JobId, int PartOpId, string OpNumber,
    string JobNumber, string PartNumber, string PartDescription, string RevCode,
    IReadOnlyList<DimensionDto> Dimensions,
    IReadOnlyList<FaiRowDto> Rows);

public record FaiRowDto(
    string SerialNumber, int ProductId,
    IReadOnlyList<FaiCellDto> Cells, bool AllPass);

public record FaiStageValueDto(
    decimal? Value, string? Result, string? MeasuredByName, DateTimeOffset? MeasuredAt,
    string? GageNo, bool HasNcr, string? NcrCode);

public record FaiCellDto(
    long? MeasureValueId, string BalloonNumber,
    decimal? Value, string? Result,
    int? MeasureStage, string? MeasuredByName, DateTimeOffset? MeasuredAt,
    string? GageNo, bool HasNcr, string? NcrCode,
    IReadOnlyDictionary<int, FaiStageValueDto> ByStage);

public class GetFaiSheetQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetFaiSheetQuery, Result<FaiSheetDto>>
{
    public async Task<Result<FaiSheetDto>> Handle(GetFaiSheetQuery req, CancellationToken ct)
    {
        var job = await db.Jobs
            .Include(j => j.PartRev).ThenInclude(pr => pr.Part)
            .FirstOrDefaultAsync(j => j.Id == req.JobId, ct);
        if (job is null) return Result.Fail("Job không tồn tại.");

        var allOps = !req.PartOpId.HasValue;
        PartOp? op = null;
        var isInspectionOp = false;
        if (!allOps)
        {
            op = await db.PartOps.Include(o => o.OpType).FirstOrDefaultAsync(o => o.Id == req.PartOpId, ct);
            if (op is null) return Result.Fail("PartOp không tồn tại.");
            isInspectionOp = string.Equals(op.OpType?.Code, "INS", StringComparison.OrdinalIgnoreCase);
        }

        // Gom dimension từ nhiều OP — khi chọn "Tất cả OP" (gom toàn bộ routing) hoặc khi xem qua OP INS
        // (gom các OP có OpNumberSort nhỏ hơn OP INS đang xét — xem CLAUDE.md / 06_dimensions_fai.md §4.3)
        var tagOpNumber = allOps || isInspectionOp;
        List<Dimension> dims;
        if (allOps || isInspectionOp)
        {
            var routingOps = await db.PartOps
                .Where(p => (p.RoutingRevId == job.RoutingRevId && !p.ForJobOnly) || (p.ForJobOnly && p.JobId == job.Id))
                .ToListAsync(ct);
            decimal EffectiveSort(PartOp p) => p.OpNumberSort ?? 9999m;
            var scopedOpIds = allOps
                ? routingOps.Select(p => p.Id).ToList()
                : routingOps.Where(p => EffectiveSort(p) < EffectiveSort(op!)).Select(p => p.Id).ToList();

            dims = await db.Dimensions
                .Include(d => d.Category).Include(d => d.PartOp)
                .Where(d => scopedOpIds.Contains(d.PartOpId))
                .OrderBy(d => d.PartOp.OpNumberSort ?? 9999).ThenBy(d => d.BalloonSort ?? 9999).ThenBy(d => d.BalloonNumber)
                .ToListAsync(ct);
        }
        else
        {
            dims = await db.Dimensions
                .Include(d => d.Category)
                .Where(d => d.PartOpId == req.PartOpId)
                .OrderBy(d => d.BalloonSort ?? 9999).ThenBy(d => d.BalloonNumber)
                .ToListAsync(ct);
        }

        var products = await db.Products
            .Where(p => p.JobId == req.JobId)
            .OrderBy(p => p.SortOrder ?? p.Id).ToListAsync(ct);

        var dimIds = dims.Select(d => d.Id).ToList();
        var productIds = products.Select(p => p.Id).ToList();

        // Lấy MeasureValue mới nhất per (DimensionId, ProductId, MeasureStage) — giữ giá trị riêng từng stage
        var latestPerStage = await db.MeasureValues
            .Where(m => dimIds.Contains(m.DimensionId) && productIds.Contains(m.ProductId))
            .GroupBy(m => new { m.DimensionId, m.ProductId, m.MeasureStage })
            .Select(g => g.OrderByDescending(m => m.MeasuredAt).First())
            .ToListAsync(ct);

        var inspectorIds = latestPerStage.Where(m => m.MeasuredBy.HasValue).Select(m => m.MeasuredBy!.Value).Distinct().ToList();
        var inspectorNames = await db.Users
            .Where(u => inspectorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        var gageIds = latestPerStage.Where(m => m.GageId.HasValue).Select(m => m.GageId!.Value).Distinct().ToList();
        var gageNos = await db.Gages
            .Where(g => gageIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.GageNo, ct);

        string? InspectorName(int? userId) => userId != null && inspectorNames.TryGetValue(userId.Value, out var name) ? name : null;
        string? GageNo(int? gageId) => gageId != null && gageNos.TryGetValue(gageId.Value, out var no) ? no : null;

        var dimDtos = dims.Select(d => new DimensionDto(
            d.Id, d.PartOpId, d.BalloonNumber, d.BalloonSort, d.Code, d.Description,
            d.NominalValue, d.TolerancePlus, d.ToleranceMinus, d.MaxValue, d.MinValue, d.Unit,
            d.IsTextType, d.NominalText, d.Category?.Code, d.IsCritical, d.IsFinal, d.SortOrder,
            OpNumber: tagOpNumber ? d.PartOp.OpNumber : null))
            .ToList();

        var rows = products.Select(p =>
        {
            var cells = dims.Select(d =>
            {
                var stageRows = latestPerStage.Where(m => m.DimensionId == d.Id && m.ProductId == p.Id).ToList();
                var mv = stageRows.OrderByDescending(m => m.MeasuredAt).FirstOrDefault();
                var byStage = stageRows.ToDictionary(
                    sr => (int)sr.MeasureStage,
                    sr => new FaiStageValueDto(sr.Value, sr.Result.ToString(), InspectorName(sr.MeasuredBy), sr.MeasuredAt,
                        GageNo(sr.GageId), sr.HasNcr, sr.NcrCode));
                return new FaiCellDto(mv?.Id, d.BalloonNumber, mv?.Value, mv?.Result.ToString(),
                    mv != null ? (int)mv.MeasureStage : null, InspectorName(mv?.MeasuredBy), mv?.MeasuredAt,
                    GageNo(mv?.GageId), mv?.HasNcr ?? false, mv?.NcrCode, byStage);
            }).ToList();
            bool allPass = cells.All(c => c.Result == "Pass" || c.Value == null);
            return new FaiRowDto(p.SerialNumber, p.Id, cells, allPass);
        }).ToList();

        return Result.Ok(new FaiSheetDto(
            req.JobId, op?.Id ?? 0, op?.OpNumber ?? "Tất cả",
            job.JobNumber, job.PartRev.Part.PartNumber, job.PartRev.Part.Description, job.PartRev.RevCode,
            dimDtos, rows));
    }
}

// ── Product Measure Sheet — xem 1 Serial xuyên suốt mọi OP trong Job ──────

public record GetProductMeasureSheetQuery(int ProductId) : IRequest<Result<ProductMeasureSheetDto>>;

public record ProductMeasureSheetDto(
    int ProductId, string SerialNumber, int JobId, string JobNumber,
    string PartNumber, string PartDescription, string RevCode,
    IReadOnlyList<ProductMeasureRowDto> Rows);

public record ProductMeasureRowDto(
    string OpNumber, long DimensionId, string BalloonNumber, string? Code, string? Description,
    decimal? NominalValue, decimal? TolerancePlus, decimal? ToleranceMinus, string Unit,
    bool IsTextType, string? NominalText, string? CategoryCode, bool IsCritical, bool IsFinal,
    decimal? Value, string? Result, int? MeasureStage, string? MeasuredByName, DateTimeOffset? MeasuredAt,
    string? GageNo, bool HasNcr, string? NcrCode);

public class GetProductMeasureSheetQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetProductMeasureSheetQuery, Result<ProductMeasureSheetDto>>
{
    public async Task<Result<ProductMeasureSheetDto>> Handle(GetProductMeasureSheetQuery req, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([req.ProductId], ct);
        if (product is null) return Result.Fail("Product không tồn tại.");

        var job = await db.Jobs
            .Include(j => j.PartRev).ThenInclude(pr => pr.Part)
            .FirstOrDefaultAsync(j => j.Id == product.JobId, ct);
        if (job is null) return Result.Fail("Job không tồn tại.");

        // Routing của Job = PartOps thuộc RoutingRev + ForJobOnly OP riêng của Job (theo quy tắc CLAUDE.md)
        var ops = await db.PartOps
            .Where(p => p.IsVisible && ((p.RoutingRevId == job.RoutingRevId && !p.ForJobOnly) || (p.ForJobOnly && p.JobId == job.Id)))
            .OrderBy(p => p.OpNumberSort ?? 9999).ThenBy(p => p.OpNumber)
            .ToListAsync(ct);
        var opIds = ops.Select(o => o.Id).ToList();

        var dims = await db.Dimensions
            .Include(d => d.Category)
            .Where(d => opIds.Contains(d.PartOpId))
            .ToListAsync(ct);
        var dimIds = dims.Select(d => d.Id).ToList();
        var dimsByOp = dims.GroupBy(d => d.PartOpId)
            .ToDictionary(g => g.Key, g => g.OrderBy(d => d.BalloonSort ?? 9999).ThenBy(d => d.BalloonNumber).ToList());

        // MeasureValue mới nhất per Dimension (không phân biệt stage — xem tổng quan toàn bộ Job)
        var latest = await db.MeasureValues
            .Where(m => dimIds.Contains(m.DimensionId) && m.ProductId == req.ProductId)
            .GroupBy(m => m.DimensionId)
            .Select(g => g.OrderByDescending(m => m.MeasuredAt).First())
            .ToListAsync(ct);
        var latestByDim = latest.ToDictionary(m => m.DimensionId);

        var inspectorIds = latest.Where(m => m.MeasuredBy.HasValue).Select(m => m.MeasuredBy!.Value).Distinct().ToList();
        var inspectorNames = await db.Users.Where(u => inspectorIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, ct);
        var gageIds = latest.Where(m => m.GageId.HasValue).Select(m => m.GageId!.Value).Distinct().ToList();
        var gageNos = await db.Gages.Where(g => gageIds.Contains(g.Id)).ToDictionaryAsync(g => g.Id, g => g.GageNo, ct);

        var rows = new List<ProductMeasureRowDto>();
        foreach (var op in ops)
        {
            if (!dimsByOp.TryGetValue(op.Id, out var opDims)) continue;
            foreach (var d in opDims)
            {
                latestByDim.TryGetValue(d.Id, out var mv);
                string? inspectorName = mv?.MeasuredBy != null && inspectorNames.TryGetValue(mv.MeasuredBy.Value, out var name) ? name : null;
                string? gageNo = mv?.GageId != null && gageNos.TryGetValue(mv.GageId.Value, out var no) ? no : null;
                rows.Add(new ProductMeasureRowDto(
                    op.OpNumber, d.Id, d.BalloonNumber, d.Code, d.Description,
                    d.NominalValue, d.TolerancePlus, d.ToleranceMinus, d.Unit,
                    d.IsTextType, d.NominalText, d.Category?.Code, d.IsCritical, d.IsFinal,
                    mv?.Value, mv?.Result.ToString(), mv != null ? (int)mv.MeasureStage : null, inspectorName, mv?.MeasuredAt,
                    gageNo, mv?.HasNcr ?? false, mv?.NcrCode));
            }
        }

        return Result.Ok(new ProductMeasureSheetDto(
            product.Id, product.SerialNumber, job.Id, job.JobNumber,
            job.PartRev.Part.PartNumber, job.PartRev.Part.Description, job.PartRev.RevCode,
            rows));
    }
}

// ── Dimensions (định nghĩa, không gắn Job) ──────────────────────

/// <summary>
/// Danh sách Dimension định nghĩa cho một PartOp — dùng cho trang Part & Routing
/// (không cần JobId, khác với GetFaiSheetQuery dùng cho FAI matrix theo Job).
/// </summary>
public record GetOpDimensionsQuery(int PartOpId) : IRequest<Result<IReadOnlyList<DimensionDto>>>;

public class GetOpDimensionsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetOpDimensionsQuery, Result<IReadOnlyList<DimensionDto>>>
{
    public async Task<Result<IReadOnlyList<DimensionDto>>> Handle(GetOpDimensionsQuery req, CancellationToken ct)
    {
        var dims = await db.Dimensions
            .Include(d => d.Category)
            .Where(d => d.PartOpId == req.PartOpId)
            .OrderBy(d => d.BalloonSort ?? 9999).ThenBy(d => d.BalloonNumber)
            .Select(d => new DimensionDto(
                d.Id, d.PartOpId, d.BalloonNumber, d.BalloonSort, d.Code, d.Description,
                d.NominalValue, d.TolerancePlus, d.ToleranceMinus, d.MaxValue, d.MinValue, d.Unit,
                d.IsTextType, d.NominalText, d.Category != null ? d.Category.Code : null,
                d.IsCritical, d.IsFinal, d.SortOrder,
                d.Status.ToString(), d.ReviewedBy, d.ReviewedAt, d.ReviewNote, null))
            .ToListAsync(ct);

        return Result.Ok<IReadOnlyList<DimensionDto>>(dims);
    }
}

/// <summary>
/// Tổng hợp toàn bộ Dimension của các PartOp template (ForJobOnly=false) thuộc một RoutingRev — dùng cho trang "Dimension Sheet".
/// </summary>
public record RoutingRevDimensionDto(
    long Id, int OpId, string OpNumber, decimal? OpNumberSort,
    string BalloonNumber, decimal? BalloonSort, string? Code, string? Description,
    decimal? NominalValue, decimal? TolerancePlus, decimal? ToleranceMinus,
    decimal? MaxValue, decimal? MinValue, string Unit,
    bool IsTextType, string? NominalText,
    string? CategoryCode, bool IsCritical, bool IsFinal, int SortOrder,
    string Status = "Approved", int? ReviewedBy = null, DateTimeOffset? ReviewedAt = null, string? ReviewNote = null);

public record GetDimensionsByRoutingRevQuery(int RoutingRevId) : IRequest<Result<List<RoutingRevDimensionDto>>>;

public class GetDimensionsByRoutingRevQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetDimensionsByRoutingRevQuery, Result<List<RoutingRevDimensionDto>>>
{
    public async Task<Result<List<RoutingRevDimensionDto>>> Handle(GetDimensionsByRoutingRevQuery req, CancellationToken ct)
    {
        var items = await db.Dimensions
            .Include(d => d.Category)
            .Include(d => d.PartOp)
            .Where(d => d.PartOp.RoutingRevId == req.RoutingRevId && !d.PartOp.ForJobOnly && d.PartOp.IsVisible)
            .OrderBy(d => d.PartOp.OpNumberSort ?? 9999).ThenBy(d => d.PartOp.OpNumber)
            .ThenBy(d => d.BalloonSort ?? 9999).ThenBy(d => d.BalloonNumber)
            .Select(d => new RoutingRevDimensionDto(
                d.Id, d.PartOpId, d.PartOp.OpNumber, d.PartOp.OpNumberSort,
                d.BalloonNumber, d.BalloonSort, d.Code, d.Description,
                d.NominalValue, d.TolerancePlus, d.ToleranceMinus, d.MaxValue, d.MinValue, d.Unit,
                d.IsTextType, d.NominalText, d.Category != null ? d.Category.Code : null,
                d.IsCritical, d.IsFinal, d.SortOrder,
                d.Status.ToString(), d.ReviewedBy, d.ReviewedAt, d.ReviewNote))
            .ToListAsync(ct);

        return Result.Ok(items);
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
            dim.IsTextType, dim.NominalText, cat?.Code, dim.IsCritical, dim.IsFinal, dim.SortOrder,
            dim.Status.ToString(), dim.ReviewedBy, dim.ReviewedAt, dim.ReviewNote));
    }
}

// ── Update Dimension (inline edit từ Dimension Sheet) ────────────

public record UpdateDimensionCommand(
    long Id, decimal? NominalValue, decimal? TolerancePlus, decimal? ToleranceMinus, int? RequesterId)
    : IRequest<Result<DimensionDto>>;

public class UpdateDimensionCommandValidator : AbstractValidator<UpdateDimensionCommand>
{
    public UpdateDimensionCommandValidator()
    {
        RuleFor(x => x.TolerancePlus).GreaterThanOrEqualTo(0).When(x => x.TolerancePlus.HasValue);
        RuleFor(x => x.ToleranceMinus).GreaterThanOrEqualTo(0).When(x => x.ToleranceMinus.HasValue);
    }
}

public class UpdateDimensionCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdateDimensionCommand, Result<DimensionDto>>
{
    public async Task<Result<DimensionDto>> Handle(UpdateDimensionCommand req, CancellationToken ct)
    {
        var dim = await db.Dimensions.Include(d => d.Category).FirstOrDefaultAsync(d => d.Id == req.Id, ct);
        if (dim is null) return Result.Fail($"Không tìm thấy Dimension ID {req.Id}.");
        if (dim.IsTextType) return Result.Fail("Không thể sửa Nominal/Tolerance của dimension dạng text.");

        dim.NominalValue = req.NominalValue;
        dim.TolerancePlus = req.TolerancePlus;
        dim.ToleranceMinus = req.ToleranceMinus;
        dim.MaxValue = dim.NominalValue.HasValue && dim.TolerancePlus.HasValue ? dim.NominalValue + dim.TolerancePlus : null;
        dim.MinValue = dim.NominalValue.HasValue && dim.ToleranceMinus.HasValue ? dim.NominalValue - dim.ToleranceMinus : null;
        dim.UpdatedAt = DateTimeOffset.UtcNow;
        dim.UpdatedBy = req.RequesterId;

        await db.SaveChangesAsync(ct);

        return Result.Ok(new DimensionDto(dim.Id, dim.PartOpId,
            dim.BalloonNumber, dim.BalloonSort, dim.Code, dim.Description,
            dim.NominalValue, dim.TolerancePlus, dim.ToleranceMinus, dim.MaxValue, dim.MinValue, dim.Unit,
            dim.IsTextType, dim.NominalText, dim.Category?.Code, dim.IsCritical, dim.IsFinal, dim.SortOrder,
            dim.Status.ToString(), dim.ReviewedBy, dim.ReviewedAt, dim.ReviewNote));
    }
}

// ── Import Dimensions từ Excel ───────────────────────────────

public record ImportDimensionRow(
    string BalloonNumber, string? Code, string? Description, string? NominalRaw,
    decimal? TolPlus, decimal? TolMinus, string? Unit, string? CategoryCode,
    bool IsFinal = false);

// ── Import Bulk Dimensions cho toàn bộ RoutingRev từ 1 file Excel ─────────

/// <summary>
/// Dòng trong file Excel bulk import — thêm cột OpNumber và IsFinal so với ImportDimensionRow.
/// </summary>
public record ImportBulkDimensionRow(
    string OpNumber,
    string BalloonNumber, string? Code, string? Description, string? NominalRaw,
    decimal? TolPlus, decimal? TolMinus, string? Unit, string? CategoryCode,
    bool IsFinal = false);

public record ImportBulkDimensionsCommand(int RoutingRevId, List<ImportBulkDimensionRow> Rows, int? RequesterId)
    : IRequest<Result<ImportResultDto>>;

public class ImportBulkDimensionsCommandValidator : AbstractValidator<ImportBulkDimensionsCommand>
{
    public ImportBulkDimensionsCommandValidator()
    {
        RuleFor(x => x.RoutingRevId).GreaterThan(0);
        RuleFor(x => x.Rows).NotEmpty();
    }
}

public class ImportBulkDimensionsCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<ImportBulkDimensionsCommand, Result<ImportResultDto>>
{
    public async Task<Result<ImportResultDto>> Handle(ImportBulkDimensionsCommand req, CancellationToken ct)
    {
        // Load tất cả PartOps của RoutingRev
        var ops = await db.PartOps
            .Where(o => o.RoutingRevId == req.RoutingRevId && o.JobId == null)
            .ToListAsync(ct);

        if (ops.Count == 0)
            return Result.Fail($"RoutingRev {req.RoutingRevId} không có OP nào.");

        var opByNumber = ops.ToDictionary(o => o.OpNumber, StringComparer.OrdinalIgnoreCase);

        // Load existing balloons per OP
        var existingBalloons = (await db.Dimensions
            .Where(d => ops.Select(o => o.Id).Contains(d.PartOpId))
            .Select(d => new { d.PartOpId, d.BalloonNumber })
            .ToListAsync(ct))
            .GroupBy(x => x.PartOpId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.BalloonNumber)
                .ToHashSet(StringComparer.OrdinalIgnoreCase));

        var categories = await db.DimensionCategories.ToListAsync(ct);

        var errors = new List<ImportRowError>();
        int created = 0, skipped = 0;

        for (var i = 0; i < req.Rows.Count; i++)
        {
            var row = req.Rows[i];
            var rowNumber = i + 2;

            // Validate OpNumber
            if (string.IsNullOrWhiteSpace(row.OpNumber))
            {
                errors.Add(new ImportRowError(rowNumber, "Thiếu OpNumber."));
                skipped++;
                continue;
            }

            if (!opByNumber.TryGetValue(row.OpNumber, out var op))
            {
                errors.Add(new ImportRowError(rowNumber, $"OP '{row.OpNumber}' không tồn tại trong RoutingRev."));
                skipped++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.BalloonNumber))
            {
                errors.Add(new ImportRowError(rowNumber, "Thiếu BalloonNumber."));
                skipped++;
                continue;
            }

            if (!existingBalloons.TryGetValue(op.Id, out var balloonSet))
            {
                balloonSet = [];
                existingBalloons[op.Id] = balloonSet;
            }

            if (balloonSet.Contains(row.BalloonNumber))
            {
                errors.Add(new ImportRowError(rowNumber, $"Balloon '{row.BalloonNumber}' đã tồn tại trong OP {row.OpNumber}."));
                skipped++;
                continue;
            }

            decimal? balloonSort = null;
            var numPart = new string(row.BalloonNumber.TakeWhile(char.IsDigit).ToArray());
            if (decimal.TryParse(numPart, out var bs)) balloonSort = bs;

            int? categoryId = null;
            if (!string.IsNullOrWhiteSpace(row.CategoryCode))
            {
                var cat = categories.FirstOrDefault(c => string.Equals(c.Code, row.CategoryCode, StringComparison.OrdinalIgnoreCase));
                if (cat is null)
                    errors.Add(new ImportRowError(rowNumber, $"Không tìm thấy Category '{row.CategoryCode}'."));
                else
                    categoryId = cat.Id;
            }

            var dim = new Dimension
            {
                PartOpId = op.Id,
                BalloonNumber = row.BalloonNumber, BalloonSort = balloonSort,
                Code = row.Code, Description = row.Description,
                CategoryId = categoryId, CreatedBy = req.RequesterId,
                IsFinal = row.IsFinal,
                Status = FileStatus.Pending,  // Bulk import cần Lead Engineer duyệt
            };

            if (decimal.TryParse(row.NominalRaw, out var nominal))
            {
                var tolPlus = row.TolPlus ?? 0;
                var tolMinus = row.TolMinus ?? 0;
                dim.NominalValue = nominal;
                dim.TolerancePlus = tolPlus;
                dim.ToleranceMinus = tolMinus;
                dim.MaxValue = nominal + tolPlus;
                dim.MinValue = nominal - tolMinus;
                dim.Unit = string.IsNullOrWhiteSpace(row.Unit) ? "mm" : row.Unit;
            }
            else if (!string.IsNullOrWhiteSpace(row.NominalRaw))
            {
                dim.IsTextType = true;
                dim.NominalText = row.NominalRaw;
            }
            else
            {
                errors.Add(new ImportRowError(rowNumber, "Thiếu Nominal."));
                skipped++;
                continue;
            }

            db.Dimensions.Add(dim);
            balloonSet.Add(row.BalloonNumber);
            created++;
        }

        await db.SaveChangesAsync(ct);
        return Result.Ok(new ImportResultDto(created, 0, skipped, errors));
    }
}

public record ImportDimensionsCommand(int PartOpId, List<ImportDimensionRow> Rows, int? RequesterId)
    : IRequest<Result<ImportResultDto>>;

public class ImportDimensionsCommandValidator : AbstractValidator<ImportDimensionsCommand>
{
    public ImportDimensionsCommandValidator()
    {
        RuleFor(x => x.PartOpId).GreaterThan(0);
        RuleFor(x => x.Rows).NotEmpty();
    }
}

public class ImportDimensionsCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<ImportDimensionsCommand, Result<ImportResultDto>>
{
    public async Task<Result<ImportResultDto>> Handle(ImportDimensionsCommand req, CancellationToken ct)
    {
        var existingBalloons = (await db.Dimensions
            .Where(d => d.PartOpId == req.PartOpId)
            .Select(d => d.BalloonNumber)
            .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var categories = await db.DimensionCategories.ToListAsync(ct);

        var errors = new List<ImportRowError>();
        int created = 0, skipped = 0;

        for (var i = 0; i < req.Rows.Count; i++)
        {
            var row = req.Rows[i];
            var rowNumber = i + 2; // dòng 1 là header

            if (string.IsNullOrWhiteSpace(row.BalloonNumber))
            {
                errors.Add(new ImportRowError(rowNumber, "Thiếu BalloonNumber."));
                skipped++;
                continue;
            }

            if (existingBalloons.Contains(row.BalloonNumber))
            {
                errors.Add(new ImportRowError(rowNumber, $"Balloon '{row.BalloonNumber}' đã tồn tại."));
                skipped++;
                continue;
            }

            // Parse BalloonSort từ phần số đầu BalloonNumber
            decimal? balloonSort = null;
            var numPart = new string(row.BalloonNumber.TakeWhile(char.IsDigit).ToArray());
            if (decimal.TryParse(numPart, out var bs)) balloonSort = bs;

            int? categoryId = null;
            if (!string.IsNullOrWhiteSpace(row.CategoryCode))
            {
                var cat = categories.FirstOrDefault(c => string.Equals(c.Code, row.CategoryCode, StringComparison.OrdinalIgnoreCase));
                if (cat is null)
                    errors.Add(new ImportRowError(rowNumber, $"Không tìm thấy Category '{row.CategoryCode}'."));
                else
                    categoryId = cat.Id;
            }

            var dim = new Dimension
            {
                PartOpId = req.PartOpId,
                BalloonNumber = row.BalloonNumber, BalloonSort = balloonSort,
                Code = row.Code, Description = row.Description,
                CategoryId = categoryId, CreatedBy = req.RequesterId,
                IsFinal = row.IsFinal,
                Status = FileStatus.Pending,  // Import per-OP cũng cần duyệt
            };

            if (decimal.TryParse(row.NominalRaw, out var nominal))
            {
                var tolPlus = row.TolPlus ?? 0;
                var tolMinus = row.TolMinus ?? 0;
                dim.NominalValue = nominal;
                dim.TolerancePlus = tolPlus;
                dim.ToleranceMinus = tolMinus;
                dim.MaxValue = nominal + tolPlus;
                dim.MinValue = nominal - tolMinus;
                dim.Unit = string.IsNullOrWhiteSpace(row.Unit) ? "mm" : row.Unit;
            }
            else if (!string.IsNullOrWhiteSpace(row.NominalRaw))
            {
                dim.IsTextType = true;
                dim.NominalText = row.NominalRaw;
            }
            else
            {
                errors.Add(new ImportRowError(rowNumber, "Thiếu Nominal."));
                skipped++;
                continue;
            }

            db.Dimensions.Add(dim);
            existingBalloons.Add(row.BalloonNumber);
            created++;
        }

        await db.SaveChangesAsync(ct);

        return Result.Ok(new ImportResultDto(created, 0, skipped, errors));
    }
}

// ── MeasureValues ─────────────────────────────────────────────

public record SaveMeasureCommand(
    long DimensionId, int ProductId, decimal? Value,
    bool? ManualResult,    // Dùng cho text dimension — true=Pass, false=Fail
    bool IsFinal,          // true khi re-inspect sau rework (FAI Final)
    int? FinalOpId,
    string? Note, int RequesterId,
    MeasureStage MeasureStage = MeasureStage.InprocessFAI)
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
            MeasuredBy = req.RequesterId,
            MeasureStage = req.MeasureStage,
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

// ── Review Dimension (Approve / Reject) ──────────────────────

public record ReviewDimensionCommand(long Id, bool Approve, string? Note, int ReviewerId)
    : IRequest<Result<DimensionDto>>;

public class ReviewDimensionCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<ReviewDimensionCommand, Result<DimensionDto>>
{
    public async Task<Result<DimensionDto>> Handle(ReviewDimensionCommand req, CancellationToken ct)
    {
        var dim = await db.Dimensions.Include(d => d.Category)
            .FirstOrDefaultAsync(d => d.Id == req.Id, ct);
        if (dim is null) return Result.Fail($"Không tìm thấy Dimension ID {req.Id}.");
        if (dim.Status == FileStatus.Approved)
            return Result.Fail("Dimension đã được Approved — không thể review lại.");

        dim.Status = req.Approve ? FileStatus.Approved : FileStatus.Rejected;
        dim.ReviewedBy = req.ReviewerId;
        dim.ReviewedAt = DateTimeOffset.UtcNow;
        dim.ReviewNote = req.Note;
        dim.UpdatedAt = DateTimeOffset.UtcNow;
        dim.UpdatedBy = req.ReviewerId;

        await db.SaveChangesAsync(ct);
        return Result.Ok(new DimensionDto(dim.Id, dim.PartOpId,
            dim.BalloonNumber, dim.BalloonSort, dim.Code, dim.Description,
            dim.NominalValue, dim.TolerancePlus, dim.ToleranceMinus, dim.MaxValue, dim.MinValue, dim.Unit,
            dim.IsTextType, dim.NominalText, dim.Category?.Code, dim.IsCritical, dim.IsFinal, dim.SortOrder,
            dim.Status.ToString(), dim.ReviewedBy, dim.ReviewedAt, dim.ReviewNote));
    }
}

/// <summary>Approve/Reject toàn bộ Dimension Pending của một RoutingRev cùng lúc.</summary>
public record ReviewBatchDimensionsCommand(int RoutingRevId, bool Approve, string? Note, int ReviewerId)
    : IRequest<Result<int>>;

public class ReviewBatchDimensionsCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<ReviewBatchDimensionsCommand, Result<int>>
{
    public async Task<Result<int>> Handle(ReviewBatchDimensionsCommand req, CancellationToken ct)
    {
        var dims = await db.Dimensions
            .Where(d => d.PartOp.RoutingRevId == req.RoutingRevId
                        && !d.PartOp.ForJobOnly
                        && d.Status == FileStatus.Pending)
            .Include(d => d.PartOp)
            .ToListAsync(ct);

        if (dims.Count == 0) return Result.Ok(0);

        var now = DateTimeOffset.UtcNow;
        foreach (var dim in dims)
        {
            dim.Status = req.Approve ? FileStatus.Approved : FileStatus.Rejected;
            dim.ReviewedBy = req.ReviewerId;
            dim.ReviewedAt = now;
            dim.ReviewNote = req.Note;
            dim.UpdatedAt = now;
            dim.UpdatedBy = req.ReviewerId;
        }

        await db.SaveChangesAsync(ct);
        return Result.Ok(dims.Count);
    }
}

// ── QC Final Progress ──────────────────────────────────────────

public record QcFinalProgressDto(int TotalDim, int CompleteDim, int PassDim, int FailDim);

/// <summary>Tiến độ QC Final cho một Product: số dimension đã đo ở stage QCFinal.</summary>
public record GetQcFinalProgressQuery(int ProductId)
    : IRequest<Result<QcFinalProgressDto>>;

public class GetQcFinalProgressQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetQcFinalProgressQuery, Result<QcFinalProgressDto>>
{
    public async Task<Result<QcFinalProgressDto>> Handle(GetQcFinalProgressQuery req, CancellationToken ct)
    {
        // Lấy product và job liên quan
        var product = await db.Products.Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.Id == req.ProductId, ct);
        if (product is null) return Result.Fail("Product không tồn tại.");

        var routingRevId = product.Job.RoutingRevId;

        // Tổng số dimension của routing rev này (template OPs)
        var totalDim = await db.Dimensions
            .CountAsync(d => d.PartOp.RoutingRevId == routingRevId && !d.PartOp.ForJobOnly, ct);

        // Lấy MeasureValues giai đoạn QCFinal cho product này
        // Chỉ lấy lần đo gần nhất (theo CreatedAt) cho mỗi DimensionId
        var mvQuery = db.MeasureValues
            .Where(mv => mv.ProductId == req.ProductId && mv.MeasureStage == MeasureStage.QCFinal);

        var latestMvs = await mvQuery
            .GroupBy(mv => mv.DimensionId)
            .Select(g => new { DimId = g.Key, Result = g.OrderByDescending(x => x.MeasuredAt).First().Result })
            .ToListAsync(ct);

        var completeDim = latestMvs.Count;
        var passDim = latestMvs.Count(x => x.Result == MeasureResult.Pass);
        var failDim = latestMvs.Count(x => x.Result == MeasureResult.Fail);

        return Result.Ok(new QcFinalProgressDto(totalDim, completeDim, passDim, failDim));
    }
}
