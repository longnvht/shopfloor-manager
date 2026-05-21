using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Non-Conformance Report — sự kiện không phù hợp.
/// Format NcrNumber: NCR-{YY}-{NNNN} (ví dụ: NCR-26-0001). Reset sequence mỗi năm.
/// </summary>
public class Ncr
{
    public long Id { get; set; }
    public string NcrNumber { get; set; } = string.Empty;  // NCR-26-0001
    public int YearCode { get; set; }                      // 26
    public int Sequence { get; set; }                      // 1

    public int JobId { get; set; }
    public int? ProductId { get; set; }
    public int? PartOpId { get; set; }
    public long? MeasureValueId { get; set; }              // MeasureValue gây ra fail

    // ── Classification ────────────────────────────────────────
    public int? ReasonId { get; set; }                     // → NcrReason
    public int? DepartmentId { get; set; }                 // Phòng ban chịu trách nhiệm
    public string? MachineCode { get; set; }               // Máy nơi phát sinh (text)

    public string Description { get; set; } = string.Empty;
    public NcrStatus Status { get; set; } = NcrStatus.Open;
    public int RaisedBy { get; set; }
    public DateTimeOffset RaisedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? ClosedBy { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    public Job Job { get; set; } = null!;
    public Product? Product { get; set; }
    public PartOp? PartOp { get; set; }
    public NcrReason? Reason { get; set; }
    public User Raiser { get; set; } = null!;
    public User? Closer { get; set; }
    public ICollection<NcrLog> Logs { get; set; } = [];
}
