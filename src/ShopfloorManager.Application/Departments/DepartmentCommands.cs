using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Departments;

public record DepartmentDto(int Id, string Code, string Name);

// ── Queries ───────────────────────────────────────────────────

public record GetDepartmentsQuery : IRequest<Result<List<DepartmentDto>>>;

public class GetDepartmentsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetDepartmentsQuery, Result<List<DepartmentDto>>>
{
    public async Task<Result<List<DepartmentDto>>> Handle(GetDepartmentsQuery _, CancellationToken ct)
    {
        var items = await db.Departments.OrderBy(d => d.Code)
            .Select(d => new DepartmentDto(d.Id, d.Code, d.Name)).ToListAsync(ct);
        return Result.Ok(items);
    }
}

// ── Commands ──────────────────────────────────────────────────

public record CreateDepartmentCommand(string Code, string Name) : IRequest<Result<DepartmentDto>>;

public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateDepartmentCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateDepartmentCommand, Result<DepartmentDto>>
{
    public async Task<Result<DepartmentDto>> Handle(CreateDepartmentCommand req, CancellationToken ct)
    {
        if (await db.Departments.AnyAsync(d => d.Code == req.Code, ct))
            return Result.Fail($"Phòng ban '{req.Code}' đã tồn tại.");

        var dept = new Department { Code = req.Code, Name = req.Name };
        db.Departments.Add(dept);
        await db.SaveChangesAsync(ct);
        return Result.Ok(new DepartmentDto(dept.Id, dept.Code, dept.Name));
    }
}

public record UpdateDepartmentCommand(int Id, string Code, string Name) : IRequest<Result<DepartmentDto>>;

public class UpdateDepartmentCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdateDepartmentCommand, Result<DepartmentDto>>
{
    public async Task<Result<DepartmentDto>> Handle(UpdateDepartmentCommand req, CancellationToken ct)
    {
        var dept = await db.Departments.FindAsync([req.Id], ct);
        if (dept is null) return Result.Fail($"Không tìm thấy phòng ban ID {req.Id}.");

        dept.Code = req.Code;
        dept.Name = req.Name;
        await db.SaveChangesAsync(ct);
        return Result.Ok(new DepartmentDto(dept.Id, dept.Code, dept.Name));
    }
}
