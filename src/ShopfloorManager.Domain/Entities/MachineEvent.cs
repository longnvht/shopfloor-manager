namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Ghi nhận khi trạng thái máy thay đổi (IDLE↔RUNNING↔ALARM↔OFF).
/// Không ghi mỗi giây — chỉ ghi khi state change.
/// Dữ liệu spindle/feedrate/position KHÔNG lưu DB — chỉ cache SignalR.
/// </summary>
public class MachineEvent
{
    public long Id { get; set; }
    public int MachineId { get; set; }
    public int? CreatedBy { get; set; }       // null = tự động từ MQTT
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? TmMode       { get; set; } // MANUAL | AUTO | MDI
    public string? AtMode       { get; set; } // MEMORY | TAPE | MDI
    public string? RunMode      { get; set; } // RESET | START | ACTIVE
    public string? Alarm        { get; set; } // mã alarm
    public string? AlarmMessage { get; set; } // mô tả alarm

    // Navigation
    public Machine Machine { get; set; } = null!;
}
