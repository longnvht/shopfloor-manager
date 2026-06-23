using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Infrastructure.Data;
using Xunit;

namespace ShopfloorManager.Application.Tests;

file class NoopRealtimeNotifier : IRealtimeNotifier
{
    public Task NotifyNcrCreatedAsync(NcrDto ncr, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyMeasureSubmittedAsync(MeasureValueDto measure, CancellationToken ct = default) => Task.CompletedTask;
}

public class SaveMeasureCommandGageTests
{
    private static async Task<(ShopfloorDbContext Db, Dimension Dim, Product Product, Gage Gage)> SeedAsync()
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
        var job = new Job { JobNumber = "JB-26-050", PartRev = partRev, RoutingRev = routingRev };
        db.Jobs.Add(job);
        var product = new Product { Job = job, SerialNumber = "001" };
        db.Products.Add(product);
        var dim = new Dimension { PartOp = op, BalloonNumber = "1", NominalValue = 50, TolerancePlus = 0.01m, ToleranceMinus = 0.01m, MaxValue = 50.01m, MinValue = 49.99m, Unit = "mm" };
        db.Dimensions.Add(dim);
        var gage = new Gage { GageNo = "MIC-001", Description = "Micrometer", StatusCode = GageStatusCode.Valid };
        db.Gages.Add(gage);
        await db.SaveChangesAsync();

        return (db, dim, product, gage);
    }

    [Fact]
    public async Task Save_measure_persists_the_selected_gage_id()
    {
        var (db, dim, product, gage) = await SeedAsync();
        var handler = new SaveMeasureCommandHandler(db, new NoopRealtimeNotifier());

        var result = await handler.Handle(
            new SaveMeasureCommand(dim.Id, product.Id, 50m, null, false, null, null, RequesterId: 1, GageId: gage.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var saved = await db.MeasureValues.FindAsync(result.Value.Id);
        Assert.Equal(gage.Id, saved!.GageId);
    }

    [Fact]
    public async Task Save_measure_without_gage_leaves_gage_id_null()
    {
        var (db, dim, product, _) = await SeedAsync();
        var handler = new SaveMeasureCommandHandler(db, new NoopRealtimeNotifier());

        var result = await handler.Handle(
            new SaveMeasureCommand(dim.Id, product.Id, 50m, null, false, null, null, RequesterId: 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var saved = await db.MeasureValues.FindAsync(result.Value.Id);
        Assert.Null(saved!.GageId);
    }
}
