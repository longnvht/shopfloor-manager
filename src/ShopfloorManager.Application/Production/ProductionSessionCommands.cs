using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

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

// ===== CLAIM (operator chọn product) =====

public record ClaimSessionCommand(int ProductId, int PartOpId, string MachineCode, int UserId)
    : IRequest<Result<ProductionSessionDto>>;

public class ClaimSessionCommandValidator : AbstractValidator<ClaimSessionCommand>
{
    public ClaimSessionCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.PartOpId).GreaterThan(0);
        RuleFor(x => x.MachineCode).NotEmpty().MaximumLength(20);
    }
}

public class ClaimSessionHandler(IShopfloorDbContext db)
    : IRequestHandler<ClaimSessionCommand, Result<ProductionSessionDto>>
{
    public async Task<Result<ProductionSessionDto>> Handle(ClaimSessionCommand req, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([req.ProductId], ct);
        if (product is null)
            return Result.Fail("Sản phẩm không tồn tại.");

        var existing = await db.ProductionSessions
            .FirstOrDefaultAsync(s => s.ProductId == req.ProductId
                                   && s.Status == SessionStatus.Open, ct);
        if (existing is not null)
            return Result.Fail($"Sản phẩm đang được sử dụng trên máy {existing.MachineCode}.");

        var session = new ProductionSession
        {
            ProductId   = req.ProductId,
            PartOpId    = req.PartOpId,
            MachineCode = req.MachineCode,
            Status      = SessionStatus.Open,
            ClaimedAt   = DateTimeOffset.UtcNow,
            ClaimedBy   = req.UserId,
            CreatedAt   = DateTimeOffset.UtcNow
        };

        db.ProductionSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return Result.Ok(Mapper.Map(session, product.SerialNumber));
    }
}

// ===== START (bấm "Bắt đầu") =====

public record StartSessionCommand(int SessionId) : IRequest<Result<ProductionSessionDto>>;

public class StartSessionHandler(IShopfloorDbContext db)
    : IRequestHandler<StartSessionCommand, Result<ProductionSessionDto>>
{
    public async Task<Result<ProductionSessionDto>> Handle(StartSessionCommand req, CancellationToken ct)
    {
        var session = await db.ProductionSessions
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == req.SessionId, ct);

        if (session is null) return Result.Fail("Phiên gia công không tồn tại.");
        if (session.Status != SessionStatus.Open) return Result.Fail("Phiên không ở trạng thái mở.");
        if (session.StartedAt.HasValue) return Result.Fail("Phiên đã được bắt đầu.");

        session.StartedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Ok(Mapper.Map(session, session.Product.SerialNumber));
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
            .Where(s => s.MachineCode == req.MachineCode && s.Status == SessionStatus.Open)
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

// ===== HELPER =====

file static class Mapper
{
    internal static ProductionSessionDto Map(ProductionSession s, string serial) => new(
        s.Id, s.ProductId, serial, s.PartOpId, s.MachineCode,
        s.Status, s.ClaimedAt, s.StartedAt, s.CompletedAt,
        s.ClaimedBy, s.CancelledBy, s.Note);
}
