using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.MasterData;

public record OpTypeDto(int Id, string Code, string? Name, string? Description, bool IsActive);

// ── Queries ───────────────────────────────────────────────────

public record GetOpTypesQuery(bool ActiveOnly = false) : IRequest<Result<List<OpTypeDto>>>;

public class GetOpTypesQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetOpTypesQuery, Result<List<OpTypeDto>>>
{
    public async Task<Result<List<OpTypeDto>>> Handle(GetOpTypesQuery req, CancellationToken ct)
    {
        var query = db.OpTypes.AsQueryable();
        if (req.ActiveOnly)
            query = query.Where(t => t.IsActive);

        var items = await query.OrderBy(t => t.Code)
            .Select(t => new OpTypeDto(t.Id, t.Code, t.Name, t.Description, t.IsActive))
            .ToListAsync(ct);
        return Result.Ok(items);
    }
}

// ── Commands ──────────────────────────────────────────────────

public record CreateOpTypeCommand(string Code, string? Name, string? Description, bool IsActive) : IRequest<Result<OpTypeDto>>;

public class CreateOpTypeCommandValidator : AbstractValidator<CreateOpTypeCommand>
{
    public CreateOpTypeCommandValidator() =>
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
}

public class CreateOpTypeCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateOpTypeCommand, Result<OpTypeDto>>
{
    public async Task<Result<OpTypeDto>> Handle(CreateOpTypeCommand req, CancellationToken ct)
    {
        if (await db.OpTypes.AnyAsync(t => t.Code == req.Code, ct))
            return Result.Fail($"Loại OP '{req.Code}' đã tồn tại.");

        var opType = new OpType { Code = req.Code, Name = req.Name, Description = req.Description, IsActive = req.IsActive };
        db.OpTypes.Add(opType);
        await db.SaveChangesAsync(ct);
        return Result.Ok(new OpTypeDto(opType.Id, opType.Code, opType.Name, opType.Description, opType.IsActive));
    }
}

public record UpdateOpTypeCommand(int Id, string Code, string? Name, string? Description, bool IsActive) : IRequest<Result<OpTypeDto>>;

public class UpdateOpTypeCommandValidator : AbstractValidator<UpdateOpTypeCommand>
{
    public UpdateOpTypeCommandValidator() =>
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
}

public class UpdateOpTypeCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdateOpTypeCommand, Result<OpTypeDto>>
{
    public async Task<Result<OpTypeDto>> Handle(UpdateOpTypeCommand req, CancellationToken ct)
    {
        var opType = await db.OpTypes.FindAsync([req.Id], ct);
        if (opType is null) return Result.Fail($"Không tìm thấy loại OP ID {req.Id}.");

        if (await db.OpTypes.AnyAsync(t => t.Code == req.Code && t.Id != req.Id, ct))
            return Result.Fail($"Loại OP '{req.Code}' đã tồn tại.");

        opType.Code = req.Code;
        opType.Name = req.Name;
        opType.Description = req.Description;
        opType.IsActive = req.IsActive;
        await db.SaveChangesAsync(ct);
        return Result.Ok(new OpTypeDto(opType.Id, opType.Code, opType.Name, opType.Description, opType.IsActive));
    }
}
