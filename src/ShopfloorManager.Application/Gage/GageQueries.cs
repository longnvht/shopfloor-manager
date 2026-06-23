using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Application.GageManagement;

// ── DTOs ──────────────────────────────────────────────────────────────────

public record GageDto(
    int Id, string GageNo, string? SerialNo,
    string Description, string? MeasuringRange, string? Accuracy, string Unit,
    string? Manufacturer, int? CalibFrequencyDays,
    DateOnly? LastCalibration, DateOnly? DueDate, int? DaysRemaining,
    DateOnly? InServiceDate,
    string StatusCode, bool IsValid,
    int? GageTypeId, string? GageTypeName, string? CategoryCode,
    int? CurrentLocationId, string? CurrentLocationDesc,
    bool IsBorrowed, bool HasPendingCalib,
    string? Note);

public record MesGageDto(int Id, string GageNo, string Description, string Unit, string? CategoryCode);

public record GageTypeDto(int Id, string Code, string Name, string? CategoryCode);
public record GageLocationDto(int Id, string Code, string Description);
public record CalibVendorDto(int Id, string Name, string? Contact, string? Phone, string? Email);
public record CalibProcedureDto(int Id, string Name, string? Revision, bool IsLatest);

public record CalibRequestDto(
    int Id, int GageId, string GageNo, string GageDescription,
    int? VendorId, string? VendorName,
    DateOnly RequestDate, CalibRequestStatus Status,
    string? ProcedureName, DateOnly? CalibrationDate,
    string? CalibratedBy, string? AsFoundConditions);

public record BorrowTransactionDto(
    int Id, int GageId, string GageNo,
    int BorrowerId, string? BorrowerName,
    DateOnly BorrowDate, DateOnly? ExpectedReturnDate, DateOnly? ReturnDate,
    BorrowStatus Status, string? Note);

// ── Gage Queries ──────────────────────────────────────────────────────────

public record GetGagesQuery(
    string? Search = null,
    string? StatusCode = null,
    int? GageTypeId = null,
    bool? IsBorrowed = null,
    int Page = 1, int PageSize = 50)
    : IRequest<Result<List<GageDto>>>;

public class GetGagesQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetGagesQuery, Result<List<GageDto>>>
{
    public async Task<Result<List<GageDto>>> Handle(GetGagesQuery req, CancellationToken ct)
    {
        var q = db.Gages
            .Include(g => g.GageType).ThenInclude(t => t!.Category)
            .Include(g => g.CurrentLocation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(g => g.GageNo.Contains(req.Search) || g.Description.Contains(req.Search));
        if (!string.IsNullOrWhiteSpace(req.StatusCode))
            q = q.Where(g => g.StatusCode == req.StatusCode);
        if (req.GageTypeId.HasValue)
            q = q.Where(g => g.GageTypeId == req.GageTypeId);
        if (req.IsBorrowed.HasValue)
            q = q.Where(g => g.IsBorrowed == req.IsBorrowed);

        var items = await q.OrderBy(g => g.GageNo)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .ToListAsync(ct);

        return Result.Ok(items.Select(Map).ToList());
    }

    internal static GageDto Map(Gage g) => new(
        g.Id, g.GageNo, g.SerialNo, g.Description, g.MeasuringRange, g.Accuracy, g.Unit,
        g.Manufacturer, g.CalibFrequencyDays, g.LastCalibration, g.DueDate, g.DaysRemaining,
        g.InServiceDate, g.StatusCode, g.IsValid,
        g.GageTypeId, g.GageType?.Name, g.GageType?.Category?.Code,
        g.CurrentLocationId, g.CurrentLocation?.Description,
        g.IsBorrowed, g.HasPendingCalib, g.Note);
}

// ── MES: chọn gage khi nhập measure value ──────────────────────────────────
// Chỉ trả gage is_valid=true, chưa bị mượn, sorted by gage_no (xem 08_gage_management.md §6).

public record GetMesGagesQuery(string? CategoryCode = null) : IRequest<Result<List<MesGageDto>>>;

public class GetMesGagesQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetMesGagesQuery, Result<List<MesGageDto>>>
{
    public async Task<Result<List<MesGageDto>>> Handle(GetMesGagesQuery req, CancellationToken ct)
    {
        var q = db.Gages
            .Include(g => g.GageType).ThenInclude(t => t!.Category)
            .Where(g => (g.StatusCode == GageStatusCode.Valid || g.StatusCode == GageStatusCode.Borrowed) && !g.IsBorrowed)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.CategoryCode))
            q = q.Where(g => g.GageType != null && g.GageType.Category != null
                           && g.GageType.Category.Code == req.CategoryCode);

        var items = await q.OrderBy(g => g.GageNo)
            .Select(g => new MesGageDto(g.Id, g.GageNo, g.Description, g.Unit,
                g.GageType != null && g.GageType.Category != null ? g.GageType.Category.Code : null))
            .ToListAsync(ct);

        return Result.Ok(items);
    }
}

public record GetGagesCalibDueQuery(int DaysThreshold = 60) : IRequest<Result<List<GageDto>>>;

