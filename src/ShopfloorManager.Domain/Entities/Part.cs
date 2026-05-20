namespace ShopfloorManager.Domain.Entities;

public class Part : SoftDeletableEntity
{
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Revision { get; set; }
    public string? RoutingRevision { get; set; }
    public int Status { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsComplete { get; set; }
    public int? ConfirmedBy { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public int? CompletedBy { get; set; }

    public ICollection<Job> Jobs { get; set; } = [];
    public ICollection<PartOp> PartOps { get; set; } = [];
}
