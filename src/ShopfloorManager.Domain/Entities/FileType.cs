namespace ShopfloorManager.Domain.Entities;

public class FileType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;   // DRAWING, GCODE, ROUTECARD...
    public string Name { get; set; } = string.Empty;
    public string? Folder { get; set; }
    public bool IsGcode { get; set; }
    public bool RequiresOpNumber { get; set; }
    public int SortOrder { get; set; }

    public ICollection<TechDocument> TechDocuments { get; set; } = [];
}
