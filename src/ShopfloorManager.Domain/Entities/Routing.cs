namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Quy trình gia công cho một PartRev.
/// Một PartRev có thể có nhiều Routing (ví dụ: quy trình tiêu chuẩn, quy trình rework).
/// </summary>
public class Routing
{
    public int Id { get; set; }
    public int PartRevId { get; set; }
    public string Name { get; set; } = "Standard";        // Tên quy trình
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? CreatedBy { get; set; }

    public PartRev PartRev { get; set; } = null!;
    public ICollection<RoutingRev> Revisions { get; set; } = [];
}
