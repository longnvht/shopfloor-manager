using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Infrastructure.Data;
using Xunit;

namespace ShopfloorManager.Application.Tests;

public class GetFaiSheetQueryGageTypeTests
{
    [Fact]
    public async Task Dimension_with_gage_type_assigned_exposes_gage_type_id_and_code()
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
        var job = new Job { JobNumber = "JB-26-060", PartRev = partRev, RoutingRev = routingRev };
        db.Jobs.Add(job);

        var gageType = new GageType { Code = "MIC", Name = "Micrometer" };
        db.GageTypes.Add(gageType);
        var dim = new Dimension
        {
            PartOp = op, BalloonNumber = "1", NominalValue = 50,
            TolerancePlus = 0.01m, ToleranceMinus = 0.01m, MaxValue = 50.01m, MinValue = 49.99m,
            Unit = "mm", GageType = gageType,
        };
        db.Dimensions.Add(dim);
        await db.SaveChangesAsync();

        var handler = new GetFaiSheetQueryHandler(db);
        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, op.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = result.Value.Dimensions.Single();
        Assert.Equal(gageType.Id, dto.GageTypeId);
        Assert.Equal("MIC", dto.GageTypeCode);
    }
}
