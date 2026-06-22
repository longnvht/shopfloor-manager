using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Infrastructure.Data;
using Xunit;

namespace ShopfloorManager.Application.Tests;

public class GetJobOpsQueryHandlerTests
{
    // Mirrors the real legacy routing: ...OP90 -> OP100 STP -> OP110 INS -> OP120 PPG -> OP130 INS
    private static async Task<(ShopfloorDbContext Db, Job Job, PartOp Op60, PartOp Op110Ins, PartOp Op120, PartOp Op130Ins)> SeedRoutingAsync()
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
        var op100 = new PartOp { RoutingRev = routingRev, OpNumber = "100", OpNumberSort = 100m, OpType = mlaType, IsVisible = true };
        var op110Ins = new PartOp { RoutingRev = routingRev, OpNumber = "110", OpNumberSort = 110m, OpType = insType, IsVisible = true };
        var op120 = new PartOp { RoutingRev = routingRev, OpNumber = "120", OpNumberSort = 120m, OpType = ppgType, IsVisible = true };
        var op130Ins = new PartOp { RoutingRev = routingRev, OpNumber = "130", OpNumberSort = 130m, OpType = insType, IsVisible = true };
        db.PartOps.AddRange(op60, op100, op110Ins, op120, op130Ins);

        var job = new Job { JobNumber = "JB-26-031", PartRev = partRev, RoutingRev = routingRev };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        db.Dimensions.AddRange(
            new Dimension { PartOpId = op60.Id, BalloonNumber = "1", NominalValue = 50, TolerancePlus = 0.02m, ToleranceMinus = 0.02m, MaxValue = 50.02m, MinValue = 49.98m, Unit = "mm" },
            new Dimension { PartOpId = op60.Id, BalloonNumber = "2", NominalValue = 80, TolerancePlus = 0.1m, ToleranceMinus = 0.1m, MaxValue = 80.1m, MinValue = 79.9m, Unit = "mm" });
        await db.SaveChangesAsync();

        return (db, job, op60, op110Ins, op120, op130Ins);
    }

    [Fact]
    public async Task Normal_op_reports_its_own_dimension_count_and_op_type_code()
    {
        var (db, job, op60, _, _, _) = await SeedRoutingAsync();
        var handler = new GetJobOpsQueryHandler(db);

        var result = await handler.Handle(new GetJobOpsQuery(job.Id), CancellationToken.None);

        var dto = result.Value.Single(o => o.Id == op60.Id);
        Assert.Equal(2, dto.DimCount);
        Assert.Equal("MLA", dto.OpTypeCode);
    }

    [Fact]
    public async Task Ins_op_aggregates_dimension_count_from_prior_ops_only()
    {
        var (db, job, _, op110Ins, op120, _) = await SeedRoutingAsync();
        var handler = new GetJobOpsQueryHandler(db);

        var result = await handler.Handle(new GetJobOpsQuery(job.Id), CancellationToken.None);

        var op110Dto = result.Value.Single(o => o.Id == op110Ins.Id);
        var op120Dto = result.Value.Single(o => o.Id == op120.Id);
        Assert.Equal(2, op110Dto.DimCount);   // OP110 INS sees OP60's 2 dimensions
        Assert.Equal("INSP", op110Dto.OpTypeCode);
        Assert.Equal(0, op120Dto.DimCount);   // OP120 PPG owns no dimension itself
    }

    [Fact]
    public async Task Second_ins_op_after_the_first_sees_the_same_aggregated_set()
    {
        var (db, job, _, _, _, op130Ins) = await SeedRoutingAsync();
        var handler = new GetJobOpsQueryHandler(db);

        var result = await handler.Handle(new GetJobOpsQuery(job.Id), CancellationToken.None);

        var op130Dto = result.Value.Single(o => o.Id == op130Ins.Id);
        Assert.Equal(2, op130Dto.DimCount);   // Still OP60's 2 dimensions — OP110/OP120 contribute none
    }
}
