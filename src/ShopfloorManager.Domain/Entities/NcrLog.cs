using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Domain.Entities;

public class NcrLog
{
    public int Id { get; set; }
    public long NcrId { get; set; }
    public NcrAction Action { get; set; }
    public string? Note { get; set; }
    public int ActionBy { get; set; }
    public DateTimeOffset ActionAt { get; set; } = DateTimeOffset.UtcNow;

    public Ncr Ncr { get; set; } = null!;
    public User Actor { get; set; } = null!;
}
