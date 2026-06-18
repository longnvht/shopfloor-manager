using ShopfloorManager.Application.Production;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Domain.Enums;
using ShopfloorManager.Infrastructure.Data;
using Xunit;

namespace ShopfloorManager.Application.Tests;

public class GetJobsQueryHandlerTests
{
    private static async Task<(ShopfloorDbContext Db, Job JobWithNcrs, Job JobWithoutNcrs)> SeedAsync()
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

        var jobWithNcrs = new Job { JobNumber = "JB-26-001", PartRev = partRev, RoutingRev = routingRev };
        var jobWithoutNcrs = new Job { JobNumber = "JB-26-002", PartRev = partRev, RoutingRev = routingRev };
        db.Jobs.AddRange(jobWithNcrs, jobWithoutNcrs);
        await db.SaveChangesAsync();

        db.Ncrs.AddRange(
            new Ncr { JobId = jobWithNcrs.Id, Description = "Fail OD", Status = NcrStatus.Open, RaisedBy = 1 },
            new Ncr { JobId = jobWithNcrs.Id, Description = "Fail length", Status = NcrStatus.Open, RaisedBy = 1 },
            new Ncr { JobId = jobWithNcrs.Id, Description = "Closed one", Status = NcrStatus.Closed, RaisedBy = 1 });
        await db.SaveChangesAsync();

        return (db, jobWithNcrs, jobWithoutNcrs);
    }

    [Fact]
    public async Task Counts_only_open_ncrs_per_job()
    {
        var (db, jobWithNcrs, jobWithoutNcrs) = await SeedAsync();
        var handler = new GetJobsQueryHandler(db);

        var result = await handler.Handle(new GetJobsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dtoWithNcrs = result.Value.Items.Single(j => j.Id == jobWithNcrs.Id);
        var dtoWithoutNcrs = result.Value.Items.Single(j => j.Id == jobWithoutNcrs.Id);
        Assert.Equal(2, dtoWithNcrs.OpenNcrCount);
        Assert.Equal(0, dtoWithoutNcrs.OpenNcrCount);
    }
}
