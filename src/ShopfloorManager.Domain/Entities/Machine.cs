namespace ShopfloorManager.Domain.Entities;

public class Machine : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? MachineType { get; set; }
    public bool IsCnc { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
