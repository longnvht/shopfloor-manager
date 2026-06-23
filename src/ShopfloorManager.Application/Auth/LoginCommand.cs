using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;

namespace ShopfloorManager.Application.Auth;

public record LoginCommand(string UserLogin, string Password) : IRequest<Result<LoginResponse>>;

public record LoginResponse(string Token, int UserId, string Name, string Role, bool FirstLogin);

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.UserLogin).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler(
    IShopfloorDbContext db,
    IPasswordHasher hasher,
    IJwtTokenService jwt) : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand req, CancellationToken ct)
    {
        var login = req.UserLogin.ToLowerInvariant();
        var user = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserLogin.ToLower() == login && u.IsActive, ct);

        if (user is null || !hasher.Verify(req.Password, user.PasswordHash))
            return Result.Fail("Tên đăng nhập hoặc mật khẩu không đúng.");

        var token = jwt.GenerateToken(user);

        db.AuditLogs.Add(new Domain.Entities.AuditLog
        {
            UserId = user.Id,
            LoggedInAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);

        return Result.Ok(new LoginResponse(token, user.Id, user.Name, user.Role?.Name ?? "", user.FirstLogin));
    }
}
