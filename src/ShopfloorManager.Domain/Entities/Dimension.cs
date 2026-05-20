namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Kích thước cần kiểm tra trong một công đoạn.
/// BalloonNumber: số bóng trên bản vẽ (ví dụ "Ø1", "L2", "Ra3").
/// </summary>
public class Dimension
{
    public long Id { get; set; }
    public int PartOpId { get; set; }
    public string BalloonNumber { get; set; } = string.Empty;  // Số bóng trên bản vẽ: "Ø1", "L2"
    public string? Code { get; set; }                          // Mã nội bộ: "D1", "L1"
    public string? Description { get; set; }
    public decimal Nominal { get; set; }
    public decimal UpperTol { get; set; }
    public decimal LowerTol { get; set; }                      // Thường âm: -0.016
    public string Unit { get; set; } = "mm";
    public bool IsCritical { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? CreatedBy { get; set; }

    // Computed helpers (ignored by EF Core)
    public decimal UpperLimit => Nominal + UpperTol;
    public decimal LowerLimit => Nominal + LowerTol;

    public PartOp PartOp { get; set; } = null!;
    public ICollection<MeasureValue> MeasureValues { get; set; } = [];
}
