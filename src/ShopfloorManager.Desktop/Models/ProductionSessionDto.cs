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
    int ClaimedBy,
    int? CancelledBy,
    string? Note);

public record ActiveSessionDto(
    int SessionId,
    string MachineCode,
    int ClaimedBy,
    string ClaimedByName,
    int ProductId,
    string SerialNumber,
    int PartOpId,
    string Status,
    DateTimeOffset ClaimedAt,
    DateTimeOffset? StartedAt,
    int JobId,
    string JobNumber,
    string PartNumber,
    string OpNumber);
