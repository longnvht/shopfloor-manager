namespace ShopfloorManager.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public int JobId { get; set; }
    public bool IsComplete { get; set; }
    public int? SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Job Job { get; set; } = null!;
}
