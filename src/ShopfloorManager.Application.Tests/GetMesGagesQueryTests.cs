using ShopfloorManager.Application.GageManagement;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Infrastructure.Data;
using Xunit;

namespace ShopfloorManager.Application.Tests;

public class GetMesGagesQueryTests
{
    private static async Task<ShopfloorDbContext> SeedAsync()
    {
        var db = TestDbContextFactory.Create();

        var category = new DimensionCategory { Code = "LIN", Name = "Linear" };
        db.DimensionCategories.Add(category);
        var gageType = new GageType { Code = "MIC", Name = "Micrometer", Category = category };
        db.GageTypes.Add(gageType);
        var otherCategory = new DimensionCategory { Code = "ANG", Name = "Angle" };
        db.DimensionCategories.Add(otherCategory);
        var otherGageType = new GageType { Code = "PRO", Name = "Protractor", Category = otherCategory };
        db.GageTypes.Add(otherGageType);

        db.Gages.Add(new Gage { GageNo = "MIC-002", Description = "Micrometer 0-25mm", GageType = gageType, StatusCode = GageStatusCode.Valid, IsBorrowed = false });
        db.Gages.Add(new Gage { GageNo = "MIC-001", Description = "Micrometer 25-50mm", GageType = gageType, StatusCode = GageStatusCode.Valid, IsBorrowed = false });
        db.Gages.Add(new Gage { GageNo = "MIC-003", Description = "Borrowed micrometer", GageType = gageType, StatusCode = GageStatusCode.Valid, IsBorrowed = true });
        db.Gages.Add(new Gage { GageNo = "MIC-004", Description = "Expired micrometer", GageType = gageType, StatusCode = GageStatusCode.Expired, IsBorrowed = false });
        db.Gages.Add(new Gage { GageNo = "PRO-001", Description = "Protractor", GageType = otherGageType, StatusCode = GageStatusCode.Valid, IsBorrowed = false });

        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Only_returns_valid_and_not_borrowed_gages()
    {
        var db = await SeedAsync();
        var handler = new GetMesGagesQueryHandler(db);

        var result = await handler.Handle(new GetMesGagesQuery(null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["MIC-001", "MIC-002", "PRO-001"], result.Value.Select(g => g.GageNo));
    }

    [Fact]
    public async Task Filters_by_category_code_when_provided()
    {
        var db = await SeedAsync();
        var handler = new GetMesGagesQueryHandler(db);

        var result = await handler.Handle(new GetMesGagesQuery("LIN"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["MIC-001", "MIC-002"], result.Value.Select(g => g.GageNo));
    }
}
