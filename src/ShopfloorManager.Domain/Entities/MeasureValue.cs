using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Giá trị đo thực tế của một Dimension cho một Product.
/// Upsert per (DimensionId, ProductId) — đo lại sẽ ghi đè.
/// </summary>
public class MeasureValue
{
    public long Id { get; set; }
    public long DimensionId { get; set; }
    public int ProductId { get; set; }
    public int PartOpId { get; set; }           // Denorm từ Dimension.PartOpId — để query nhanh hơn
    public decimal Value { get; set; }
    public MeasureResult Result { get; set; }   // Pass=1, Fail=2
    public int? MeasuredBy { get; set; }
    public DateTimeOffset MeasuredAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Note { get; set; }

    public Dimension Dimension { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public PartOp PartOp { get; set; } = null!;
    public User? Inspector { get; set; }
}
