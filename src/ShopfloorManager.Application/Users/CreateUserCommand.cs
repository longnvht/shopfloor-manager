using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Users;

public record CreateUserCommand(
    string UserLogin,
    string Password,
    string Name,
    string? Email,
    int? RoleId,
    int? UserTypeId,
    int? PositionId,
    int? WorkStatusId) : IRequest<Result<UserDto>>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.UserLogin).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
    }
}

public class CreateUserCommandHandler(IShopfloorDbContext db, IPasswordHasher hasher)
    : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(CreateUserCommand req, CancellationToken ct)
    {
        var exists = await db.Users.AnyAsync(u => u.UserLogin == req.UserLogin, ct);
        if (exists)
            return Result.Fail($"Tên đăng nhập '{req.UserLogin}' đã tồn tại.");

        var user = new User
        {
            UserLogin = req.UserLogin,
            PasswordHash = hasher.Hash(req.Password),
            Name = req.Name,
            Email = req.Email,
            RoleId = req.RoleId,
            UserTypeId = req.UserTypeId,
            PositionId = req.PositionId,
            WorkStatusId = req.WorkStatusId
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        await db.Users.Entry(user).Reference(u => u.Role).LoadAsync(ct);
        await db.Users.Entry(user).Reference(u => u.UserType).LoadAsync(ct);
        await db.Users.Entry(user).Reference(u => u.Position).LoadAsync(ct);

        return Result.Ok(UserDto.From(user));
    }
}
