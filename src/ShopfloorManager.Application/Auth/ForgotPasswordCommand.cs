using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;

namespace ShopfloorManager.Application.Auth;

public record ForgotPasswordCommand(string Email) : IRequest<Result>;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator() =>
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
}

public class ForgotPasswordCommandHandler(IShopfloorDbContext db, IEmailService email)
    : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand req, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Email == req.Email && u.IsActive, ct);

        // Không tiết lộ user có tồn tại không
        if (user is null) return Result.Ok();

        var code = Random.Shared.Next(100000, 999999).ToString();
        user.ResetCode = code;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        await email.SendAsync(
            req.Email,
            "Shopfloor Manager — Đặt lại mật khẩu",
            $"Mã đặt lại mật khẩu của bạn: <b>{code}</b><br>Mã có hiệu lực trong 30 phút.",
            ct);

        return Result.Ok();
    }
}
