using ShopfloorManager.Domain.Entities;
using Xunit;

namespace ShopfloorManager.Application.Tests;

public class SmokeTests
{
    [Fact]
    public async Task Can_insert_and_query_a_part()
    {
        using var db = TestDbContextFactory.Create();
        db.Parts.Add(new Part { PartNumber = "SHAFT-50H6", Description = "Trục dẫn động" });
        await db.SaveChangesAsync();

        var part = db.Parts.Single(p => p.PartNumber == "SHAFT-50H6");

        Assert.Equal("Trục dẫn động", part.Description);
    }
}
