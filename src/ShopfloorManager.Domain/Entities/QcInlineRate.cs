namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Mức kiểm QC Inline (% sản phẩm QC kiểm ngẫu nhiên trên chuyền).
/// JobId=null + PartOpId=null = mặc định toàn nhà máy (luôn tồn tại, không cho ẩn).
/// Độ ưu tiên resolve: (JobId,PartOpId) > (JobId,null) > (null,PartOpId) > (null,null).
/// </summary>
public class QcInlineRate : BaseEntity
{
    public int? JobId { get; set; }
    public int? PartOpId { get; set; }
    public decimal RatePercent { get; set; }
    public bool IsActive { get; set; } = true;
}
