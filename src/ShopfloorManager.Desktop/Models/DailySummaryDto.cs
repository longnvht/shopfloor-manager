namespace ShopfloorManager.Desktop.Models;

public record DailySummaryDto(
    int CompletedCount,
    int TotalActiveMinutes,
    int PassCount,
    int FailCount)
{
    public int TotalMeasured => PassCount + FailCount;
    public double QualityPct => TotalMeasured > 0 ? PassCount * 100.0 / TotalMeasured : 0;
}
