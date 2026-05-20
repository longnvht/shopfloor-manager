namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Công đoạn gia công. Thuộc về một RoutingRev cụ thể.
/// ForJobOnly=true + JobId có giá trị → OP bổ sung riêng cho một Job nhất định.
/// </summary>
public class PartOp
{
    public int Id { get; set; }
    public int? RoutingRevId { get; set; }     // NULL nếu ForJobOnly
    public int? JobId { get; set; }            // Không NULL nếu ForJobOnly=true
    public bool ForJobOnly { get; set; }       // true = OP riêng cho Job này, không thuộc routing template
    public string OpNumber { get; set; } = string.Empty;  // "10", "20", "25"...
    public decimal? OpNumberSort { get; set; } // Dùng để sort: "10"→10.0, "10.1"→10.1
    public int? OpTypeId { get; set; }
    public string? Description { get; set; }
    public string? Note { get; set; }
    public decimal? SetupTime { get; set; }    // Giờ setup máy
    public decimal? ProdTime { get; set; }     // Giờ gia công mỗi cái
    public bool IsVisible { get; set; } = true;
    public bool IsComplete { get; set; }
    public int? CompletedBy { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? CreatedBy { get; set; }

    public RoutingRev? RoutingRev { get; set; }
    public Job? Job { get; set; }
    public OpType? OpType { get; set; }
    public ICollection<Dimension> Dimensions { get; set; } = [];
    public ICollection<TechDocument> TechDocuments { get; set; } = [];
}
