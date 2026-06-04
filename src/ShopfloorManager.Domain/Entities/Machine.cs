namespace ShopfloorManager.Domain.Entities;

public class Machine : BaseEntity
{
    public string  Code        { get; set; } = string.Empty;
    public string  Name        { get; set; } = string.Empty;
    public string? MachineType { get; set; }   // = MachineGroup.Code (ResourceGroup concept)
    public bool    IsCnc       { get; set; } = true;
    public bool    IsActive    { get; set; } = true;

    // Phase 5 additions
    public int?    FactoryId   { get; set; }
    public string? SerialNumber { get; set; }

    // Navigation
    public MachineGroup? MachineGroup { get; set; }
}
