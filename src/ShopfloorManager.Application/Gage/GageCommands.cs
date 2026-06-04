using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Application.GageManagement;

// ── Create Gage ────────────────────────────────────────────────────────────

public record CreateGageCommand(
    string GageNo, string Description,
    string? SerialNo, string? MeasuringRange, string? Accuracy, string Unit,
    string? Manufacturer, int? CalibFrequencyDays,
    DateOnly? LastCalibration, DateOnly? InServiceDate,
    int? GageTypeId, int? DefaultLocationId, int? DefaultSlotId,
    int? VendorId, string? Note)
    : IRequest<Result<GageDto>>;

public class CreateGageCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateGageCommand, Result<GageDto>>
{
    public async Task<Result<GageDto>> Handle(CreateGageCommand req, CancellationToken ct)
    {
        if (await db.Gages.AnyAsync(g => g.GageNo == req.GageNo, ct))
            return Result.Fail($"Gage No '{req.GageNo}' đã tồn tại.");

        var gage = new Gage
        {
            GageNo = req.GageNo, Description = req.Description,
            SerialNo = req.SerialNo, MeasuringRange = req.MeasuringRange,
            Accuracy = req.Accuracy, Unit = req.Unit, Manufacturer = req.Manufacturer,
            CalibFrequencyDays = req.CalibFrequencyDays,
            LastCalibration = req.LastCalibration, InServiceDate = req.InServiceDate,
            GageTypeId = req.GageTypeId,
            DefaultLocationId = req.DefaultLocationId, DefaultSlotId = req.DefaultSlotId,
            CurrentLocationId = req.DefaultLocationId, CurrentSlotId = req.DefaultSlotId,
            VendorId = req.VendorId, Note = req.Note,
            StatusCode = GageStatusCode.Valid,
        };

        db.Gages.Add(gage);
        await db.SaveChangesAsync(ct);
        return Result.Ok(GetGagesQueryHandler.Map(gage));
    }
}

// ── Borrow Gage ────────────────────────────────────────────────────────────

public record BorrowGageCommand(
    int GageId, int BorrowerId, int ManagerId,
    DateOnly? ExpectedReturnDate, int? UseLocationId, string? Note)
    : IRequest<Result<int>>;

public class BorrowGageCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<BorrowGageCommand, Result<int>>
{
    public async Task<Result<int>> Handle(BorrowGageCommand req, CancellationToken ct)
    {
        var gage = await db.Gages.FindAsync([req.GageId], ct);
        if (gage is null) return Result.Fail("Gage không tồn tại.");
        if (gage.IsBorrowed) return Result.Fail("Gage đang được mượn.");
        if (!gage.IsValid)   return Result.Fail($"Gage không hợp lệ (trạng thái: {gage.StatusCode}).");

        var tx = new BorrowTransaction
        {
            GageId = req.GageId, BorrowerId = req.BorrowerId, ManagerId = req.ManagerId,
            BorrowDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ExpectedReturnDate = req.ExpectedReturnDate,
            UseLocationId = req.UseLocationId,
            FromLocationId = gage.CurrentLocationId, FromSlotId = gage.CurrentSlotId,
            Status = BorrowStatus.Active,
            Note = req.Note,
        };

        gage.IsBorrowed        = true;
        gage.StatusCode        = GageStatusCode.Borrowed;
        gage.CurrentLocationId = req.UseLocationId ?? gage.CurrentLocationId;

        db.BorrowTransactions.Add(tx);
        await db.SaveChangesAsync(ct);
        return Result.Ok(tx.Id);
    }
}

// ── Return Gage ────────────────────────────────────────────────────────────

public record ReturnGageCommand(int TransactionId) : IRequest<Result>;

public class ReturnGageCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<ReturnGageCommand, Result>
{
    public async Task<Result> Handle(ReturnGageCommand req, CancellationToken ct)
    {
        var tx = await db.BorrowTransactions
            .Include(t => t.Gage)
            .FirstOrDefaultAsync(t => t.Id == req.TransactionId, ct);
        if (tx is null) return Result.Fail("Giao dịch không tồn tại.");
        if (tx.Status != BorrowStatus.Active) return Result.Fail("Giao dịch không còn active.");

        tx.ReturnDate = DateOnly.FromDateTime(DateTime.UtcNow);
        tx.Status     = BorrowStatus.Returned;

        var gage = tx.Gage;
        gage.IsBorrowed        = false;
        gage.CurrentLocationId = gage.DefaultLocationId;
        gage.CurrentSlotId     = gage.DefaultSlotId;
        // Restore status: VALID nếu vẫn trong hạn, EXPIRED nếu quá hạn
        gage.StatusCode = gage.DaysRemaining >= 0
            ? GageStatusCode.Valid : GageStatusCode.Expired;

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

// ── Create CalibRequest ────────────────────────────────────────────────────

public record CreateCalibRequestCommand(
    int GageId, int? VendorId, int CreatedBy)
    : IRequest<Result<int>>;

public class CreateCalibRequestCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateCalibRequestCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CreateCalibRequestCommand req, CancellationToken ct)
    {
        var gage = await db.Gages.FindAsync([req.GageId], ct);
        if (gage is null) return Result.Fail("Gage không tồn tại.");

