using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Tài liệu kỹ thuật — có 3 loại theo chủ sở hữu:
///
///   1. Part-level  (PartRevId set, PartOpId null)
///      → DRW (bản vẽ 2D), CAD (file 3D) — thuộc Part/Rev, tái dùng qua mọi Job
///
///   2. Standard OP (PartOpId set → OP có RoutingRevId, JobId null)
///      → GCD, TLS, CAM, THD — thuộc công nghệ routing, tái dùng qua mọi Job
///
///   3. ForJobOnly OP (PartOpId set → OP có JobId set, ForJobOnly=true)
///      → RTC, FXT và mọi loại trên OP bất thường chỉ tồn tại 1 Job
///      → Quản lý từ trang Job
///
/// MinIO path (theo spec 05_technical_documents.md):
///   Part-level : {folder}/{part_number}/{revision}/{filename}
///   Standard OP: {folder}/{part_number}/{op_number}/{revision}/{filename}  (GCD)
///   Job+OP     : {folder}/{job_number}/{op_number}/{filename}              (RTC, FXT)
/// </summary>
public class TechDocument
{
    public long Id { get; set; }
    public int FileTypeId { get; set; }

    /// <summary>Set cho tài liệu Part-level (DRW, CAD). Null nếu thuộc OP.</summary>
    public int? PartRevId { get; set; }

    /// <summary>Set cho tài liệu OP-level (GCD, RTC, FXT...). Null nếu thuộc Part.</summary>
    public int? PartOpId { get; set; }

    /// <summary>Set khi PartOp là ForJobOnly — hoặc khi RTC/FXT cần job context.</summary>
    public int? JobId { get; set; }

    /// <summary>MinIO object key — path tương đối trong bucket shopfloor-storage.</summary>
    public string StoragePath { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? Revision { get; set; }

    /// <summary>Mã chương trình G-code (ví dụ "O0020") — group segments cùng code.</summary>
    public string? Code { get; set; }

    /// <summary>Segment G-code (ví dụ "1_3" = segment 1/3). Null nếu không chia segment.</summary>
    public string? Segment { get; set; }

    /// <summary>Loại máy CNC: Fanuc/MAZAK/WC... Chỉ set cho GCD.</summary>
    public string? MachineType { get; set; }

    /// <summary>Kích thước file (bytes) — client gửi kèm lúc request upload.</summary>
    public long? FileSizeBytes { get; set; }

    public FileStatus Status { get; set; } = FileStatus.Pending;
    public int? InspectorId { get; set; }
    public DateTimeOffset? InspectedAt { get; set; }
    public string? InspectNote { get; set; }

    public int CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation
    public FileType FileType { get; set; } = null!;
    public PartRev? PartRev { get; set; }
    public PartOp? PartOp { get; set; }
    public Job? Job { get; set; }
    public User? Inspector { get; set; }
    public User Creator { get; set; } = null!;
}
