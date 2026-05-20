using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Domain.Entities;

public class MeasureValue
{
    public long Id { get; set; }
    public long DimensionId { get; set; }
    public int ProductId { get; set; }
    public decimal Value { get; set; }
    public MeasureResult Result { get; set; }
    public int? MeasuredBy { get; set; }
    public DateTimeOffset MeasuredAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Note { get; set; }

    public Dimension Dimension { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public User? Inspector { get; set; }
}
