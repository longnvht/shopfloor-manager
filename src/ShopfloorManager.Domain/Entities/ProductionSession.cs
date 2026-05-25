namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Phiên gia công của 1 sản phẩm tại 1 công đoạn trên 1 máy.
/// Constraint: mỗi product chỉ có tối đa 1 session status=open tại bất kỳ thời điểm.
/// </summary>
public class ProductionSession
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int PartOpId { get; set; }
    public string MachineCode { get; set; } = string.Empty;

    /// <summary>open | complete | cancelled</summary>
    public string Status { get; set; } = SessionStatus.Open;

    public DateTimeOffset ClaimedAt { get; set; }   // Khi operator chọn product
    public DateTimeOffset? StartedAt { get; set; }  // Khi bấm "Bắt đầu"
    public DateTimeOffset? CompletedAt { get; set; } // Khi bấm "Kết thúc"

    public int ClaimedBy { get; set; }              // UserId operator claim session
    public int? CancelledBy { get; set; }           // UserId supervisor unlock
    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Product Product { get; set; } = null!;
    public PartOp PartOp { get; set; } = null!;
    public User ClaimedByUser { get; set; } = null!;
    public User? CancelledByUser { get; set; }
}

public static class SessionStatus
{
    public const string Open      = "open";
    public const string Complete  = "complete";
    public const string Cancelled = "cancelled";
}
