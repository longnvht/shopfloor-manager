using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.MasterData;

// ── Machine ───────────────────────────────────────────────────

public record CreateMachineCommand(string Code, string Name, string? MachineType, bool IsCnc, bool IsActive, string? SerialNumber)
    : IRequest<Result<MachineDto>>;

public class CreateMachineCommandValidator : AbstractValidator<CreateMachineCommand>
{
    public CreateMachineCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.MachineType).MaximumLength(50);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
    }
}

public class CreateMachineCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateMachineCommand, Result<MachineDto>>
{
    public async Task<Result<MachineDto>> Handle(CreateMachineCommand req, CancellationToken ct)
    {
        if (await db.Machines.AnyAsync(m => m.Code == req.Code, ct))
            return Result.Fail($"Máy '{req.Code}' đã tồn tại.");

        var machine = new Machine
        {
            Code = req.Code,
            Name = req.Name,
            MachineType = req.MachineType,
            IsCnc = req.IsCnc,
            IsActive = req.IsActive,
            SerialNumber = req.SerialNumber,
        };
        db.Machines.Add(machine);
        await db.SaveChangesAsync(ct);
        return Result.Ok(new MachineDto(machine.Id, machine.Code, machine.Name, machine.MachineType, machine.IsCnc, machine.IsActive, machine.SerialNumber));
    }
}

public record UpdateMachineCommand(int Id, string Code, string Name, string? MachineType, bool IsCnc, bool IsActive, string? SerialNumber)
    : IRequest<Result<MachineDto>>;

public class UpdateMachineCommandValidator : AbstractValidator<UpdateMachineCommand>
{
    public UpdateMachineCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.MachineType).MaximumLength(50);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
    }
}

public class UpdateMachineCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdateMachineCommand, Result<MachineDto>>
{
    public async Task<Result<MachineDto>> Handle(UpdateMachineCommand req, CancellationToken ct)
    {
        var machine = await db.Machines.FindAsync([req.Id], ct);
        if (machine is null) return Result.Fail($"Không tìm thấy máy ID {req.Id}.");

        if (await db.Machines.AnyAsync(m => m.Code == req.Code && m.Id != req.Id, ct))
            return Result.Fail($"Máy '{req.Code}' đã tồn tại.");

        machine.Code = req.Code;
        machine.Name = req.Name;
        machine.MachineType = req.MachineType;
        machine.IsCnc = req.IsCnc;
        machine.IsActive = req.IsActive;
        machine.SerialNumber = req.SerialNumber;
        await db.SaveChangesAsync(ct);
        return Result.Ok(new MachineDto(machine.Id, machine.Code, machine.Name, machine.MachineType, machine.IsCnc, machine.IsActive, machine.SerialNumber));
    }
}

// ── MachineGroup ──────────────────────────────────────────────

public record CreateMachineGroupCommand(string Code, string Name, bool IsActive) : IRequest<Result<MachineGroupDto>>;

public class CreateMachineGroupCommandValidator : AbstractValidator<CreateMachineGroupCommand>
{
    public CreateMachineGroupCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateMachineGroupCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateMachineGroupCommand, Result<MachineGroupDto>>
{
    public async Task<Result<MachineGroupDto>> Handle(CreateMachineGroupCommand req, CancellationToken ct)
    {
        if (await db.MachineGroups.AnyAsync(g => g.Code == req.Code, ct))
            return Result.Fail($"Nhóm máy '{req.Code}' đã tồn tại.");

        var group = new MachineGroup { Code = req.Code, Name = req.Name, IsActive = req.IsActive };
        db.MachineGroups.Add(group);
        await db.SaveChangesAsync(ct);
        return Result.Ok(new MachineGroupDto(group.Id, group.Code, group.Name, group.IsActive, 0));
    }
}

public record UpdateMachineGroupCommand(int Id, string Code, string Name, bool IsActive) : IRequest<Result<MachineGroupDto>>;

public class UpdateMachineGroupCommandValidator : AbstractValidator<UpdateMachineGroupCommand>
{
    public UpdateMachineGroupCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpdateMachineGroupCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdateMachineGroupCommand, Result<MachineGroupDto>>
{
    public async Task<Result<MachineGroupDto>> Handle(UpdateMachineGroupCommand req, CancellationToken ct)
    {
        var group = await db.MachineGroups.FindAsync([req.Id], ct);
        if (group is null) return Result.Fail($"Không tìm thấy nhóm máy ID {req.Id}.");

        if (await db.MachineGroups.AnyAsync(g => g.Code == req.Code && g.Id != req.Id, ct))
            return Result.Fail($"Nhóm máy '{req.Code}' đã tồn tại.");

        group.Code = req.Code;
        group.Name = req.Name;
        group.IsActive = req.IsActive;
        await db.SaveChangesAsync(ct);

        var machineCount = await db.Machines.CountAsync(m => m.MachineType == group.Code && m.IsActive, ct);
        return Result.Ok(new MachineGroupDto(group.Id, group.Code, group.Name, group.IsActive, machineCount));
    }
}
