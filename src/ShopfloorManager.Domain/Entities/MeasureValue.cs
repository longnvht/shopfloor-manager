using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Kết quả đo một kích thước cho một serial.
///
/// KHÔNG upsert — mỗi lần đo tạo một record mới để giữ lịch sử đầy đủ.
/// "Giá trị hiện tại" = record mới nhất theo MeasuredAt.
///
/// Pass/Fail: min_value ≤ value ≤ max_value (lấy từ Dimension).
/// Text dimension: operator chọn Pass/Fail thủ công (value = NULL).
/// </summary>
public class MeasureValue
{
    public long Id { get; set; }
    public long DimensionId { get; set; }
    public int ProductId { get; set; }
    public int PartOpId { get; set; }            // Denorm từ Dimension.PartOpId

    public decimal? Value { get; set; }          // NULL nếu IsTextType
    public MeasureResult Result { get; set; }    // Pass=1, Fail=2
    public string? Note { get; set; }

    public int? MeasuredBy { get; set; }
    public DateTimeOffset MeasuredAt { get; set; } = DateTimeOffset.UtcNow;

    // ── Measurement stage ─────────────────────────────────────
    /// <summary>Giai đoạn đo: InprocessFAI=0, QCInline=1, QCFinal=2.</summary>
    public MeasureStage MeasureStage { get; set; } = MeasureStage.InprocessFAI;

    // ── Final inspection (legacy — deprecated, dùng MeasureStage=QCFinal thay thế) ──
    public bool IsFinal { get; set; }
    public int? FinalOpId { get; set; }          // OP final inspection

    // ── NCR tracking ──────────────────────────────────────────
    public bool HasNcr { get; set; }
    public string? NcrCode { get; set; }         // Snapshot NCR code khi fail

    // ── Phase 4/5 (nullable, thêm sau) ───────────────────────
    public int? MachineId { get; set; }          // Máy nào đang gia công
    public int? GageId { get; set; }             // Dụng cụ đo

    public int? UpdatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Dimension Dimension { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public PartOp PartOp { get; set; } = null!;
    public User? Inspector { get; set; }
}
