namespace ShopfloorManager.Domain.Entities;

public class WorkStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsWorking { get; set; } = true;

    public ICollection<User> Users { get; set; } = [];
}
