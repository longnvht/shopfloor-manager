namespace ShopfloorManager.Desktop.Models;

public record ProductWithSessionDto(
    int ProductId,
    string SerialNumber,
    int SortOrder,
    int? SessionId,
    string? SessionStatus,
    string? MachineCode,
    DateTimeOffset? ClaimedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt)
{
    public string DisplayStatus => SessionStatus switch
    {
        "open" when StartedAt.HasValue  => "Đang gia công",
        "open" when !StartedAt.HasValue => "Đã chọn",
        "complete"                      => "Hoàn thành",
        _                               => "Sẵn sàng"
    };

    public string StatusCode => SessionStatus switch
    {
        "open" when StartedAt.HasValue  => "inprogress",
        "open" when !StartedAt.HasValue => "claimed",
        "complete"                      => "complete",
        _                               => "available"
    };

    public bool IsAvailable => SessionStatus is null;

    public string MachineDisplay => MachineCode ?? string.Empty;

    public string ElapsedDisplay
    {
        get
        {
            if (StatusCode == "complete" && StartedAt.HasValue && CompletedAt.HasValue)
            {
                var span = CompletedAt.Value - StartedAt.Value;
                return $"{(int)span.TotalMinutes:D2}:{span.Seconds:D2}";
            }
            if (StatusCode == "inprogress" && StartedAt.HasValue)
            {
                var span = DateTimeOffset.UtcNow - StartedAt.Value;
                return $"{(int)span.TotalMinutes:D2}:{span.Seconds:D2} ⏱";
            }
            return string.Empty;
        }
    }
}
