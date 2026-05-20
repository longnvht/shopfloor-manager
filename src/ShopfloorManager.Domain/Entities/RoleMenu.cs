namespace ShopfloorManager.Domain.Entities;

public class RoleMenu
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int MenuId { get; set; }

    public Role Role { get; set; } = null!;
    public Menu Menu { get; set; } = null!;
}
