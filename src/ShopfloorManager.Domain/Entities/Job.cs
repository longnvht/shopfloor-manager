namespace ShopfloorManager.Domain.Entities;

public class Job
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public int? RunQty { get; set; }
    public DateOnly? ShipBy { get; set; }
    public int PartId { get; set; }
    public int? PoLineId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Part Part { get; set; } = null!;
    public PoLine? PoLine { get; set; }
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<PartOp> PartOps { get; set; } = [];
}
