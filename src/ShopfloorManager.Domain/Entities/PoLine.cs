namespace ShopfloorManager.Domain.Entities;

public class PoLine
{
    public int Id { get; set; }
    public string? PoNumber { get; set; }
    public string? PoLineNumber { get; set; }
    public int? CustomerId { get; set; }

    public ICollection<Job> Jobs { get; set; } = [];
}
