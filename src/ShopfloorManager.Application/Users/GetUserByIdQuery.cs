using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;

namespace ShopfloorManager.Application.Users;

public record GetUserByIdQuery(int Id) : IRequest<Result<UserDto>>;

public class GetUserByIdQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetUserByIdQuery req, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.Role)
            .Include(u => u.UserType)
            .Include(u => u.Position)
            .FirstOrDefaultAsync(u => u.Id == req.Id, ct);

        return user is null
            ? Result.Fail($"Không tìm thấy user ID {req.Id}.")
            : Result.Ok(UserDto.From(user));
    }
}
