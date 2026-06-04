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

public record MachineGroupDto(int Id, string Code, string Name, int MachineCount);

public record GetMachineGroupsQuery : IRequest<Result<List<MachineGroupDto>>>;

public class GetMachineGroupsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetMachineGroupsQuery, Result<List<MachineGroupDto>>>
{
    public async Task<Result<List<MachineGroupDto>>> Handle(GetMachineGroupsQuery _, CancellationToken ct)
    {
        var groups = await db.MachineGroups.OrderBy(g => g.Code)
            .Select(g => new MachineGroupDto(g.Id, g.Code, g.Name, 0))
            .ToListAsync(ct);

        // Count machines per group via MachineType match
        var counts = await db.Machines
            .Where(m => m.MachineType != null && m.IsActive)
            .GroupBy(m => m.MachineType!)
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Code, g => g.Count, ct);

        return Result.Ok(groups.Select(g => g with { MachineCount = counts.GetValueOrDefault(g.Code, 0) }).ToList());
    }
}
