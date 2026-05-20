namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Lệnh sản xuất — snapshot của PartRev + RoutingRev tại thời điểm phát lệnh.
/// RoutingRevId KHÔNG thay đổi dù routing sau này được cập nhật.
/// </summary>
public class Job
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public int PartRevId { get; set; }         // Snapshot: dùng PartRev nào
    public int RoutingRevId { get; set; }      // Snapshot: dùng RoutingRev nào
    public int? PoLineId { get; set; }
    public int? RunQty { get; set; }
    public DateOnly? ShipBy { get; set; }
    public bool IsComplete { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? CreatedBy { get; set; }

    public PartRev PartRev { get; set; } = null!;
    public RoutingRev RoutingRev { get; set; } = null!;
    public PoLine? PoLine { get; set; }
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<PartOp> ForJobOnlyOps { get; set; } = [];  // OPs riêng của Job này
}
