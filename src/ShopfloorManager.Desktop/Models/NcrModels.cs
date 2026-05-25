namespace ShopfloorManager.Desktop.Models;

public record DepartmentLookupDto(int Id, string Code, string Name);

public record NcrReasonDto(int Id, string Name, string? Tag, int? DepartmentId);

public record NcrTriggerArgs(
    int JobId,
    int ProductId,
    int PartOpId,
    string BalloonNumber,
    decimal? MeasuredValue,
    decimal? MinValue,
    decimal? MaxValue,
    string MachineCode);
