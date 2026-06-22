using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Production;

// ── DTOs ─────────────────────────────────────────────────────

public record QcInlineRateDto(
    int Id, int? JobId, string? JobNumber, int? PartOpId, string? OpNumber,
    decimal RatePercent, bool IsActive);

// ── Queries ──────────────────────────────────────────────────

public record GetQcInlineRatesQuery : IRequest<Result<List<QcInlineRateDto>>>;

public class GetQcInlineRatesQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetQcInlineRatesQuery, Result<List<QcInlineRateDto>>>
{
    public async Task<Result<List<QcInlineRateDto>>> Handle(GetQcInlineRatesQuery req, CancellationToken ct)
    {
        var rates = await db.QcInlineRates
            .OrderByDescending(r => r.JobId.HasValue).ThenByDescending(r => r.PartOpId.HasValue)
            .ToListAsync(ct);

        var jobIds = rates.Where(r => r.JobId.HasValue).Select(r => r.JobId!.Value).Distinct().ToList();
        var jobNumbers = await db.Jobs.Where(j => jobIds.Contains(j.Id))
            .ToDictionaryAsync(j => j.Id, j => j.JobNumber, ct);

        var opIds = rates.Where(r => r.PartOpId.HasValue).Select(r => r.PartOpId!.Value).Distinct().ToList();
        var opNumbers = await db.PartOps.Where(o => opIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.OpNumber, ct);

        var dtos = rates.Select(r => new QcInlineRateDto(
            r.Id, r.JobId, r.JobId.HasValue ? jobNumbers.GetValueOrDefault(r.JobId.Value) : null,
            r.PartOpId, r.PartOpId.HasValue ? opNumbers.GetValueOrDefault(r.PartOpId.Value) : null,
            r.RatePercent, r.IsActive)).ToList();
        return Result.Ok(dtos);
    }
}

public record GetEffectiveQcInlineRateQuery(int JobId, int? PartOpId) : IRequest<Result<decimal>>;

public class GetEffectiveQcInlineRateQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetEffectiveQcInlineRateQuery, Result<decimal>>
{
    public async Task<Result<decimal>> Handle(GetEffectiveQcInlineRateQuery req, CancellationToken ct)
    {
        var candidates = await db.QcInlineRates
            .Where(r => r.IsActive && (
                (r.JobId == req.JobId && r.PartOpId == req.PartOpId) ||
                (r.JobId == req.JobId && r.PartOpId == null) ||
                (r.JobId == null && r.PartOpId == req.PartOpId) ||
                (r.JobId == null && r.PartOpId == null)))
            .ToListAsync(ct);

        var best = candidates
            .OrderByDescending(r => r.JobId == req.JobId && r.PartOpId == req.PartOpId)
            .ThenByDescending(r => r.JobId == req.JobId && r.PartOpId == null)
            .ThenByDescending(r => r.JobId == null && r.PartOpId == req.PartOpId)
            .FirstOrDefault();

        return best is null ? Result.Fail("Chưa cấu hình mức kiểm QC Inline.") : Result.Ok(best.RatePercent);
    }
}

// ── Commands ─────────────────────────────────────────────────

public record CreateQcInlineRateCommand(int? JobId, int? PartOpId, decimal RatePercent)
    : IRequest<Result<QcInlineRateDto>>;

public class CreateQcInlineRateCommandValidator : AbstractValidator<CreateQcInlineRateCommand>
{
    public CreateQcInlineRateCommandValidator()
    {
        RuleFor(x => x.RatePercent).InclusiveBetween(0m, 100m);
    }
}

public class CreateQcInlineRateCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateQcInlineRateCommand, Result<QcInlineRateDto>>
{
    public async Task<Result<QcInlineRateDto>> Handle(CreateQcInlineRateCommand req, CancellationToken ct)
    {
        if (await db.QcInlineRates.AnyAsync(r => r.JobId == req.JobId && r.PartOpId == req.PartOpId, ct))
            return Result.Fail("Đã có mức kiểm cho Job/OP này — hãy sửa dòng hiện có.");

        var rate = new QcInlineRate { JobId = req.JobId, PartOpId = req.PartOpId, RatePercent = req.RatePercent, IsActive = true };
        db.QcInlineRates.Add(rate);
        await db.SaveChangesAsync(ct);
        return Result.Ok(new QcInlineRateDto(rate.Id, rate.JobId, null, rate.PartOpId, null, rate.RatePercent, rate.IsActive));
    }
}

public record UpdateQcInlineRateCommand(int Id, decimal RatePercent, bool IsActive)
    : IRequest<Result<QcInlineRateDto>>;

public class UpdateQcInlineRateCommandValidator : AbstractValidator<UpdateQcInlineRateCommand>
{
    public UpdateQcInlineRateCommandValidator()
    {
        RuleFor(x => x.RatePercent).InclusiveBetween(0m, 100m);
    }
}

public class UpdateQcInlineRateCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdateQcInlineRateCommand, Result<QcInlineRateDto>>
{
    public async Task<Result<QcInlineRateDto>> Handle(UpdateQcInlineRateCommand req, CancellationToken ct)
    {
        var rate = await db.QcInlineRates.FindAsync([req.Id], ct);
        if (rate is null) return Result.Fail($"Không tìm thấy mức kiểm ID {req.Id}.");

        var isFactoryDefault = rate.JobId is null && rate.PartOpId is null;
        if (isFactoryDefault && !req.IsActive)
            return Result.Fail("Không thể ẩn mức kiểm mặc định toàn nhà máy.");

        rate.RatePercent = req.RatePercent;
        rate.IsActive = req.IsActive;
        await db.SaveChangesAsync(ct);
        return Result.Ok(new QcInlineRateDto(rate.Id, rate.JobId, null, rate.PartOpId, null, rate.RatePercent, rate.IsActive));
    }
}
