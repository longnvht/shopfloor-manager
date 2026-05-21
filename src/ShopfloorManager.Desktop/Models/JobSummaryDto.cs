namespace ShopfloorManager.Desktop.Models;

public record JobSummaryDto(
    int Id,
    string JobNumber,
    string PartNumber,
    string RevCode,
    string? RoutingRevCode,
    int? RunQty,
    DateOnly? ShipBy,
    bool IsComplete,
    DateTimeOffset CreatedAt)
{
    public bool IsOverdue => ShipBy.HasValue
        && ShipBy.Value < DateOnly.FromDateTime(DateTime.Today)
        && !IsComplete;

    public string ShipByDisplay => ShipBy.HasValue
        ? ShipBy.Value.ToString("dd/MM/yyyy")
        : "—";

    public string StatusLabel => IsComplete ? "Hoàn thành" : "Đang chạy";
}
