using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Parts;

public record PartDto(
    int Id,
    string PartNumber,
    string Description,
    string? Revision,
    string? RoutingRevision,
    bool IsActive,
    bool IsComplete,
    int Status,
    DateTimeOffset CreatedAt)
{
    public static PartDto From(Part p) => new(
        p.Id, p.PartNumber, p.Description, p.Revision,
        p.RoutingRevision, p.IsActive, p.IsComplete, p.Status, p.CreatedAt);
}
