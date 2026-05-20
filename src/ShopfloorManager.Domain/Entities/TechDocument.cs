using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Domain.Entities;

public class TechDocument
{
    public long Id { get; set; }
    public int FileTypeId { get; set; }
    public int? JobId { get; set; }
    public int? PartId { get; set; }
    public int? PartOpId { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Revision { get; set; }
    public string? Code { get; set; }
    public string? Segment { get; set; }
    public FileStatus Status { get; set; } = FileStatus.Pending;
    public int? InspectorId { get; set; }
    public DateTimeOffset? InspectedAt { get; set; }
    public string? InspectNote { get; set; }
    public int CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }

    public FileType FileType { get; set; } = null!;
    public Job? Job { get; set; }
    public Part? Part { get; set; }
    public PartOp? PartOp { get; set; }
    public User? Inspector { get; set; }
    public User? Creator { get; set; }
}
