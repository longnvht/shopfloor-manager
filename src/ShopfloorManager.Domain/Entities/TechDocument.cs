using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Tài liệu kỹ thuật gắn với PartOp (RouteCard, G-code, FixtureDrawing...).
/// Có thể gắn thêm JobId nếu là tài liệu riêng cho một lệnh SX.
///
/// MinIO path convention (từ phân tích FormUpdateTechnology.cs):
///   IsPartNumber: {PartNumber}/{RevCode}/{RoutingRevCode}/{OpNumber}/{Folder}/{filename}
///   IsJobNumber:  {JobNumber}/{OpNumber}/{Folder}/{filename}
///
/// Upload rules:
///   - Block nếu Status=Approved (kể cả creator)
///   - Block nếu Status=Pending + CreatedBy ≠ current user
///   - Nếu Status=Rejected → rename file cũ, upload mới, reset Pending
/// </summary>
public class TechDocument
{
    public long Id { get; set; }
    public int FileTypeId { get; set; }
    public int PartOpId { get; set; }

    /// <summary>Không null nếu tài liệu upload cho Job cụ thể (IsJobNumber path).</summary>
    public int? JobId { get; set; }

    /// <summary>MinIO object key — full path trong bucket shopfloor-storage.</summary>
    public string StoragePath { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? Revision { get; set; }

    /// <summary>
    /// Mã chương trình G-code (ví dụ: "O0020").
    /// Dùng để group các segments lại với nhau.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Segment của G-code (ví dụ: "1_3" = segment 1 trong 3).
    /// Null nếu file không có segment.
    /// </summary>
    public string? Segment { get; set; }

    /// <summary>
    /// Loại máy CNC cho G-code (Fanuc/MAZAK/WC/FMAZAK...).
    /// Null cho các loại tài liệu khác (RouteCard, Drawing...).
    /// </summary>
    public string? MachineType { get; set; }

    public FileStatus Status { get; set; } = FileStatus.Pending;
    public int? InspectorId { get; set; }
    public DateTimeOffset? InspectedAt { get; set; }
    public string? InspectNote { get; set; }

    public int CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Soft delete — file bị xoá nhưng lịch sử vẫn giữ.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    public FileType FileType { get; set; } = null!;
    public PartOp PartOp { get; set; } = null!;
    public Job? Job { get; set; }
    public User? Inspector { get; set; }
    public User Creator { get; set; } = null!;
}
