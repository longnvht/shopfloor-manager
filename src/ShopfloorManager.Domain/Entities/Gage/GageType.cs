namespace ShopfloorManager.Domain.Entities;

/// <summary>Loại dụng cụ đo: Micrometer, Caliper, Ring Gauge...</summary>
public class GageType : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>FK → GageCategory — filter gage phù hợp khi operator đo.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Quy trình hiệu chuẩn mặc định cho loại này.</summary>
    public int? DefaultProcedureId { get; set; }

    public GageCategory? Category { get; set; }
    public CalibProcedure? DefaultProcedure { get; set; }
    public ICollection<Gage> Gages { get; set; } = [];
}
