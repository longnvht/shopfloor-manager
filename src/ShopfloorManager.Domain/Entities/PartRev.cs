namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Phiên bản thiết kế của một Part (Rev A, Rev B...).
/// Mỗi lần thay đổi bản vẽ → tạo PartRev mới, deactivate cái cũ.
/// </summary>
public class PartRev
{
    public int Id { get; set; }
    public int PartId { get; set; }
    public string RevCode { get; set; } = string.Empty;    // "A", "B", "C"...
    public string? Description { get; set; }               // Mô tả thay đổi so với rev trước
    public bool IsActive { get; set; } = true;
    public bool IsReleased { get; set; }                   // Đã được duyệt phát hành chưa
    public int? ReleasedBy { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? CreatedBy { get; set; }

    public Part Part { get; set; } = null!;
    public ICollection<Routing> Routings { get; set; } = [];
    public ICollection<TechDocument> TechDocuments { get; set; } = [];
}
