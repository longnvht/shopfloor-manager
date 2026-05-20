using MathNet.Numerics.Statistics;
using ShopfloorManager.Application.Common.Interfaces;

namespace ShopfloorManager.Infrastructure.Services;

public class SpcService : ISpcService
{
    public SpcResult Calculate(IReadOnlyList<double> values, double usl, double lsl)
    {
        var mean = values.Mean();
        var stdDev = values.StandardDeviation();

        if (stdDev == 0)
            return new SpcResult(Math.Round(mean, 6), 0, 0, 0, 0, 0);

        var cp  = (usl - lsl) / (6 * stdDev);
        var cpu = (usl - mean) / (3 * stdDev);
        var cpl = (mean - lsl) / (3 * stdDev);
        var cpk = Math.Min(cpu, cpl);

        return new SpcResult(
            Math.Round(mean, 6), Math.Round(stdDev, 6),
            Math.Round(cp, 3), Math.Round(cpu, 3), Math.Round(cpl, 3), Math.Round(cpk, 3));
    }
}
