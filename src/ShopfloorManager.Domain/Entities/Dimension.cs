namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Kích thước cần kiểm tra trong một công đoạn.
///
/// Tolerance convention (theo tài liệu 06_dimensions_fai.md):
///   tolerance_plus  ≥ 0  → max_value = nominal + tolerance_plus
///   tolerance_minus ≥ 0  → min_value = nominal − tolerance_minus
///   Ví dụ: ±0.016 → TolerancePlus=0.016, ToleranceMinus=0.016
///   Lệch tâm +0.05/−0.02 → TolerancePlus=0.05, ToleranceMinus=0.02
///
/// Text dimension (is_text_type = true):
///   Không có max/min — operator chọn Pass/Fail thủ công.
///   Lưu vào NominalText thay vì NominalValue.
/// </summary>
public class Dimension
{
    public long Id { get; set; }
    public int PartOpId { get; set; }

    /// <summary>Số bóng trên bản vẽ: "1", "2A", "3B"... UNIQUE trong 1 PartOp.</summary>
    public string BalloonNumber { get; set; } = string.Empty;

    /// <summary>Sort key số — parse từ BalloonNumber (1→1.0, "2A"→2.0, "10"→10.0).</summary>
    public decimal? BalloonSort { get; set; }

    /// <summary>Mã nội bộ (tuỳ chọn): "D1", "L1".</summary>
    public string? Code { get; set; }
    public string? Description { get; set; }

    // ── Numeric dimension ──────────────────────────────────────
    public decimal? NominalValue { get; set; }
    public decimal? TolerancePlus { get; set; }    // ≥ 0
    public decimal? ToleranceMinus { get; set; }   // ≥ 0
    public decimal? MaxValue { get; set; }          // = Nominal + TolerancePlus
    public decimal? MinValue { get; set; }          // = Nominal - ToleranceMinus
    public string Unit { get; set; } = "mm";

    // ── Text dimension ─────────────────────────────────────────
    public bool IsTextType { get; set; }
    public string? NominalText { get; set; }       // "M10x1.5-6H", "Ra 0.8"

    // ── Classification ────────────────────────────────────────
    public int? CategoryId { get; set; }           // → DimensionCategory (LIN/ANG/THD/GEO/SFC)
    public bool IsCritical { get; set; }
    public bool IsFinal { get; set; }              // Kích thước kiểm tra lần cuối sau rework
    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public PartOp PartOp { get; set; } = null!;
    public DimensionCategory? Category { get; set; }
    public ICollection<MeasureValue> MeasureValues { get; set; } = [];
}
