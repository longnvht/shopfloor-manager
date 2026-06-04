namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Dụng cụ đo cụ thể — có GageNo duy nhất, theo dõi trạng thái và vị trí.
///
/// Status codes: VALID | EXPIRED | DAMAGED | BORROWED | CALIB
///   VALID:    Trong hạn hiệu chuẩn, sử dụng được
///   EXPIRED:  Quá hạn hiệu chuẩn
///   DAMAGED:  Hư hỏng
///   BORROWED: Đang được mượn (is_valid=true)
///   CALIB:    Đang gửi hiệu chuẩn
/// </summary>
public class Gage : BaseEntity
{
    public string GageNo { get; set; } = string.Empty;   // UNIQUE, không đổi sau khi tạo
    public string? SerialNo { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? MeasuringRange { get; set; }
    public string? Accuracy { get; set; }
    public string Unit { get; set; } = "mm";
    public string? Manufacturer { get; set; }

    // Calibration timing
    public int? CalibFrequencyDays { get; set; }
    public DateOnly? LastCalibration { get; set; }
    public DateOnly? InServiceDate { get; set; }

    // Status — string code (VALID/EXPIRED/DAMAGED/BORROWED/CALIB)
    public string StatusCode { get; set; } = GageStatusCode.Valid;
    public bool IsValid => StatusCode is GageStatusCode.Valid or GageStatusCode.Borrowed;

    // Computed due date
    public DateOnly? DueDate => LastCalibration.HasValue && CalibFrequencyDays.HasValue
        ? LastCalibration.Value.AddDays(CalibFrequencyDays.Value)
        : null;

    public int? DaysRemaining => DueDate.HasValue
        ? DueDate.Value.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber
        : null;

    // Type FK
    public int? GageTypeId { get; set; }

    // Location FKs
    public int? DefaultLocationId { get; set; }
    public int? DefaultSlotId { get; set; }
    public int? CurrentLocationId { get; set; }
    public int? CurrentSlotId { get; set; }

    // Calibration vendor
    public int? VendorId { get; set; }

    // Denorm flags — cập nhật khi borrow/return/calib thay đổi
    public bool IsBorrowed { get; set; }
    public bool HasPendingCalib { get; set; }

    public string? Note { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;

    // Navigation
    public GageType? GageType { get; set; }
    public GageLocation? DefaultLocation { get; set; }
    public GageSlot? DefaultSlot { get; set; }
    public GageLocation? CurrentLocation { get; set; }
    public GageSlot? CurrentSlot { get; set; }
    public CalibVendor? Vendor { get; set; }
    public ICollection<BorrowTransaction> BorrowTransactions { get; set; } = [];
    public ICollection<CalibRequest> CalibRequests { get; set; } = [];
}

public static class GageStatusCode
{
    public const string Valid    = "VALID";
    public const string Expired  = "EXPIRED";
    public const string Damaged  = "DAMAGED";
    public const string Borrowed = "BORROWED";
    public const string Calib    = "CALIB";
}
