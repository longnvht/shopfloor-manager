using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Lookups;

// ── DTOs ─────────────────────────────────────────────────────

public record PositionDto(int Id, string Code, string? Description, bool IsActive);
public record UserTypeDto(int Id, string TypeName, string? Description, bool CanEnterValue, bool CanRaiseNcr);
public record WorkStatusDto(int Id, string Name, bool IsWorking);

// ── Positions ─────────────────────────────────────────────────

public record GetPositionsQuery : IRequest<Result<List<PositionDto>>>;

public class GetPositionsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetPositionsQuery, Result<List<PositionDto>>>
{
    public async Task<Result<List<PositionDto>>> Handle(GetPositionsQuery _, CancellationToken ct)
    {
        var items = await db.Positions.OrderBy(p => p.Code)
            .Select(p => new PositionDto(p.Id, p.Code, p.Description, p.IsActive)).ToListAsync(ct);
        return Result.Ok(items);
    }
}

public record CreatePositionCommand(string Code, string? Description) : IRequest<Result<PositionDto>>;

public class CreatePositionCommandValidator : AbstractValidator<CreatePositionCommand>
{
    public CreatePositionCommandValidator() =>
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
}

public class CreatePositionCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreatePositionCommand, Result<PositionDto>>
{
    public async Task<Result<PositionDto>> Handle(CreatePositionCommand req, CancellationToken ct)
    {
        if (await db.Positions.AnyAsync(p => p.Code == req.Code, ct))
            return Result.Fail($"Vị trí '{req.Code}' đã tồn tại.");

        var pos = new Position { Code = req.Code, Description = req.Description };
        db.Positions.Add(pos);
        await db.SaveChangesAsync(ct);
        return Result.Ok(new PositionDto(pos.Id, pos.Code, pos.Description, pos.IsActive));
    }
}

// ── UserTypes ─────────────────────────────────────────────────

public record GetUserTypesQuery : IRequest<Result<List<UserTypeDto>>>;

public class GetUserTypesQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetUserTypesQuery, Result<List<UserTypeDto>>>
{
    public async Task<Result<List<UserTypeDto>>> Handle(GetUserTypesQuery _, CancellationToken ct)
    {
        var items = await db.UserTypes.OrderBy(t => t.TypeName)
            .Select(t => new UserTypeDto(t.Id, t.TypeName, t.Description, t.CanEnterValue, t.CanRaiseNcr))
            .ToListAsync(ct);
        return Result.Ok(items);
    }
}

public record CreateUserTypeCommand(string TypeName, string? Description, bool CanEnterValue, bool CanRaiseNcr)
    : IRequest<Result<UserTypeDto>>;

public class CreateUserTypeCommandValidator : AbstractValidator<CreateUserTypeCommand>
{
    public CreateUserTypeCommandValidator() =>
        RuleFor(x => x.TypeName).NotEmpty().MaximumLength(30);
}

public class CreateUserTypeCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateUserTypeCommand, Result<UserTypeDto>>
{
    public async Task<Result<UserTypeDto>> Handle(CreateUserTypeCommand req, CancellationToken ct)
    {
        if (await db.UserTypes.AnyAsync(t => t.TypeName == req.TypeName, ct))
            return Result.Fail($"Loại user '{req.TypeName}' đã tồn tại.");

        var ut = new UserType { TypeName = req.TypeName, Description = req.Description, CanEnterValue = req.CanEnterValue, CanRaiseNcr = req.CanRaiseNcr };
        db.UserTypes.Add(ut);
        await db.SaveChangesAsync(ct);
        return Result.Ok(new UserTypeDto(ut.Id, ut.TypeName, ut.Description, ut.CanEnterValue, ut.CanRaiseNcr));
    }
}

// ── WorkStatuses ──────────────────────────────────────────────

public record GetWorkStatusesQuery : IRequest<Result<List<WorkStatusDto>>>;

public class GetWorkStatusesQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetWorkStatusesQuery, Result<List<WorkStatusDto>>>
{
    public async Task<Result<List<WorkStatusDto>>> Handle(GetWorkStatusesQuery _, CancellationToken ct)
    {
        var items = await db.WorkStatuses.OrderBy(w => w.Name)
            .Select(w => new WorkStatusDto(w.Id, w.Name, w.IsWorking)).ToListAsync(ct);
        return Result.Ok(items);
    }
}
