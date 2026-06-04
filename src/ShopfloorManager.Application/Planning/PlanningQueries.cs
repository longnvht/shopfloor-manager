using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;

namespace ShopfloorManager.Application.Planning;

// ── DTOs ──────────────────────────────────────────────────────────────────

public record PlanningItemDto(
    int Id,
    int JobId,    string JobNumber,
    int PartOpId, string OpNumber, string? OpTypeName,
    int MachineId, string MachineCode, string? MachineName,
    int? OperatorId, string? OperatorName,
    int? ShiftId, string? ShiftName,
    DateTimeOffset StartTime, DateTimeOffset EndTime,
    string? Note);

public record ShiftDto(int Id, string Name, TimeOnly StartTime, TimeOnly EndTime);

public record BreakTimeDto(int Id, TimeOnly FromTime, TimeOnly ToTime, string? Label);

public record ShiftAssignmentDto(
    int Id, int UserId, string UserName,
    int MachineId, string MachineCode,
    int ShiftId, string ShiftName,
    DateOnly AssignedDate);

// ── Queries ────────────────────────────────────────────────────────────────

public record GetPlanningItemsQuery(
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate   = null,
    int? MachineId = null)
    : IRequest<Result<List<PlanningItemDto>>>;

public class GetPlanningItemsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetPlanningItemsQuery, Result<List<PlanningItemDto>>>
{
    public async Task<Result<List<PlanningItemDto>>> Handle(GetPlanningItemsQuery req, CancellationToken ct)
    {
        var q = db.PlanningItems
            .Include(p => p.Job)
            .Include(p => p.PartOp).ThenInclude(o => o.OpType)
            .Include(p => p.Machine)
            .Include(p => p.Operator)
            .Include(p => p.Shift)
            .AsQueryable();

        if (req.StartDate.HasValue) q = q.Where(p => p.EndTime >= req.StartDate.Value);
        if (req.EndDate.HasValue)   q = q.Where(p => p.StartTime <= req.EndDate.Value);
        if (req.MachineId.HasValue) q = q.Where(p => p.MachineId == req.MachineId.Value);

        var items = await q.OrderBy(p => p.StartTime).ToListAsync(ct);

        return Result.Ok(items.Select(p => new PlanningItemDto(
            p.Id, p.JobId, p.Job.JobNumber,
            p.PartOpId, p.PartOp.OpNumber, p.PartOp.OpType?.Name,
            p.MachineId, p.Machine.Code, p.Machine.Name,
            p.OperatorId, p.Operator != null ? p.Operator.Name : null,
            p.ShiftId, p.Shift?.Name,
            p.StartTime, p.EndTime, p.Note
        )).ToList());
    }
}

public record GetShiftsQuery : IRequest<Result<List<ShiftDto>>>;

public class GetShiftsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetShiftsQuery, Result<List<ShiftDto>>>
{
    public async Task<Result<List<ShiftDto>>> Handle(GetShiftsQuery _, CancellationToken ct)
    {
        var items = await db.Shifts.OrderBy(s => s.StartTime)
            .Select(s => new ShiftDto(s.Id, s.Name, s.StartTime, s.EndTime))
            .ToListAsync(ct);
        return Result.Ok(items);
    }
}

public record GetBreakTimesQuery : IRequest<Result<List<BreakTimeDto>>>;

public class GetBreakTimesQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetBreakTimesQuery, Result<List<BreakTimeDto>>>
{
    public async Task<Result<List<BreakTimeDto>>> Handle(GetBreakTimesQuery _, CancellationToken ct)
    {
        var items = await db.BreakTimes.OrderBy(b => b.FromTime)
            .Select(b => new BreakTimeDto(b.Id, b.FromTime, b.ToTime, b.Label))
            .ToListAsync(ct);
        return Result.Ok(items);
    }
}
