using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Planning;

public record CreatePlanningItemCommand(
    int JobId, int PartOpId, int MachineId,
    int? OperatorId, int? ShiftId,
    DateTimeOffset StartTime, DateTimeOffset EndTime,
    string? Note, int CreatedBy)
    : IRequest<Result<PlanningItemDto>>;

public class CreatePlanningItemCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreatePlanningItemCommand, Result<PlanningItemDto>>
{
    public async Task<Result<PlanningItemDto>> Handle(CreatePlanningItemCommand req, CancellationToken ct)
    {
        // Conflict check (warn only — not blocking)
        var hasConflict = await db.PlanningItems.AnyAsync(p =>
            p.MachineId == req.MachineId &&
            p.StartTime < req.EndTime &&
            p.EndTime   > req.StartTime, ct);

        var item = new PlanningItem
        {
            JobId = req.JobId, PartOpId = req.PartOpId, MachineId = req.MachineId,
            OperatorId = req.OperatorId, ShiftId = req.ShiftId,
            StartTime = req.StartTime, EndTime = req.EndTime,
            Note = req.Note, CreatedBy = req.CreatedBy,
        };
        db.PlanningItems.Add(item);
        await db.SaveChangesAsync(ct);

        // Reload with includes for response
        var loaded = await db.PlanningItems
            .Include(p => p.Job)
            .Include(p => p.PartOp).ThenInclude(o => o.OpType)
            .Include(p => p.Machine)
            .Include(p => p.Operator)
            .Include(p => p.Shift)
            .FirstAsync(p => p.Id == item.Id, ct);

        var dto = new PlanningItemDto(
            loaded.Id, loaded.JobId, loaded.Job.JobNumber,
            loaded.PartOpId, loaded.PartOp.OpNumber, loaded.PartOp.OpType?.Name,
            loaded.MachineId, loaded.Machine.Code, loaded.Machine.Name,
            loaded.OperatorId, loaded.Operator?.Name,
            loaded.ShiftId, loaded.Shift?.Name,
            loaded.StartTime, loaded.EndTime, loaded.Note);

        // hasConflict: lưu thành công — conflict chỉ là cảnh báo, không block
        return Result.Ok(dto);
    }
}

public record DeletePlanningItemCommand(int Id) : IRequest<Result>;

public class DeletePlanningItemCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<DeletePlanningItemCommand, Result>
{
    public async Task<Result> Handle(DeletePlanningItemCommand req, CancellationToken ct)
    {
        var item = await db.PlanningItems.FindAsync([req.Id], ct);
        if (item is null) return Result.Fail("Không tìm thấy kế hoạch.");
        db.PlanningItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record CreateShiftCommand(string Name, TimeOnly StartTime, TimeOnly EndTime)
    : IRequest<Result<ShiftDto>>;

public class CreateShiftCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateShiftCommand, Result<ShiftDto>>
{
    public async Task<Result<ShiftDto>> Handle(CreateShiftCommand req, CancellationToken ct)
    {
        var shift = new Shift { Name = req.Name, StartTime = req.StartTime, EndTime = req.EndTime };
        db.Shifts.Add(shift);
        await db.SaveChangesAsync(ct);
        return Result.Ok(new ShiftDto(shift.Id, shift.Name, shift.StartTime, shift.EndTime));
    }
}
