using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;
using ShopfloorManager.Shared.Constants;

namespace ShopfloorManager.Application.Production;

// ===== DTOs =====

public record ProductionSessionDto(
    int Id,
    int ProductId,
    string SerialNumber,
    int PartOpId,
    string MachineCode,
    string Status,
    DateTimeOffset ClaimedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int ClaimedBy,
    int? CancelledBy,
    string? Note);

public record ActiveSessionDto(
    int SessionId,
    string MachineCode,
    int ClaimedBy,
    string ClaimedByName,
    int ProductId,
    string SerialNumber,
    int PartOpId,
    string Status,
    DateTimeOffset ClaimedAt,
    DateTimeOffset? StartedAt,
    // Job/OP context needed to restore WorkContext on resume
    int JobId,
    string JobNumber,
    string PartNumber,
    string OpNumber);

public record ProductWithSessionDto(
    int ProductId,
    string SerialNumber,
    int SortOrder,
    // Session info (null = Available)
    int? SessionId,
    string? SessionStatus,
    string? MachineCode,
    DateTimeOffset? ClaimedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);

// ===== BEGIN (operator bấm "Bắt đầu" — tạo session và bắt đầu ngay) =====

public record BeginSessionCommand(int ProductId, int PartOpId, string MachineCode, int UserId, string Role)
    : IRequest<Result<ProductionSessionDto>>;

public class BeginSessionCommandValidator : AbstractValidator<BeginSessionCommand>
{
    public BeginSessionCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.PartOpId).GreaterThan(0);
        RuleFor(x => x.MachineCode).NotEmpty().MaximumLength(20);
    }
}

public class BeginSessionHandler(IShopfloorDbContext db)
    : IRequestHandler<BeginSessionCommand, Result<ProductionSessionDto>>
{
    // Chỉ role có nhiệm vụ vận hành máy mới được tạo session — QC/Engineer/Manager chỉ inspect/view.
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        AppConstants.Roles.Operator, AppConstants.Roles.Leader, AppConstants.Roles.Admin
    };

    public async Task<Result<ProductionSessionDto>> Handle(BeginSessionCommand req, CancellationToken ct)
    {
        if (!AllowedRoles.Contains(req.Role))
            return Result.Fail("Role không có quyền bắt đầu phiên gia công.");

        var product = await db.Products.FindAsync([req.ProductId], ct);
        if (product is null)
            return Result.Fail("Sản phẩm không tồn tại.");

        // Ràng buộc per-product: không cho bắt đầu nếu product đang inprogress ở máy khác
        var productInProgress = await db.ProductionSessions
            .FirstOrDefaultAsync(s => s.ProductId == req.ProductId
                                   && s.Status == SessionStatus.Open
                                   && s.StartedAt != null, ct);
        if (productInProgress is not null)
            return Result.Fail($"Sản phẩm đang được gia công trên máy {productInProgress.MachineCode}.");

        // Ràng buộc per-machine: không cho bắt đầu nếu máy đang có session inprogress
        var machineInProgress = await db.ProductionSessions
            .FirstOrDefaultAsync(s => s.MachineCode == req.MachineCode
                                   && s.Status == SessionStatus.Open
                                   && s.StartedAt != null, ct);
        if (machineInProgress is not null)
            return Result.Fail($"Máy {req.MachineCode} đang gia công sản phẩm khác. Kết thúc phiên hiện tại trước.");

        var now = DateTimeOffset.UtcNow;
        var session = new ProductionSession
        {
            ProductId   = req.ProductId,
            PartOpId    = req.PartOpId,
            MachineCode = req.MachineCode,
            Status      = SessionStatus.Open,
            ClaimedAt   = now,
            StartedAt   = now,
            ClaimedBy   = req.UserId,
            CreatedAt   = now
        };

        db.ProductionSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return Result.Ok(Mapper.Map(session, product.SerialNumber));
    }
}

// ===== COMPLETE (bấm "Kết thúc") =====

public record CompleteSessionCommand(int SessionId) : IRequest<Result<ProductionSessionDto>>;

