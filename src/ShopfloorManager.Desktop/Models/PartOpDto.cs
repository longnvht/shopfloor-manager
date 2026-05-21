namespace ShopfloorManager.Desktop.Models;

public record PartOpDto(
    int Id,
    int? RoutingRevId,
    int? JobId,
    bool ForJobOnly,
    string OpNumber,
    decimal? OpNumberSort,
    int? OpTypeId,
    string? OpTypeName,
    string? Description,
    string? Note,
    decimal? SetupTime,
    decimal? ProdTime,
    bool IsVisible,
    bool IsComplete)
{
    public string SetupTimeDisplay => SetupTime.HasValue ? $"{SetupTime:0.##}h" : "—";
    public string ProdTimeDisplay  => ProdTime.HasValue  ? $"{ProdTime:0.##}h"  : "—";
    public string OpTypeDisplay    => OpTypeName ?? "—";
}
