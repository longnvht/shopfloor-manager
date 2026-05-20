using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Shared.Pagination;

namespace ShopfloorManager.Application.Users;

public record GetUsersQuery(int Page = 1, int PageSize = 20, string? Search = null, bool? IsActive = null)
    : IRequest<Result<PagedResult<UserDto>>>;

public class GetUsersQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetUsersQuery, Result<PagedResult<UserDto>>>
{
    public async Task<Result<PagedResult<UserDto>>> Handle(GetUsersQuery req, CancellationToken ct)
    {
        var query = db.Users
            .Include(u => u.Role)
            .Include(u => u.UserType)
            .Include(u => u.Position)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
            query = query.Where(u => u.Name.Contains(req.Search) || u.UserLogin.Contains(req.Search));

        if (req.IsActive.HasValue)
            query = query.Where(u => u.IsActive == req.IsActive.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.Name)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(u => UserDto.From(u))
            .ToListAsync(ct);

        return Result.Ok(new PagedResult<UserDto>(items, req.Page, req.PageSize, total));
    }
}
