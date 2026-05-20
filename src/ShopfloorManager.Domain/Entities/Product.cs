namespace ShopfloorManager.Domain.Entities;

/// <summary>Sản phẩm thực tế (serial). Mỗi Job có RunQty Product.</summary>
public class Product
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;   // "001", "002"...
    public int? SortOrder { get; set; }
    public bool IsComplete { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Job Job { get; set; } = null!;
    public ICollection<MeasureValue> MeasureValues { get; set; } = [];
}