public class GetGagesCalibDueQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetGagesCalibDueQuery, Result<List<GageDto>>>
{
    public async Task<Result<List<GageDto>>> Handle(GetGagesCalibDueQuery req, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var items = await db.Gages
            .Include(g => g.GageType)
            .Include(g => g.CurrentLocation)
            .Where(g => g.LastCalibration != null && g.CalibFrequencyDays != null)
            .ToListAsync(ct);

        var due = items
            .Where(g => g.DaysRemaining.HasValue && g.DaysRemaining.Value <= req.DaysThreshold)
            .OrderBy(g => g.DueDate)
            .ToList();

        return Result.Ok(due.Select(GetGagesQueryHandler.Map).ToList());
    }
}

// ── Lookup Queries ────────────────────────────────────────────────────────

public record GetGageTypesQuery(int? CategoryId = null) : IRequest<Result<List<GageTypeDto>>>;

public class GetGageTypesQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetGageTypesQuery, Result<List<GageTypeDto>>>
{
    public async Task<Result<List<GageTypeDto>>> Handle(GetGageTypesQuery req, CancellationToken ct)
    {
        var q = db.GageTypes.Include(t => t.Category).AsQueryable();
        if (req.CategoryId.HasValue)
            q = q.Where(t => t.CategoryId == req.CategoryId);
        var items = await q.OrderBy(t => t.Name)
            .Select(t => new GageTypeDto(t.Id, t.Code, t.Name, t.Category != null ? t.Category.Code : null))
            .ToListAsync(ct);
        return Result.Ok(items);
    }
}

public record GetGageLocationsQuery : IRequest<Result<List<GageLocationDto>>>;

public class GetGageLocationsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetGageLocationsQuery, Result<List<GageLocationDto>>>
{
    public async Task<Result<List<GageLocationDto>>> Handle(GetGageLocationsQuery _, CancellationToken ct)
    {
        var items = await db.GageLocations.OrderBy(l => l.Code)
            .Select(l => new GageLocationDto(l.Id, l.Code, l.Description))
            .ToListAsync(ct);
        return Result.Ok(items);
    }
}

// ── Calibration Queries ───────────────────────────────────────────────────

public record GetCalibRequestsQuery(
    CalibRequestStatus? Status = null,
    int? GageId = null,
    int Page = 1, int PageSize = 30)
    : IRequest<Result<List<CalibRequestDto>>>;

public class GetCalibRequestsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetCalibRequestsQuery, Result<List<CalibRequestDto>>>
{
    public async Task<Result<List<CalibRequestDto>>> Handle(GetCalibRequestsQuery req, CancellationToken ct)
    {
        var q = db.CalibRequests
            .Include(r => r.Gage)
            .Include(r => r.Vendor)
            .Include(r => r.Record).ThenInclude(rec => rec!.Procedure)
            .AsQueryable();

        if (req.Status.HasValue) q = q.Where(r => r.Status == req.Status);
        if (req.GageId.HasValue)  q = q.Where(r => r.GageId == req.GageId);

        var items = await q.OrderByDescending(r => r.RequestDate)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .ToListAsync(ct);

        return Result.Ok(items.Select(r => new CalibRequestDto(
            r.Id, r.GageId, r.Gage.GageNo, r.Gage.Description,
            r.VendorId, r.Vendor?.Name,
            r.RequestDate, r.Status,
            r.Record?.Procedure?.Name, r.Record?.CalibrationDate,
            r.Record?.CalibratedBy, r.Record?.AsFoundConditions
        )).ToList());
    }
}

public record GetBorrowTransactionsQuery(
    int? GageId = null,
    BorrowStatus? Status = null,
    int Page = 1, int PageSize = 50)
    : IRequest<Result<List<BorrowTransactionDto>>>;

public class GetBorrowTransactionsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetBorrowTransactionsQuery, Result<List<BorrowTransactionDto>>>
{
    public async Task<Result<List<BorrowTransactionDto>>> Handle(GetBorrowTransactionsQuery req, CancellationToken ct)
    {
        var q = db.BorrowTransactions
            .Include(t => t.Gage)
            .Include(t => t.Borrower)
            .AsQueryable();

        if (req.GageId.HasValue) q = q.Where(t => t.GageId == req.GageId);
        if (req.Status.HasValue) q = q.Where(t => t.Status == req.Status);

        var items = await q.OrderByDescending(t => t.BorrowDate)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .ToListAsync(ct);

        return Result.Ok(items.Select(t => new BorrowTransactionDto(
            t.Id, t.GageId, t.Gage.GageNo,
            t.BorrowerId, t.Borrower.Name,
            t.BorrowDate, t.ExpectedReturnDate, t.ReturnDate,
            t.Status, t.Note
        )).ToList());
    }
}

public record GetCalibVendorsQuery : IRequest<Result<List<CalibVendorDto>>>;

public class GetCalibVendorsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetCalibVendorsQuery, Result<List<CalibVendorDto>>>
{
    public async Task<Result<List<CalibVendorDto>>> Handle(GetCalibVendorsQuery _, CancellationToken ct)
    {
        var items = await db.CalibVendors.OrderBy(v => v.Name)
            .Select(v => new CalibVendorDto(v.Id, v.Name, v.Contact, v.Phone, v.Email))
            .ToListAsync(ct);
        return Result.Ok(items);
    }
}
