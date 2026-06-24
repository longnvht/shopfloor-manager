namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Nhóm phương pháp đo rộng (LIN/ANG/THD/GEO/SFC) — gắn trên GageType, phục vụ phân nhóm/quản lý
/// Gage: GageCategory → GageType → Gage. KHÔNG gắn trực tiếp trên Dimension (Dimension chỉ lưu
/// GageTypeId — tránh trùng lặp dữ liệu, xem 06_dimensions_fai.md §3.6).
/// </summary>
public class GageCategory
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;  // LIN, ANG, THD, GEO, SFC
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<GageType> GageTypes { get; set; } = [];
}
