namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Nhóm máy theo loại (LLA40, MIL, SLA...) — tương đương Epicor ResourceGroup.
/// Machine.MachineType khớp với MachineGroup.Code.
/// </summary>
public class MachineGroup : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Machine> Machines { get; set; } = [];
}
