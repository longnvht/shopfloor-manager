using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Infrastructure.Data;
using Xunit;

namespace ShopfloorManager.Application.Tests;

public class QcFinalDimensionAggregationTests
{
    private static async Task<(ShopfloorDbContext Db, Job Job, PartOp Op60, PartOp InsOp)> SeedAsync()
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
        db.OpTypes.AddRange(insType, mlaType);

        var op60 = new PartOp { RoutingRev = routingRev, OpNumber = "60", OpNumberSort = 60m, OpType = mlaType, IsVisible = true };
        var insOp = new PartOp { RoutingRev = routingRev, OpNumber = "110", OpNumberSort = 110m, OpType = insType, IsVisible = true };
        db.PartOps.AddRange(op60, insOp);

        var job = new Job { JobNumber = "JB-26-040", PartRev = partRev, RoutingRev = routingRev };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        // op60 (kỹ thuật): balloon "1" IsFinal=true (QC tái sử dụng), balloon "2" IsFinal=false
        // (chỉ kỹ thuật dùng, không liên quan QC Final), balloon "4" IsFinal=true (QC tái sử dụng,
        // không bị override bởi insOp).
        db.Dimensions.AddRange(
            new Dimension { PartOpId = op60.Id, BalloonNumber = "1", NominalValue = 50, TolerancePlus = 0.01m, ToleranceMinus = 0.01m, MaxValue = 50.01m, MinValue = 49.99m, Unit = "mm", IsFinal = true },
            new Dimension { PartOpId = op60.Id, BalloonNumber = "2", NominalValue = 10, TolerancePlus = 0.1m, ToleranceMinus = 0.1m, MaxValue = 10.1m, MinValue = 9.9m, Unit = "mm", IsFinal = false },
            new Dimension { PartOpId = op60.Id, BalloonNumber = "4", NominalValue = 30, TolerancePlus = 0.05m, ToleranceMinus = 0.05m, MaxValue = 30.05m, MinValue = 29.95m, Unit = "mm", IsFinal = true });

        // insOp (QC tự tạo): balloon "1" với dung sai khác (theo yêu cầu khách hàng) — phải override
        // bản balloon "1" của op60 dù bản đó IsFinal=true; balloon "3" — dimension chỉ QC kiểm tra,
        // không tồn tại ở OP nào khác trong routing.
        db.Dimensions.AddRange(
            new Dimension { PartOpId = insOp.Id, BalloonNumber = "1", NominalValue = 50, TolerancePlus = 0.03m, ToleranceMinus = 0.03m, MaxValue = 50.03m, MinValue = 49.97m, Unit = "mm" },
            new Dimension { PartOpId = insOp.Id, BalloonNumber = "3", NominalValue = 5, TolerancePlus = 0.02m, ToleranceMinus = 0.02m, MaxValue = 5.02m, MinValue = 4.98m, Unit = "mm" });
        await db.SaveChangesAsync();

        return (db, job, op60, insOp);
    }

    [Fact]
    public async Task Ins_op_returns_exactly_balloons_1_3_4_not_2()
    {
        var (db, job, _, insOp) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, insOp.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var balloons = result.Value.Dimensions.Select(d => d.BalloonNumber).OrderBy(b => b).ToList();
        Assert.Equal(new[] { "1", "3", "4" }, balloons);
    }

    [Fact]
    public async Task Ins_op_own_dimension_overrides_prior_final_dimension_with_same_balloon()
    {
        var (db, job, op60, insOp) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, insOp.Id), CancellationToken.None);

        var balloon1 = result.Value.Dimensions.Single(d => d.BalloonNumber == "1");
        Assert.Equal(insOp.Id, balloon1.PartOpId);
        Assert.Equal(50.03m, balloon1.MaxValue);
        Assert.Null(balloon1.OpNumber);
    }

    [Fact]
    public async Task Ins_op_includes_its_own_dimension_with_no_op_number_tag()
    {
        var (db, job, _, insOp) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, insOp.Id), CancellationToken.None);

        var balloon3 = result.Value.Dimensions.Single(d => d.BalloonNumber == "3");
        Assert.Equal(insOp.Id, balloon3.PartOpId);
        Assert.Null(balloon3.OpNumber);
    }

    [Fact]
    public async Task Ins_op_includes_prior_final_dimension_tagged_with_its_op_number()
    {
        var (db, job, op60, insOp) = await SeedAsync();
        var handler = new GetFaiSheetQueryHandler(db);

        var result = await handler.Handle(new GetFaiSheetQuery(job.Id, insOp.Id), CancellationToken.None);

        var balloon4 = result.Value.Dimensions.Single(d => d.BalloonNumber == "4");
        Assert.Equal(op60.Id, balloon4.PartOpId);
        Assert.Equal("60", balloon4.OpNumber);
    }
}
