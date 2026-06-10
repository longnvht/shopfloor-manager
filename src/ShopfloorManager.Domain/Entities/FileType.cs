namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Loại tài liệu kỹ thuật. Các flags điều khiển naming convention và MinIO path.
/// Phân tích từ bảng filestype trong legacy ManageData.
/// </summary>
public class FileType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;    // "RC", "GC", "FD", "DRW"...
    public string Name { get; set; } = string.Empty;    // "Route Card", "G-Code"...
    public string? Folder { get; set; }                 // MinIO subfolder: "routecard", "gcode"

    // ── Naming / path flags (từ filestype legacy) ─────────────
    /// <summary>File có thể chia thành nhiều segment (G-code nhiều phần).</summary>
    public bool IsSegment { get; set; }
    /// <summary>G-code file — cần thêm MachineType (Fanuc/MAZAK/WC).</summary>
    public bool IsGcode { get; set; }
    /// <summary>MinIO path bắt đầu bằng PartNumber (thay vì JobNumber).</summary>
    public bool IsPartNumber { get; set; } = true;
    /// <summary>MinIO path bao gồm RevCode của PartRev.</summary>
    public bool IsRevision { get; set; } = true;
    /// <summary>MinIO path bao gồm OpNumber.</summary>
    public bool IsOpNumber { get; set; }
    /// <summary>MinIO path bắt đầu bằng JobNumber (thay vì PartNumber).</summary>
    public bool IsJobNumber { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<TechDocument> TechDocuments { get; set; } = [];
}
