using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Roles;

public record RoleDto(int Id, string Name);

// ── Queries ───────────────────────────────────────────────────

public record GetRolesQuery : IRequest<Result<List<RoleDto>>>;

public class GetRolesQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetRolesQuery, Result<List<RoleDto>>>
{
    public async Task<Result<List<RoleDto>>> Handle(GetRolesQuery _, CancellationToken ct)
    {
        var roles = await db.Roles.OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Name)).ToListAsync(ct);
        return Result.Ok(roles);
    }
}

// ── Commands ──────────────────────────────────────────────────

public record CreateRoleCommand(string Name) : IRequest<Result<RoleDto>>;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator() =>
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
}

public class CreateRoleCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateRoleCommand, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(CreateRoleCommand req, CancellationToken ct)
    {
        if (await db.Roles.AnyAsync(r => r.Name == req.Name, ct))
            return Result.Fail($"Role '{req.Name}' đã tồn tại.");

        var role = new Role { Name = req.Name };
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        return Result.Ok(new RoleDto(role.Id, role.Name));
    }
}

public record UpdateRoleCommand(int Id, string Name) : IRequest<Result<RoleDto>>;

public class UpdateRoleCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdateRoleCommand, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(UpdateRoleCommand req, CancellationToken ct)
    {
        var role = await db.Roles.FindAsync([req.Id], ct);
        if (role is null) return Result.Fail($"Không tìm thấy role ID {req.Id}.");

        role.Name = req.Name;
        await db.SaveChangesAsync(ct);
        return Result.Ok(new RoleDto(role.Id, role.Name));
    }
}
