using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;
using ShopfloorManager.Infrastructure.Data;
using Xunit;

namespace ShopfloorManager.Application.Tests;

public class GetFaiSheetQueryHandlerTests
{
    private static async Task<(ShopfloorDbContext Db, Job Job, PartOp Op60, PartOp Op110Ins, PartOp Op130Ins, Product Product)> SeedAsync()
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

        var insType = new OpType { Code = "INSP", Name = "INSPECTION" };
        var mlaType = new OpType { Code = "MLA", Name = "Medium Lathe" };
        var ppgType = new OpType { Code = "PPG", Name = "PHOSPHATING" };
        db.OpTypes.AddRange(insType, mlaType, ppgType);

        var op60 = new PartOp { RoutingRev = routingRev, OpNumber = "60", OpNumberSort = 60m, OpType = mlaType, IsVisible = true };
        var op110Ins = new PartOp { RoutingRev = routingRev, OpNumber = "110", OpNumberSort = 110m, OpType = insType, IsVisible = true };
        var op120 = new PartOp { RoutingRev = routingRev, OpNumber = "120", OpNumberSort = 120m, OpType = ppgType, IsVisible = true };
        var op130Ins = new PartOp { RoutingRev = routingRev, OpNumber = "130", OpNumberSort = 130m, OpType = insType, IsVisible = true };
        db.PartOps.AddRange(op60, op110Ins, op120, op130Ins);

        var job = new Job { JobNumber = "JB-26-031", PartRev = partRev, RoutingRev = routingRev };
        db.Jobs.Add(job);
        var product = new Product { Job = job, SerialNumber = "001" };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        db.Dimensions.AddRange(
            new Dimension { PartOpId = op60.Id, BalloonNumber = "1", NominalValue = 50, TolerancePlus = 0.02m, ToleranceMinus = 0.02m, MaxValue = 50.02m, MinValue = 49.98m, Unit = "mm", IsFinal = true },
            new Dimension { PartOpId = op60.Id, BalloonNumber = "2", NominalValue = 80, TolerancePlus = 0.1m, ToleranceMinus = 0.1m, MaxValue = 80.1m, MinValue = 79.9m, Unit = "mm", IsFinal = true });
        await db.SaveChangesAsync();

        return (db, job, op60, op110Ins, op130Ins, product);
    }

    [Fact]
    public async Task Normal_op_returns_only_its_own_dimensions_with_null_op_number()
    {
        var (db, job, op60, _, _, _) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, op60.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Dimensions.Count);
        Assert.All(result.Value.Dimensions, d => Assert.Null(d.OpNumber));
    }

    [Fact]
    public async Task Ins_op_aggregates_dimensions_from_prior_ops_and_tags_op_number()
    {
        var (db, job, op60, op110Ins, _, _) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, op110Ins.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Dimensions.Count);
        Assert.All(result.Value.Dimensions, d => Assert.Equal("60", d.OpNumber));
    }

    [Fact]
    public async Task Second_ins_op_sees_the_same_aggregated_dimensions_as_the_first()
    {
        var (db, job, _, _, op130Ins, _) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, op130Ins.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Dimensions.Count);
        Assert.All(result.Value.Dimensions, d => Assert.Equal("60", d.OpNumber));
    }

    [Fact]
    public async Task Rows_have_one_cell_per_dimension_for_every_product()
    {
        var (db, job, op60, _, _, product) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, op60.Id), CancellationToken.None);

        var row = result.Value.Rows.Single(r => r.ProductId == product.Id);
        Assert.Equal(2, row.Cells.Count);
    }
}
