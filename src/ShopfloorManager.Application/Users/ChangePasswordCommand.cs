using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;

namespace ShopfloorManager.Application.Users;

public record ChangePasswordCommand(int UserId, string CurrentPassword, string NewPassword)
    : IRequest<Result>;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6)
            .NotEqual(x => x.CurrentPassword).WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại.");
    }
}

public class ChangePasswordCommandHandler(IShopfloorDbContext db, IPasswordHasher hasher)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand req, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId, ct);
        if (user is null) return Result.Fail("Không tìm thấy user.");

        if (!hasher.Verify(req.CurrentPassword, user.PasswordHash))
            return Result.Fail("Mật khẩu hiện tại không đúng.");

        user.PasswordHash = hasher.Hash(req.NewPassword);
        user.FirstLogin = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
