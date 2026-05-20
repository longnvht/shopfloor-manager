using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;

namespace ShopfloorManager.Application.Users;

public record UpdateUserCommand(
    int Id,
    string Name,
    string? Email,
    string? Sex,
    int? RoleId,
    int? UserTypeId,
    int? PositionId,
    int? WorkStatusId,
    bool IsActive) : IRequest<Result<UserDto>>;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
    }
}

public class UpdateUserCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdateUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(UpdateUserCommand req, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.Role).Include(u => u.UserType).Include(u => u.Position)
            .FirstOrDefaultAsync(u => u.Id == req.Id, ct);

        if (user is null) return Result.Fail($"Không tìm thấy user ID {req.Id}.");

        user.Name = req.Name;
        user.Email = req.Email;
        user.Sex = req.Sex;
        user.RoleId = req.RoleId;
        user.UserTypeId = req.UserTypeId;
        user.PositionId = req.PositionId;
        user.WorkStatusId = req.WorkStatusId;
        user.IsActive = req.IsActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        // Reload navigations after FK changes
        if (req.RoleId != null) await db.Users.Entry(user).Reference(u => u.Role).LoadAsync(ct);
        if (req.UserTypeId != null) await db.Users.Entry(user).Reference(u => u.UserType).LoadAsync(ct);
        if (req.PositionId != null) await db.Users.Entry(user).Reference(u => u.Position).LoadAsync(ct);

        return Result.Ok(UserDto.From(user));
    }
}
