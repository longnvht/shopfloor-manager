namespace ShopfloorManager.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public int? MachineId { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset? LoggedInAt { get; set; }
    public DateTimeOffset? LoggedOutAt { get; set; }

    public User? User { get; set; }
}
