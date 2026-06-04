using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;

namespace ShopfloorManager.Application.MasterData;

public record MachineDto(int Id, string Code, string Name, string? MachineType, bool IsCnc);

public record GetMachinesQuery(bool ActiveOnly = true) : IRequest<Result<List<MachineDto>>>;

public class GetMachinesQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetMachinesQuery, Result<List<MachineDto>>>
{
    public async Task<Result<List<MachineDto>>> Handle(GetMachinesQuery req, CancellationToken ct)
    {
        var query = db.Machines.AsQueryable();
        if (req.ActiveOnly)
            query = query.Where(m => m.IsActive);

        var items = await query
            .OrderBy(m => m.Code)
            .Select(m => new MachineDto(m.Id, m.Code, m.Name, m.MachineType, m.IsCnc))
            .ToListAsync(ct);

        return Result.Ok(items);
    }
}
