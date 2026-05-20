using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;

namespace ShopfloorManager.Application.Auth;

public record ResetPasswordCommand(string Email, string Code, string NewPassword) : IRequest<Result>;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Length(6);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}

public class ResetPasswordCommandHandler(IShopfloorDbContext db, IPasswordHasher hasher)
    : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand req, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Email == req.Email && u.ResetCode == req.Code && u.IsActive, ct);

        if (user is null)
            return Result.Fail("Mã đặt lại không hợp lệ hoặc đã hết hạn.");

        user.PasswordHash = hasher.Hash(req.NewPassword);
        user.ResetCode = null;
        user.FirstLogin = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
