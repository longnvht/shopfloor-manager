namespace ShopfloorManager.Domain.Entities;

public class FileType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Folder { get; set; }
    public bool IsGcode { get; set; }
    public bool IsSegment { get; set; }
    public bool RequiresJobNumber { get; set; } = true;
    public bool RequiresPartNumber { get; set; } = true;
    public bool RequiresOpNumber { get; set; }
    public bool RequiresRevision { get; set; }
    public int SortOrder { get; set; }

    public ICollection<TechDocument> TechDocuments { get; set; } = [];
}
