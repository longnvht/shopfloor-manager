using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Jobs;

public record JobDto(
    int Id,
    string JobNumber,
    int PartId,
    string PartNumber,
    string PartDescription,
    string? PartRevision,
    int? RunQty,
    DateOnly? ShipBy,
    DateTimeOffset CreatedAt);

public record JobDetailDto(
    int Id,
    string JobNumber,
    int PartId,
    string PartNumber,
    string PartDescription,
    string? PartRevision,
    int? RunQty,
    DateOnly? ShipBy,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PartOpSummary> Operations,
    IReadOnlyList<ProductSummary> Products);

public record PartOpSummary(int Id, string OpNumber, string? OpTypeName, string? Description, bool IsComplete);
public record ProductSummary(int Id, string SerialNumber, bool IsComplete);
