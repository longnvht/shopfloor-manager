namespace ShopfloorManager.Domain.Entities;

public class OpType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }

    public ICollection<PartOp> PartOps { get; set; } = [];
}
