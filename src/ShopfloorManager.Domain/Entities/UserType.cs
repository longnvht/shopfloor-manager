namespace ShopfloorManager.Domain.Entities;

public class UserType
{
    public int Id { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool CanEnterValue { get; set; }
    public bool CanRaiseNcr { get; set; }

    public ICollection<User> Users { get; set; } = [];
}
