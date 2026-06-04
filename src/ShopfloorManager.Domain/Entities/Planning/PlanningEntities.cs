namespace ShopfloorManager.Domain.Entities;

/// <summary>Ca làm việc: Ca 1 (06:00–14:00), Ca 2 (14:00–22:00)...</summary>
public class Shift : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public ICollection<PlanningItem>   PlanningItems   { get; set; } = [];
    public ICollection<ShiftAssignment> ShiftAssignments { get; set; } = [];
}

/// <summary>Thời gian nghỉ giữa ca (12:00–13:00 ăn trưa...)</summary>
public class BreakTime : BaseEntity
{
    public TimeOnly FromTime { get; set; }
    public TimeOnly ToTime   { get; set; }
    public string?  Label    { get; set; }
}

/// <summary>
/// Một mục kế hoạch sản xuất: Job+OP trên máy X từ StartTime đến EndTime.
/// Một OP có thể có nhiều planning_items (split trên nhiều máy).
/// </summary>
public class PlanningItem : BaseEntity
{
    public int JobId     { get; set; }
    public int PartOpId  { get; set; }
    public int MachineId { get; set; }

    public int? OperatorId { get; set; }
    public int? ShiftId    { get; set; }

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime   { get; set; }

    public string? Note      { get; set; }
    public int     CreatedBy { get; set; }
    public int?    UpdatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation
    public Job     Job      { get; set; } = null!;
    public PartOp  PartOp   { get; set; } = null!;
    public Machine Machine  { get; set; } = null!;
    public User?   Operator { get; set; }
    public Shift?  Shift    { get; set; }
    public User    Creator  { get; set; } = null!;
}

/// <summary>Gán operator vào ca + máy + ngày cụ thể.</summary>
public class ShiftAssignment : BaseEntity
{
    public int     UserId       { get; set; }
    public int     MachineId    { get; set; }
    public int     ShiftId      { get; set; }
    public DateOnly AssignedDate { get; set; }

    // Navigation
    public User    User    { get; set; } = null!;
    public Machine Machine { get; set; } = null!;
    public Shift   Shift   { get; set; } = null!;
}
