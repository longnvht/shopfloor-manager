using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;

namespace ShopfloorManager.Application.Production;

public record GetSpcQuery(long DimensionId) : IRequest<Result<SpcDto>>;

public record SpcDto(
    long DimensionId, string BalloonNumber, string? Code,
    decimal? NominalValue, decimal? MaxValue, decimal? MinValue, string Unit,
    int SampleCount, double Mean, double StdDev,
    double Cp, double Cpu, double Cpl, double Cpk,
    IReadOnlyList<double> Values);

public class GetSpcQueryHandler(IShopfloorDbContext db, ISpcService spc)
    : IRequestHandler<GetSpcQuery, Result<SpcDto>>
{
    public async Task<Result<SpcDto>> Handle(GetSpcQuery req, CancellationToken ct)
    {
        var dim = await db.Dimensions.FindAsync([req.DimensionId], ct);
        if (dim is null) return Result.Fail("Dimension không tồn tại.");

        if (!dim.MaxValue.HasValue || !dim.MinValue.HasValue)
            return Result.Fail("Dimension chưa có MaxValue/MinValue — không thể tính SPC.");

        var values = await db.MeasureValues
            .Where(m => m.DimensionId == req.DimensionId && m.Value.HasValue)
            .OrderBy(m => m.MeasuredAt)
            .Select(m => (double)m.Value!.Value)
            .ToListAsync(ct);

        if (values.Count < 2)
            return Result.Fail("Cần ít nhất 2 giá trị đo để tính SPC.");

        var usl = (double)dim.MaxValue.Value;
        var lsl = (double)dim.MinValue.Value;
        var r = spc.Calculate(values, usl, lsl);

        return Result.Ok(new SpcDto(
            dim.Id, dim.BalloonNumber, dim.Code,
            dim.NominalValue, dim.MaxValue, dim.MinValue, dim.Unit,
            values.Count, r.Mean, r.StdDev,
            r.Cp, r.Cpu, r.Cpl, r.Cpk, values));
    }
}
