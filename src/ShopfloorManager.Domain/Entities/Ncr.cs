using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Domain.Entities;

public class Ncr
{
    public long Id { get; set; }
    public string NcrNumber { get; set; } = string.Empty;
    public int JobId { get; set; }
    public int? ProductId { get; set; }
    public int? PartOpId { get; set; }
    public int? DepartmentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public NcrStatus Status { get; set; } = NcrStatus.Open;
    public int RaisedBy { get; set; }
    public DateTimeOffset RaisedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? ClosedBy { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    public Job Job { get; set; } = null!;
    public Product? Product { get; set; }
    public PartOp? PartOp { get; set; }
    public User Raiser { get; set; } = null!;
    public User? Closer { get; set; }
    public ICollection<NcrLog> Logs { get; set; } = [];
}
