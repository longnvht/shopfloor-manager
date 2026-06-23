using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Infrastructure.Data;
using Xunit;

namespace ShopfloorManager.Application.Tests;

public class BeginSessionCommandTests
{
    private static async Task<(ShopfloorDbContext Db, Product Product, PartOp Op)> SeedAsync()
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
        var product = new Product { Job = job, SerialNumber = "001" };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        return (db, product, op);
    }

    [Theory]
    [InlineData("QC Inspector")]
    [InlineData("Engineer")]
    [InlineData("Manager")]
    public async Task Non_production_roles_cannot_begin_a_session(string role)
    {
        var (db, product, op) = await SeedAsync();
        var handler = new BeginSessionHandler(db);

        var result = await handler.Handle(
            new BeginSessionCommand(product.Id, op.Id, "CNC-01", UserId: 1, Role: role), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData("Operator")]
    [InlineData("Leader")]
    [InlineData("Administrator")]
    public async Task Production_roles_can_begin_a_session(string role)
    {
        var (db, product, op) = await SeedAsync();
        var handler = new BeginSessionHandler(db);

        var result = await handler.Handle(
            new BeginSessionCommand(product.Id, op.Id, "CNC-01", UserId: 1, Role: role), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