public class CompleteSessionHandler(IShopfloorDbContext db)
    : IRequestHandler<CompleteSessionCommand, Result<ProductionSessionDto>>
{
    public async Task<Result<ProductionSessionDto>> Handle(CompleteSessionCommand req, CancellationToken ct)
    {
        var session = await db.ProductionSessions
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == req.SessionId, ct);

        if (session is null) return Result.Fail("Phiên gia công không tồn tại.");
        if (session.Status != SessionStatus.Open) return Result.Fail("Phiên không ở trạng thái mở.");

        session.Status      = SessionStatus.Complete;
        session.CompletedAt = DateTimeOffset.UtcNow;
        if (!session.StartedAt.HasValue)
            session.StartedAt = session.CompletedAt;

        await db.SaveChangesAsync(ct);
        return Result.Ok(Mapper.Map(session, session.Product.SerialNumber));
    }
}

// ===== CANCEL (supervisor unlock) =====

public record CancelSessionCommand(int SessionId, int SupervisorId, string? Note)
    : IRequest<Result<ProductionSessionDto>>;

public class CancelSessionHandler(IShopfloorDbContext db)
    : IRequestHandler<CancelSessionCommand, Result<ProductionSessionDto>>
{
    public async Task<Result<ProductionSessionDto>> Handle(CancelSessionCommand req, CancellationToken ct)
    {
        var session = await db.ProductionSessions
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == req.SessionId, ct);

        if (session is null) return Result.Fail("Phiên gia công không tồn tại.");
        if (session.Status != SessionStatus.Open) return Result.Fail("Chỉ có thể huỷ phiên đang mở.");

        session.Status      = SessionStatus.Cancelled;
        session.CancelledBy = req.SupervisorId;
        session.Note        = req.Note;
        await db.SaveChangesAsync(ct);
        return Result.Ok(Mapper.Map(session, session.Product.SerialNumber));
    }
}

// ===== QUERY: Active session on a machine =====

public record GetActiveSessionQuery(string MachineCode) : IRequest<Result<ActiveSessionDto?>>;

public class GetActiveSessionHandler(IShopfloorDbContext db)
    : IRequestHandler<GetActiveSessionQuery, Result<ActiveSessionDto?>>
{
    public async Task<Result<ActiveSessionDto?>> Handle(GetActiveSessionQuery req, CancellationToken ct)
    {
        var session = await db.ProductionSessions
            .Where(s => s.MachineCode == req.MachineCode && s.Status == SessionStatus.Open && s.StartedAt != null)
            .Include(s => s.ClaimedByUser)
            .Include(s => s.Product).ThenInclude(p => p.Job).ThenInclude(j => j.PartRev).ThenInclude(r => r.Part)
            .Include(s => s.PartOp)
            .FirstOrDefaultAsync(ct);

        if (session is null) return Result.Ok<ActiveSessionDto?>(null);

        var dto = new ActiveSessionDto(
            session.Id,
            session.MachineCode,
            session.ClaimedBy,
            session.ClaimedByUser?.Name ?? session.ClaimedBy.ToString(),
            session.ProductId,
            session.Product.SerialNumber,
            session.PartOpId,
            session.Status,
            session.ClaimedAt,
            session.StartedAt,
            session.Product.JobId,
            session.Product.Job.JobNumber,
            session.Product.Job.PartRev.Part.PartNumber,
            session.PartOp.OpNumber);

        return Result.Ok<ActiveSessionDto?>(dto);
    }
}

// ===== FORCE-COMPLETE (Leader/Admin kết thúc session của người khác) =====

public record ForceCompleteSessionCommand(int SessionId, int ForcedByUserId)
    : IRequest<Result<ProductionSessionDto>>;

public class ForceCompleteSessionHandler(IShopfloorDbContext db)
    : IRequestHandler<ForceCompleteSessionCommand, Result<ProductionSessionDto>>
{
    public async Task<Result<ProductionSessionDto>> Handle(ForceCompleteSessionCommand req, CancellationToken ct)
    {
        var session = await db.ProductionSessions
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == req.SessionId, ct);

        if (session is null) return Result.Fail("Phiên gia công không tồn tại.");
        if (session.Status != SessionStatus.Open) return Result.Fail("Chỉ có thể kết thúc phiên đang mở.");

        session.Status      = SessionStatus.Complete;
        session.CompletedAt = DateTimeOffset.UtcNow;
        session.CancelledBy = req.ForcedByUserId;   // ghi lại ai force-complete
        if (!session.StartedAt.HasValue)
            session.StartedAt = session.CompletedAt;

        await db.SaveChangesAsync(ct);
        return Result.Ok(Mapper.Map(session, session.Product.SerialNumber));
    }
}

