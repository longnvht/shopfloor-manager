using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.MasterData;

public record FileTypeDto(
    int Id, string Code, string Name, string? Folder,
    bool IsSegment, bool IsGcode, bool IsPartNumber, bool IsRevision, bool IsOpNumber, bool IsJobNumber,
    int SortOrder, bool IsActive);

// ── Queries ───────────────────────────────────────────────────

public record GetFileTypesQuery(bool ActiveOnly = false) : IRequest<Result<List<FileTypeDto>>>;

public class GetFileTypesQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetFileTypesQuery, Result<List<FileTypeDto>>>
{
    public async Task<Result<List<FileTypeDto>>> Handle(GetFileTypesQuery req, CancellationToken ct)
    {
        var query = db.FileTypes.AsQueryable();
        if (req.ActiveOnly)
            query = query.Where(f => f.IsActive);

        var items = await query.OrderBy(f => f.SortOrder)
            .Select(f => new FileTypeDto(
                f.Id, f.Code, f.Name, f.Folder,
                f.IsSegment, f.IsGcode, f.IsPartNumber, f.IsRevision, f.IsOpNumber, f.IsJobNumber,
                f.SortOrder, f.IsActive))
            .ToListAsync(ct);
        return Result.Ok(items);
    }
}

// ── Commands ──────────────────────────────────────────────────

public record CreateFileTypeCommand(
    string Code, string Name, string? Folder,
    bool IsSegment, bool IsGcode, bool IsPartNumber, bool IsRevision, bool IsOpNumber, bool IsJobNumber,
    int SortOrder, bool IsActive) : IRequest<Result<FileTypeDto>>;

public class CreateFileTypeCommandValidator : AbstractValidator<CreateFileTypeCommand>
{
    public CreateFileTypeCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateFileTypeCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateFileTypeCommand, Result<FileTypeDto>>
{
    public async Task<Result<FileTypeDto>> Handle(CreateFileTypeCommand req, CancellationToken ct)
    {
        if (await db.FileTypes.AnyAsync(f => f.Code == req.Code, ct))
            return Result.Fail($"Loại tài liệu '{req.Code}' đã tồn tại.");

        var fileType = new FileType
        {
            Code = req.Code, Name = req.Name, Folder = req.Folder,
            IsSegment = req.IsSegment, IsGcode = req.IsGcode,
            IsPartNumber = req.IsPartNumber, IsRevision = req.IsRevision,
            IsOpNumber = req.IsOpNumber, IsJobNumber = req.IsJobNumber,
            SortOrder = req.SortOrder, IsActive = req.IsActive,
        };
        db.FileTypes.Add(fileType);
        await db.SaveChangesAsync(ct);
        return Result.Ok(new FileTypeDto(
            fileType.Id, fileType.Code, fileType.Name, fileType.Folder,
            fileType.IsSegment, fileType.IsGcode, fileType.IsPartNumber, fileType.IsRevision, fileType.IsOpNumber, fileType.IsJobNumber,
            fileType.SortOrder, fileType.IsActive));
    }
}

public record UpdateFileTypeCommand(
    int Id, string Code, string Name, string? Folder,
    bool IsSegment, bool IsGcode, bool IsPartNumber, bool IsRevision, bool IsOpNumber, bool IsJobNumber,
    int SortOrder, bool IsActive) : IRequest<Result<FileTypeDto>>;

public class UpdateFileTypeCommandValidator : AbstractValidator<UpdateFileTypeCommand>
{
    public UpdateFileTypeCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpdateFileTypeCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdateFileTypeCommand, Result<FileTypeDto>>
{
    public async Task<Result<FileTypeDto>> Handle(UpdateFileTypeCommand req, CancellationToken ct)
    {
        var fileType = await db.FileTypes.FindAsync([req.Id], ct);
        if (fileType is null) return Result.Fail($"Không tìm thấy loại tài liệu ID {req.Id}.");

        if (await db.FileTypes.AnyAsync(f => f.Code == req.Code && f.Id != req.Id, ct))
            return Result.Fail($"Loại tài liệu '{req.Code}' đã tồn tại.");

        fileType.Code = req.Code;
        fileType.Name = req.Name;
        fileType.Folder = req.Folder;
        fileType.IsSegment = req.IsSegment;
        fileType.IsGcode = req.IsGcode;
        fileType.IsPartNumber = req.IsPartNumber;
        fileType.IsRevision = req.IsRevision;
        fileType.IsOpNumber = req.IsOpNumber;
        fileType.IsJobNumber = req.IsJobNumber;
        fileType.SortOrder = req.SortOrder;
        fileType.IsActive = req.IsActive;
        await db.SaveChangesAsync(ct);
        return Result.Ok(new FileTypeDto(
            fileType.Id, fileType.Code, fileType.Name, fileType.Folder,
            fileType.IsSegment, fileType.IsGcode, fileType.IsPartNumber, fileType.IsRevision, fileType.IsOpNumber, fileType.IsJobNumber,
            fileType.SortOrder, fileType.IsActive));
    }
}
