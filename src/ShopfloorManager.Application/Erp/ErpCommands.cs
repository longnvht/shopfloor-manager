using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Erp;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record ErpConnectionDto(
    int Id, string Name, string ErpType, string BaseUrl,
    string? Company, string? Username, bool HasPassword, bool IsActive);

public record ErpPreviewDto(
    List<ErpPreviewRowDto> Rows,
    int TotalCount,
    List<string> Warnings);

public record ErpPreviewRowDto(
    string PartNumber, string? PartDescription, string? Revision,
    string JobNumber, string? PoNumber, string? PoLine,
    int? RunQty, string? ShipBy,
    string OpNumber, string? OpTypeCode, string? OpDescription,
    decimal? SetupTime, decimal? ProdTime);

// ── List connections ──────────────────────────────────────────────────────────

public record GetErpConnectionsQuery : IRequest<List<ErpConnectionDto>>;

public class GetErpConnectionsQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetErpConnectionsQuery, List<ErpConnectionDto>>
{
    public async Task<List<ErpConnectionDto>> Handle(GetErpConnectionsQuery _, CancellationToken ct)
    {
        var list = await db.ErpConnections
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
        return list.Select(ToDto).ToList();
    }

    static ErpConnectionDto ToDto(ErpConnection c) =>
        new(c.Id, c.Name, c.ErpType, c.BaseUrl, c.Company, c.Username,
            !string.IsNullOrEmpty(c.Password), c.IsActive);
}

// ── Create connection ─────────────────────────────────────────────────────────

public record CreateErpConnectionCommand(
    string Name, string ErpType, string BaseUrl,
    string? Company, string? Username, string? Password)
    : IRequest<Result<ErpConnectionDto>>;

public class CreateErpConnectionCommandValidator : AbstractValidator<CreateErpConnectionCommand>
{
    public CreateErpConnectionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.ErpType).NotEmpty();
        RuleFor(x => x.BaseUrl).NotEmpty();
    }
}

public class CreateErpConnectionCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<CreateErpConnectionCommand, Result<ErpConnectionDto>>
{
    public async Task<Result<ErpConnectionDto>> Handle(CreateErpConnectionCommand req, CancellationToken ct)
    {
        var conn = new ErpConnection
        {
            Name = req.Name, ErpType = req.ErpType,
            BaseUrl = req.BaseUrl.TrimEnd('/'),
            Company = req.Company, Username = req.Username, Password = req.Password,
            IsActive = true,
        };
        db.ErpConnections.Add(conn);
        await db.SaveChangesAsync(ct);
        return Result.Ok(new ErpConnectionDto(conn.Id, conn.Name, conn.ErpType, conn.BaseUrl,
            conn.Company, conn.Username, !string.IsNullOrEmpty(conn.Password), conn.IsActive));
    }
}

// ── Update connection ─────────────────────────────────────────────────────────

public record UpdateErpConnectionCommand(
    int Id, string Name, string ErpType, string BaseUrl,
    string? Company, string? Username, string? Password, bool IsActive)
    : IRequest<Result<ErpConnectionDto>>;

public class UpdateErpConnectionCommandValidator : AbstractValidator<UpdateErpConnectionCommand>
{
    public UpdateErpConnectionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.BaseUrl).NotEmpty();
    }
}

public class UpdateErpConnectionCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<UpdateErpConnectionCommand, Result<ErpConnectionDto>>
{
    public async Task<Result<ErpConnectionDto>> Handle(UpdateErpConnectionCommand req, CancellationToken ct)
    {
        var conn = await db.ErpConnections.FindAsync([req.Id], ct);
        if (conn is null) return Result.Fail("Không tìm thấy kết nối ERP.");

        conn.Name = req.Name; conn.ErpType = req.ErpType;
        conn.BaseUrl = req.BaseUrl.TrimEnd('/');
        conn.Company = req.Company; conn.Username = req.Username;
        if (!string.IsNullOrEmpty(req.Password)) conn.Password = req.Password;
        conn.IsActive = req.IsActive;

        await db.SaveChangesAsync(ct);
        return Result.Ok(new ErpConnectionDto(conn.Id, conn.Name, conn.ErpType, conn.BaseUrl,
            conn.Company, conn.Username, !string.IsNullOrEmpty(conn.Password), conn.IsActive));
    }
}

// ── Test connection ───────────────────────────────────────────────────────────

public record TestErpConnectionQuery(int Id) : IRequest<Result<bool>>;

public class TestErpConnectionQueryHandler(IShopfloorDbContext db, IErpConnectorFactory factory)
    : IRequestHandler<TestErpConnectionQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(TestErpConnectionQuery req, CancellationToken ct)
    {
        var conn = await db.ErpConnections.FindAsync([req.Id], ct);
        if (conn is null) return Result.Fail("Không tìm thấy kết nối ERP.");

        var connector = factory.Create(conn.ErpType, conn.BaseUrl, conn.Company, conn.Username, conn.Password);
        var ok = await connector.TestConnectionAsync(ct);
        return Result.Ok(ok);
    }
}

// ── Preview (fetch from ERP without importing) ────────────────────────────────

public record GetErpPreviewQuery(
    int ConnectionId, DateOnly? DateFrom, DateOnly? DateTo, string? PoNumber)
    : IRequest<Result<ErpPreviewDto>>;

public class GetErpPreviewQueryHandler(IShopfloorDbContext db, IErpConnectorFactory factory)
    : IRequestHandler<GetErpPreviewQuery, Result<ErpPreviewDto>>
{
    public async Task<Result<ErpPreviewDto>> Handle(GetErpPreviewQuery req, CancellationToken ct)
    {
        var conn = await db.ErpConnections.FindAsync([req.ConnectionId], ct);
        if (conn is null) return Result.Fail("Không tìm thấy kết nối ERP.");

        var connector = factory.Create(conn.ErpType, conn.BaseUrl, conn.Company, conn.Username, conn.Password);
        var filter = new ErpImportFilter(req.DateFrom, req.DateTo, req.PoNumber);
        var result = await connector.FetchPreviewAsync(filter, ct);

        var rows = result.Rows.Select(r => new ErpPreviewRowDto(
            r.PartNumber, r.PartDescription, r.Revision,
            r.JobNumber, r.PoNumber, r.PoLineNumber,
            r.RunQty, r.ShipBy?.ToString("yyyy-MM-dd"),
            r.OpNumber, r.OpTypeCode, r.OpDescription,
            r.SetupTime, r.ProdTime)).ToList();

        return Result.Ok(new ErpPreviewDto(rows, result.TotalCount, result.Warnings));
    }
}

// ── Import (preview → ImportJobBatchCommand) ──────────────────────────────────
// Endpoint phía API lấy preview rồi gọi ImportJobBatchCommand — xem ErpController.
