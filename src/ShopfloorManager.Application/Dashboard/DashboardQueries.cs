using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;

namespace ShopfloorManager.Application.Dashboard;

// ── DTOs ──────────────────────────────────────────────────────────────────

public record DashboardOverviewDto(
    int ActiveJobs,
    int CompletedJobsThisMonth,
    int OpenNcrs,
    int TotalParts,
    int TotalGages,
    int GagesExpiredOrDamaged,
    int PendingCalibRequests,
    double PassRatePercent);

public record ProductionKpiDto(
    int JobsRunning,
    int SerialsDoneToday,
    int SerialsTotal,
    double AvgProgressPercent);

public record QualityKpiDto(
    long TotalMeasured,
    long PassCount,
    long FailCount,
    double PassRate,
    int OpenNcrs,
    int ClosedNcrsThisMonth);

// ── Queries ────────────────────────────────────────────────────────────────

public record GetDashboardOverviewQuery(DateOnly? Date = null) : IRequest<Result<DashboardOverviewDto>>;

public class GetDashboardOverviewQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetDashboardOverviewQuery, Result<DashboardOverviewDto>>
{
    public async Task<Result<DashboardOverviewDto>> Handle(GetDashboardOverviewQuery req, CancellationToken ct)
    {
        var today    = req.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var activeJobs   = await db.Jobs.CountAsync(j => !j.IsComplete, ct);
        var completedJobs = await db.Jobs.CountAsync(j => j.IsComplete && j.CreatedAt >= monthStart, ct);
        var openNcrs     = await db.Ncrs.CountAsync(n => n.Status == NcrStatus.Open, ct);
        var totalParts   = await db.Parts.CountAsync(ct);

        // Gage stats
        var totalGages   = await db.Gages.CountAsync(ct);
        var invalidGages = await db.Gages.CountAsync(g =>
            g.StatusCode == GageStatusCode.Expired || g.StatusCode == GageStatusCode.Damaged, ct);
        var pendingCalib = await db.CalibRequests.CountAsync(r =>
            r.Status == CalibRequestStatus.Pending || r.Status == CalibRequestStatus.Approved, ct);

        // Pass rate (last 30 days)
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        var passCount  = await db.MeasureValues.CountAsync(m => m.Result == MeasureResult.Pass && m.MeasuredAt >= cutoff, ct);
        var totalMeasure = await db.MeasureValues.CountAsync(m => m.MeasuredAt >= cutoff, ct);
        var passRate = totalMeasure > 0 ? Math.Round(passCount * 100.0 / totalMeasure, 1) : 0;

        return Result.Ok(new DashboardOverviewDto(
            activeJobs, completedJobs, openNcrs, totalParts,
            totalGages, invalidGages, pendingCalib, passRate));
    }
}

public record GetProductionKpiQuery : IRequest<Result<ProductionKpiDto>>;

public class GetProductionKpiQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetProductionKpiQuery, Result<ProductionKpiDto>>
{
    public async Task<Result<ProductionKpiDto>> Handle(GetProductionKpiQuery _, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var todayEnd   = todayStart.AddDays(1);

        var runningJobs    = await db.Jobs.CountAsync(j => !j.IsComplete, ct);
        var serialsDoneToday = await db.ProductionSessions.CountAsync(s =>
            s.Status == "complete" && s.CompletedAt >= todayStart && s.CompletedAt < todayEnd, ct);
        var totalSerials   = await db.Products.CountAsync(ct);
        var doneSerials    = await db.Products.CountAsync(p => p.IsComplete, ct);
        var avgProgress    = totalSerials > 0 ? Math.Round(doneSerials * 100.0 / totalSerials, 1) : 0;

        return Result.Ok(new ProductionKpiDto(
            runningJobs, serialsDoneToday, totalSerials, avgProgress));
    }
}

public record GetQualityKpiQuery(int Days = 30) : IRequest<Result<QualityKpiDto>>;

public class GetQualityKpiQueryHandler(IShopfloorDbContext db)
    : IRequestHandler<GetQualityKpiQuery, Result<QualityKpiDto>>
{
    public async Task<Result<QualityKpiDto>> Handle(GetQualityKpiQuery req, CancellationToken ct)
    {
        var cutoff     = DateTimeOffset.UtcNow.AddDays(-req.Days);
        var monthStart = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var passCount  = await db.MeasureValues.CountAsync(m => m.Result == MeasureResult.Pass && m.MeasuredAt >= cutoff, ct);
        var failCount  = await db.MeasureValues.CountAsync(m => m.Result == MeasureResult.Fail && m.MeasuredAt >= cutoff, ct);
        var totalMeas  = passCount + failCount;
        var passRate   = totalMeas > 0 ? Math.Round(passCount * 100.0 / totalMeas, 1) : 0;

        var openNcrs   = await db.Ncrs.CountAsync(n => n.Status == NcrStatus.Open, ct);
        var closedNcrs = await db.Ncrs.CountAsync(n => n.Status == NcrStatus.Closed && n.RaisedAt >= monthStart, ct);

        return Result.Ok(new QualityKpiDto(totalMeas, passCount, failCount, passRate, openNcrs, closedNcrs));
    }
}
