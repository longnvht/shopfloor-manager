using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Infrastructure.Data;
using Xunit;

namespace ShopfloorManager.Application.Tests;

public class QcInlineRateCommandsTests
{
    private static async Task<(ShopfloorDbContext Db, Job Job, PartOp Op)> SeedAsync(decimal factoryDefault = 10m)
    {
        var db = TestDbContextFactory.Create();

        var part = new Part { PartNumber = "SHAFT-50H6", Description = "Trục dẫn động" };
        db.Parts.Add(part);
        var partRev = new PartRev { Part = part, RevCode = "B" };
        db.PartRevs.Add(partRev);
        var routing = new Routing { PartRev = partRev };
        db.Routings.Add(routing);
        var routingRev = new RoutingRev { Routing = routing, RevCode = "R1" };
        db.RoutingRevs.Add(routingRev);
        var opType = new OpType { Code = "MLA", Name = "Medium Lathe" };
        db.OpTypes.Add(opType);
        var op = new PartOp { RoutingRev = routingRev, OpNumber = "60", OpNumberSort = 60m, OpType = opType, IsVisible = true };
        db.PartOps.Add(op);
        var job = new Job { JobNumber = "JB-26-031", PartRev = partRev, RoutingRev = routingRev };
        db.Jobs.Add(job);

        db.QcInlineRates.Add(new QcInlineRate { JobId = null, PartOpId = null, RatePercent = factoryDefault, IsActive = true });
        await db.SaveChangesAsync();

        return (db, job, op);
    }

    [Fact]
    public async Task Effective_rate_falls_back_to_factory_default_when_no_override_exists()
    {
        var (db, job, op) = await SeedAsync(factoryDefault: 10m);
        var handler = new GetEffectiveQcInlineRateQueryHandler(db);

        var result = await handler.Handle(new GetEffectiveQcInlineRateQuery(job.Id, op.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10m, result.Value);
    }

    [Fact]
    public async Task Job_and_partop_specific_override_wins_over_job_only_and_default()
    {
        var (db, job, op) = await SeedAsync(factoryDefault: 10m);
        db.QcInlineRates.Add(new QcInlineRate { JobId = job.Id, PartOpId = null, RatePercent = 20m, IsActive = true });
        db.QcInlineRates.Add(new QcInlineRate { JobId = job.Id, PartOpId = op.Id, RatePercent = 50m, IsActive = true });
        await db.SaveChangesAsync();
        var handler = new GetEffectiveQcInlineRateQueryHandler(db);

        var result = await handler.Handle(new GetEffectiveQcInlineRateQuery(job.Id, op.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(50m, result.Value);
    }

    [Fact]
    public async Task Job_only_override_wins_over_partop_only_override()
    {
        var (db, job, op) = await SeedAsync(factoryDefault: 10m);
        db.QcInlineRates.Add(new QcInlineRate { JobId = job.Id, PartOpId = null, RatePercent = 20m, IsActive = true });
        db.QcInlineRates.Add(new QcInlineRate { JobId = null, PartOpId = op.Id, RatePercent = 30m, IsActive = true });
        await db.SaveChangesAsync();
        var handler = new GetEffectiveQcInlineRateQueryHandler(db);

        var result = await handler.Handle(new GetEffectiveQcInlineRateQuery(job.Id, op.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(20m, result.Value);
    }

    [Fact]
    public async Task Inactive_override_is_ignored_falls_back_to_default()
    {
        var (db, job, op) = await SeedAsync(factoryDefault: 10m);
        db.QcInlineRates.Add(new QcInlineRate { JobId = job.Id, PartOpId = null, RatePercent = 20m, IsActive = false });
        await db.SaveChangesAsync();
        var handler = new GetEffectiveQcInlineRateQueryHandler(db);

        var result = await handler.Handle(new GetEffectiveQcInlineRateQuery(job.Id, op.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10m, result.Value);
    }

    [Fact]
    public async Task Create_then_update_rate_round_trips_via_handlers()
    {
        var (db, job, op) = await SeedAsync();
        var createHandler = new CreateQcInlineRateCommandHandler(db);
        var updateHandler = new UpdateQcInlineRateCommandHandler(db);

        var created = await createHandler.Handle(new CreateQcInlineRateCommand(job.Id, op.Id, 35m), CancellationToken.None);
        Assert.True(created.IsSuccess);
        Assert.Equal(35m, created.Value.RatePercent);

        var updated = await updateHandler.Handle(new UpdateQcInlineRateCommand(created.Value.Id, 45m, true), CancellationToken.None);
        Assert.True(updated.IsSuccess);
        Assert.Equal(45m, updated.Value.RatePercent);
    }

    [Fact]
    public async Task Update_cannot_change_isactive_to_false_on_the_factory_default_row()
    {
        var db = TestDbContextFactory.Create();
        var defaultRow = new QcInlineRate { JobId = null, PartOpId = null, RatePercent = 10m, IsActive = true };
        db.QcInlineRates.Add(defaultRow);
        await db.SaveChangesAsync();
        var handler = new UpdateQcInlineRateCommandHandler(db);

        var result = await handler.Handle(new UpdateQcInlineRateCommand(defaultRow.Id, 15m, false), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
