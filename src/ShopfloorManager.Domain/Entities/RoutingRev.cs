namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Phiên bản của một Routing (R1, R2...).
/// Mỗi lần thay đổi quy trình (thêm/bớt/sửa công đoạn) → tạo RoutingRev mới.
/// Job lưu snapshot RoutingRevId → không bị ảnh hưởng khi routing thay đổi sau này.
/// </summary>
public class RoutingRev
{
    public int Id { get; set; }
    public int RoutingId { get; set; }
    public string RevCode { get; set; } = string.Empty;   // "R1", "R2"...
    public string? ChangeNote { get; set; }               // Lý do thay đổi
    public bool IsActive { get; set; } = true;
    public bool IsReleased { get; set; }
    public int? ReleasedBy { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? CreatedBy { get; set; }

    public Routing Routing { get; set; } = null!;
    public ICollection<PartOp> PartOps { get; set; } = [];
}
