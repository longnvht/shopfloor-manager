using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Domain.Entities;

/// <summary>Giao dịch mượn/trả dụng cụ đo.</summary>
public class BorrowTransaction : BaseEntity
{
    public int GageId { get; set; }
    public int BorrowerId { get; set; }
    public int ManagerId { get; set; }

    public DateOnly BorrowDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ExpectedReturnDate { get; set; }
    public DateOnly? ReturnDate { get; set; }

    /// <summary>Vị trí gage lúc cho mượn (để biết cần trả về đâu).</summary>
    public int? FromLocationId { get; set; }
    public int? FromSlotId { get; set; }

    /// <summary>Vị trí sử dụng (máy CNC nào, phòng nào).</summary>
    public int? UseLocationId { get; set; }

    public BorrowStatus Status { get; set; } = BorrowStatus.Active;
    public string? Note { get; set; }

    // Navigation
    public Gage Gage { get; set; } = null!;
    public User Borrower { get; set; } = null!;
    public User Manager { get; set; } = null!;
    public GageLocation? FromLocation { get; set; }
    public GageLocation? UseLocation { get; set; }
}
