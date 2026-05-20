namespace ShopfloorManager.Domain.Entities;

/// <summary>Loại sản phẩm — chỉ lưu PartNumber và mô tả chung.</summary>
public class Part
{
    public int Id { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? CreatedBy { get; set; }

    public ICollection<PartRev> Revisions { get; set; } = [];
}
