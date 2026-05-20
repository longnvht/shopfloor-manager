using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Tài liệu kỹ thuật gắn với PartOp (RouteCard, G-code, FixtureDrawing...).
/// Có thể gắn với Job nếu là tài liệu riêng của một lệnh SX.
/// </summary>
public class TechDocument
{
    public long Id { get; set; }
    public int FileTypeId { get; set; }
    public int PartOpId { get; set; }             // OP nào
    public int? JobId { get; set; }               // Không null nếu tài liệu riêng cho Job
    public string StoragePath { get; set; } = string.Empty;  // MinIO object key
    public string? Description { get; set; }
    public string? Revision { get; set; }
    public string? Code { get; set; }             // Số chương trình (G-code)
    public string? Segment { get; set; }          // Segment A/B (G-code nhiều segment)
    public FileStatus Status { get; set; } = FileStatus.Pending;
    public int? InspectorId { get; set; }
    public DateTimeOffset? InspectedAt { get; set; }
    public string? InspectNote { get; set; }
    public int CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }

    public FileType FileType { get; set; } = null!;
    public PartOp PartOp { get; set; } = null!;
    public Job? Job { get; set; }
    public User? Inspector { get; set; }
    public User Creator { get; set; } = null!;
}
