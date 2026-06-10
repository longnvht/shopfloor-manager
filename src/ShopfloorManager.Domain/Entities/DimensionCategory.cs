namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Phương pháp đo kiểm — filter gage phù hợp khi operator chọn dụng cụ đo.
/// Seed: LIN, ANG, THD, GEO, SFC.
/// </summary>
public class DimensionCategory
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;  // LIN, ANG, THD, GEO, SFC
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Dimension> Dimensions { get; set; } = [];
}