        var pending = await db.CalibRequests.AnyAsync(r =>
            r.GageId == req.GageId &&
            (r.Status == CalibRequestStatus.Pending || r.Status == CalibRequestStatus.Approved), ct);
        if (pending) return Result.Fail("Gage đang có yêu cầu hiệu chuẩn chờ xử lý.");

        var cr = new CalibRequest
        {
            GageId = req.GageId, VendorId = req.VendorId,
            RequestDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = CalibRequestStatus.Pending, CreatedBy = req.CreatedBy,
        };

        gage.HasPendingCalib = true;
        db.CalibRequests.Add(cr);
        await db.SaveChangesAsync(ct);
        return Result.Ok(cr.Id);
    }
}

// ── Approve CalibRequest ───────────────────────────────────────────────────

public record ApproveCalibRequestCommand(int RequestId) : IRequest<Result>;

public class ApproveCalibRequestCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<ApproveCalibRequestCommand, Result>
{
    public async Task<Result> Handle(ApproveCalibRequestCommand req, CancellationToken ct)
    {
        var cr = await db.CalibRequests.Include(r => r.Gage)
            .FirstOrDefaultAsync(r => r.Id == req.RequestId, ct);
        if (cr is null) return Result.Fail("Yêu cầu không tồn tại.");
        if (cr.Status != CalibRequestStatus.Pending) return Result.Fail("Yêu cầu không ở trạng thái Pending.");

        cr.Status = CalibRequestStatus.Approved;
        cr.Gage.StatusCode = GageStatusCode.Calib;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

// ── Complete Calibration (create CalibRecord) ─────────────────────────────

public record CompleteCalibrationCommand(
    int RequestId, int? ProcedureId, string? CalibratedBy,
    DateOnly CalibrationDate, string? AsFoundConditions,
    decimal? AdjustmentMade, decimal? Temperature, decimal? Humidity,
    string? StoragePath, int CreatedBy)
    : IRequest<Result<int>>;

public class CompleteCalibrationCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CompleteCalibrationCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CompleteCalibrationCommand req, CancellationToken ct)
    {
        var cr = await db.CalibRequests.Include(r => r.Gage)
            .FirstOrDefaultAsync(r => r.Id == req.RequestId, ct);
        if (cr is null) return Result.Fail("Yêu cầu không tồn tại.");
        if (cr.Status != CalibRequestStatus.Approved) return Result.Fail("Yêu cầu chưa được duyệt.");

        var record = new CalibRecord
        {
            CalibRequestId = req.RequestId, ProcedureId = req.ProcedureId,
            CalibratedBy = req.CalibratedBy, CalibrationDate = req.CalibrationDate,
            AsFoundConditions = req.AsFoundConditions, AdjustmentMade = req.AdjustmentMade,
            Temperature = req.Temperature, Humidity = req.Humidity,
            StoragePath = req.StoragePath, CreatedBy = req.CreatedBy,
        };

        // Cập nhật Gage + Request
        var gage = cr.Gage;
        gage.LastCalibration  = req.CalibrationDate;
        gage.StatusCode       = GageStatusCode.Valid;
        gage.HasPendingCalib  = false;
        cr.Status = CalibRequestStatus.Completed;

        db.CalibRecords.Add(record);
        await db.SaveChangesAsync(ct);
        return Result.Ok(record.Id);
    }
}

// ── Create CalibVendor ─────────────────────────────────────────────────────

public record CreateCalibVendorCommand(
    string Name, string? Contact, string? Address, string? Phone, string? Email)
    : IRequest<Result<CalibVendorDto>>;

public class CreateCalibVendorCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateCalibVendorCommand, Result<CalibVendorDto>>
{
    public async Task<Result<CalibVendorDto>> Handle(CreateCalibVendorCommand req, CancellationToken ct)
    {
        var v = new CalibVendor { Name = req.Name, Contact = req.Contact, Address = req.Address, Phone = req.Phone, Email = req.Email };
        db.CalibVendors.Add(v);
        await db.SaveChangesAsync(ct);
        return Result.Ok(new CalibVendorDto(v.Id, v.Name, v.Contact, v.Phone, v.Email));
    }
}
