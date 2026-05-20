namespace ShopfloorManager.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string UserLogin { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Sex { get; set; }
    public string? Email { get; set; }
    public int? UserTypeId { get; set; }
    public int? PositionId { get; set; }
    public int? WorkStatusId { get; set; }
    public int? RoleId { get; set; }
    public int? MesRoleId { get; set; }
    public bool FirstLogin { get; set; } = true;
    public string? ResetCode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public UserType? UserType { get; set; }
    public Position? Position { get; set; }
    public WorkStatus? WorkStatus { get; set; }
    public Role? Role { get; set; }
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
