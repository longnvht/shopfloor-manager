namespace ShopfloorManager.Domain.Entities;

public class Menu
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public short Level { get; set; }
    public int SortOrder { get; set; }

    public Menu? Parent { get; set; }
    public ICollection<Menu> Children { get; set; } = [];
    public ICollection<RoleMenu> RoleMenus { get; set; } = [];
}
