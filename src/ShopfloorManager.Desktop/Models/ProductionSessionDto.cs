namespace ShopfloorManager.Desktop.Models;

public record ProductionSessionDto(
    int Id,
    int ProductId,
    string SerialNumber,
    int PartOpId,
    string MachineCode,
    string Status,
    DateTimeOffset ClaimedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int? CancelledBy,
    string? Note);