// ===== QUERY: Products with session status =====

public record GetProductsWithSessionQuery(int JobId, int PartOpId)
    : IRequest<Result<List<ProductWithSessionDto>>>;

public class GetProductsWithSessionHandler(IShopfloorDbContext db)
    : IRequestHandler<GetProductsWithSessionQuery, Result<List<ProductWithSessionDto>>>
{
    public async Task<Result<List<ProductWithSessionDto>>> Handle(
        GetProductsWithSessionQuery req, CancellationToken ct)
    {
        var products = await db.Products
            .Where(p => p.JobId == req.JobId)
            .OrderBy(p => p.SortOrder)
            .Select(p => new
            {
                p.Id, p.SerialNumber, p.SortOrder,
                Session = db.ProductionSessions
                    .Where(s => s.ProductId == p.Id && s.PartOpId == req.PartOpId
                             && s.Status != SessionStatus.Cancelled)
                    .OrderByDescending(s => s.ClaimedAt)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var result = products.Select(p => new ProductWithSessionDto(
            p.Id,
            p.SerialNumber,
            p.SortOrder ?? 0,
            p.Session?.Id,
            p.Session?.Status,
            p.Session?.MachineCode,
            p.Session?.ClaimedAt,
            p.Session?.StartedAt,
            p.Session?.CompletedAt
        )).ToList();

        return Result.Ok(result);
    }
}

// ===== QUERY: Daily summary cho 1 máy =====

public record DailySummaryDto(
    int CompletedCount,
    int TotalActiveMinutes,
    int PassCount,
    int FailCount);

public record GetDailySummaryQuery(string MachineCode, DateOnly Date)
    : IRequest<Result<DailySummaryDto>>;

public class GetDailySummaryQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetDailySummaryQuery, Result<DailySummaryDto>>
{
    public async Task<Result<DailySummaryDto>> Handle(GetDailySummaryQuery req, CancellationToken ct)
    {
        var startUtc = req.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc   = startUtc.AddDays(1);

        var sessions = await db.ProductionSessions
            .Where(s => s.MachineCode == req.MachineCode
                     && s.Status == SessionStatus.Complete
                     && s.CompletedAt >= startUtc
                     && s.CompletedAt < endUtc)
            .ToListAsync(ct);

        var completedCount      = sessions.Count;
        var totalActiveMinutes  = (int)sessions
            .Where(s => s.StartedAt.HasValue && s.CompletedAt.HasValue)
            .Sum(s => (s.CompletedAt!.Value - s.StartedAt!.Value).TotalMinutes);

        int passCount = 0, failCount = 0;
        if (sessions.Count > 0)
        {
            var productIds = sessions.Select(s => s.ProductId).ToHashSet();
            var partOpIds  = sessions.Select(s => s.PartOpId).ToHashSet();

            passCount = await db.MeasureValues
                .CountAsync(m => productIds.Contains(m.ProductId)
                              && partOpIds.Contains(m.PartOpId)
                              && m.Result == MeasureResult.Pass, ct);
            failCount = await db.MeasureValues
                .CountAsync(m => productIds.Contains(m.ProductId)
                              && partOpIds.Contains(m.PartOpId)
                              && m.Result == MeasureResult.Fail, ct);
        }

        return Result.Ok(new DailySummaryDto(completedCount, totalActiveMinutes, passCount, failCount));
    }
}

// ===== HELPER =====

file static class Mapper
{
    internal static ProductionSessionDto Map(ProductionSession s, string serial) => new(
        s.Id, s.ProductId, serial, s.PartOpId, s.MachineCode,
        s.Status, s.ClaimedAt, s.StartedAt, s.CompletedAt,
        s.ClaimedBy, s.CancelledBy, s.Note);
}
