namespace ShopfloorManager.Application.Common.Interfaces;

public record SpcResult(
    double Mean, double StdDev,
    double Cp, double Cpu, double Cpl, double Cpk);

public interface ISpcService
{
    SpcResult Calculate(IReadOnlyList<double> values, double usl, double lsl);
}
