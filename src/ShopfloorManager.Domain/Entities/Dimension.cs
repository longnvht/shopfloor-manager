namespace ShopfloorManager.Domain.Entities;

public class Dimension
{
    public long Id { get; set; }
    public int PartOpId { get; set; }
    public string Code { get; set; } = string.Empty;       // D1, L1, Ø1...
    public string? Description { get; set; }
    public decimal Nominal { get; set; }
    public decimal UpperTol { get; set; }
    public decimal LowerTol { get; set; }
    public string Unit { get; set; } = "mm";
    public bool IsCritical { get; set; }
    public int SortOrder { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public decimal UpperLimit => Nominal + UpperTol;
    public decimal LowerLimit => Nominal + LowerTol;        // LowerTol is typically negative

    public PartOp PartOp { get; set; } = null!;
    public ICollection<MeasureValue> MeasureValues { get; set; } = [];
}
