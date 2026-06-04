namespace ShopfloorManager.Infrastructure.Mqtt;

/// <summary>Payload JSON từ MDC Agent qua MQTT topic factory/cnc/{code}</summary>
public record CncPayload(
    string MachineCode,
    DateTimeOffset Timestamp,
    string? TmMode,
    string? AtMode,
    string? RunMode,
    string? Alarm,
    string? AlarmMessage,
    int? SpindleSpeed,
    int? SpindleLoad,
    int? Feedrate,
    int? FeedOverride,
    double? XPosition,
    double? YPosition,
    double? ZPosition,
    string? Program,
    int? PartCount,
    int? ToolNumber
);

/// <summary>DTO broadcast qua SignalR → web client</summary>
public record MachineStatusDto(
    int     MachineId,
    string  MachineCode,
    string? MachineName,
    string? RunMode,
    string? TmMode,
    string? AlarmMessage,
    int?    SpindleSpeed,
    int?    SpindleLoad,
    int?    Feedrate,
    double? XPosition,
    double? YPosition,
    double? ZPosition,
    string? Program,
    int?    PartCount,
    DateTimeOffset LastSeen
);
