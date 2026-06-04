using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Domain.Entities;

/// <summary>Nhà cung cấp dịch vụ hiệu chuẩn bên ngoài.</summary>
public class CalibVendor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Contact { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public ICollection<CalibRequest> CalibRequests { get; set; } = [];
}

/// <summary>Quy trình hiệu chuẩn tiêu chuẩn cho từng loại dụng cụ.</summary>
public class CalibProcedure : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Revision { get; set; }
    public DateOnly? RevDate { get; set; }
    public string? DocLink { get; set; }
    public bool IsLatest { get; set; } = true;

    public ICollection<CalibRecord> CalibRecords { get; set; } = [];
}

/// <summary>
/// Yêu cầu hiệu chuẩn — pending → approved → completed (hoặc cancelled).
/// Chỉ tạo được khi gage chưa có request pending/approved.
/// </summary>
public class CalibRequest : BaseEntity
{
    public int GageId { get; set; }
    public int? VendorId { get; set; }
    public DateOnly RequestDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public CalibRequestStatus Status { get; set; } = CalibRequestStatus.Pending;
    public int CreatedBy { get; set; }

    // Navigation
    public Gage Gage { get; set; } = null!;
    public CalibVendor? Vendor { get; set; }
    public User Creator { get; set; } = null!;
    public CalibRecord? Record { get; set; }
}

/// <summary>
/// Kết quả hiệu chuẩn — ghi lại sau khi hoàn thành.
/// Tạo record → tự động cập nhật gage.last_calibration + status = VALID.
/// </summary>
public class CalibRecord : BaseEntity
{
    public int CalibRequestId { get; set; }
    public int? ProcedureId { get; set; }
    public string? CalibratedBy { get; set; }             // Tên người thực hiện (text)
    public DateOnly CalibrationDate { get; set; }
    public string? AsFoundConditions { get; set; }        // Pass / Fail / Out of tolerance
    public decimal? AdjustmentMade { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? Humidity { get; set; }
    public string? StoragePath { get; set; }              // PDF chứng chỉ trên MinIO
    public int CreatedBy { get; set; }

    // Navigation
    public CalibRequest CalibRequest { get; set; } = null!;
    public CalibProcedure? Procedure { get; set; }
    public User Creator { get; set; } = null!;
}
