namespace ShopfloorManager.Domain.Entities;

/// <summary>Khu vực lưu trữ dụng cụ đo: Phòng QC, Tủ Dụng cụ Xưởng A...</summary>
public class GageLocation : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<GageSlot> Slots { get; set; } = [];
}

/// <summary>Vị trí cụ thể trong GageLocation: Ngăn A1, Khay 3...</summary>
public class GageSlot : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int LocationId { get; set; }

    public GageLocation Location { get; set; } = null!;
}
