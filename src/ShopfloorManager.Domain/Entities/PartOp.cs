namespace ShopfloorManager.Domain.Entities;

public class PartOp
{
    public int Id { get; set; }
    public string OpNumber { get; set; } = string.Empty;
    public decimal? OpNumberSort { get; set; }
    public int? PartId { get; set; }
    public int? OpTypeId { get; set; }
    public int? JobId { get; set; }
    public bool IsForJobOnly { get; set; }
    public string? Description { get; set; }
    public string? Note { get; set; }
    public decimal? SetupTime { get; set; }
    public decimal? ProdTime { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsComplete { get; set; }
    public int? CompletedBy { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Part? Part { get; set; }
    public OpType? OpType { get; set; }
    public Job? Job { get; set; }
}
