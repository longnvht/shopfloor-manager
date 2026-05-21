namespace ShopfloorManager.Domain.Entities;

/// <summary>Danh mục lý do không phù hợp, mỗi lý do gắn với phòng ban chịu trách nhiệm.</summary>
public class NcrReason
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;     // "Tool wear", "Setup error"...
    public string? Tag { get; set; }                     // Short tag (tuỳ chọn)
    public int? DepartmentId { get; set; }               // Phòng ban chịu trách nhiệm
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Ncr> Ncrs { get; set; } = [];
}
