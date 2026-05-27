namespace ShopfloorManager.Desktop.Models;

public record TechDocDto(
    long    Id,
    string  FileTypeCode,
    string  FileTypeName,
    int?    PartRevId,
    int?    PartOpId,
    int?    JobId,
    string? Description,
    string? Revision,
    string? Code,
    string? Segment,
    string? MachineType,
    string  Status,
    string  CreatorName,
    DateTimeOffset CreatedAt)
{
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(Description) ? Description
        : !string.IsNullOrWhiteSpace(Code)        ? Code
        : $"{FileTypeName} ({CreatedAt:dd/MM/yy})";

    public bool IsGcodeType => FileTypeCode is "GCD";

    public string BadgeColor => FileTypeCode switch
    {
        "GCD"                   => "#1565C0",   // blue
        "DRW"                   => "#6A1B9A",   // purple
        "RTC" or "FXT"          => "#E65100",   // orange
        "THD"                   => "#2E7D32",   // green
        "TLS" or "CAM"          => "#00838F",   // teal
        "CAD"                   => "#4527A0",   // deep purple
        _                       => "#6D3B1A",   // BrandPrimary
    };
}
