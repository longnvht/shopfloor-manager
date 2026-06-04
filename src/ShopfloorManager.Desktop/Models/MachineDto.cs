namespace ShopfloorManager.Desktop.Models;

public record MachineDto(int Id, string Code, string Name, string? MachineType, bool IsCnc);
