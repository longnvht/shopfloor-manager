using Microsoft.EntityFrameworkCore;
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

        var category = new GageCategory { Code = "LIN", Name = "Linear" };
        db.GageCategories.Add(category);
        var gageType = new GageType { Code = "MIC", Name = "Micrometer", Category = category };
        db.GageTypes.Add(gageType);
        var otherCategory = new GageCategory { Code = "ANG", Name = "Angle" };
        db.GageCategories.Add(otherCategory);
        var otherGageType = new GageType { Code = "PRO", Name = "Protractor", Category = otherCategory };
        db.GageTypes.Add(otherGageType);
        // Cùng category LIN với MIC nhưng khác GageType — dùng để chứng minh filter theo GageTypeId
        // chặt hơn filter theo CategoryCode (chỉ trả đúng loại, không trả cả 2 loại cùng category).
        var caliperType = new GageType { Code = "CAL", Name = "Caliper", Category = category };
        db.GageTypes.Add(caliperType);

        db.Gages.Add(new Gage { GageNo = "MIC-002", Description = "Micrometer 0-25mm", GageType = gageType, StatusCode = GageStatusCode.Valid, IsBorrowed = false });
        db.Gages.Add(new Gage { GageNo = "MIC-001", Description = "Micrometer 25-50mm", GageType = gageType, StatusCode = GageStatusCode.Valid, IsBorrowed = false });
        db.Gages.Add(new Gage { GageNo = "MIC-003", Description = "Borrowed micrometer", GageType = gageType, StatusCode = GageStatusCode.Valid, IsBorrowed = true });
        db.Gages.Add(new Gage { GageNo = "MIC-004", Description = "Expired micrometer", GageType = gageType, StatusCode = GageStatusCode.Expired, IsBorrowed = false });
        db.Gages.Add(new Gage { GageNo = "PRO-001", Description = "Protractor", GageType = otherGageType, StatusCode = GageStatusCode.Valid, IsBorrowed = false });
        db.Gages.Add(new Gage { GageNo = "CAL-001", Description = "Caliper 0-150mm", GageType = caliperType, StatusCode = GageStatusCode.Valid, IsBorrowed = false });

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
        Assert.Equal(["CAL-001", "MIC-001", "MIC-002", "PRO-001"], result.Value.Select(g => g.GageNo));
    }

    [Fact]
    public async Task Filters_by_category_code_when_provided()
    {
        var db = await SeedAsync();
        var handler = new GetMesGagesQueryHandler(db);

        var result = await handler.Handle(new GetMesGagesQuery("LIN"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["CAL-001", "MIC-001", "MIC-002"], result.Value.Select(g => g.GageNo));
    }

    [Fact]
    public async Task Filters_by_gage_type_id_more_precisely_than_category()
    {
        var db = await SeedAsync();
        var micType = await db.GageTypes.FirstAsync(t => t.Code == "MIC");
        var handler = new GetMesGagesQueryHandler(db);

        var result = await handler.Handle(new GetMesGagesQuery(CategoryCode: null, GageTypeId: micType.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Cùng category LIN có cả CAL-001, nhưng filter theo GageTypeId chỉ trả đúng MIC.
        Assert.Equal(["MIC-001", "MIC-002"], result.Value.Select(g => g.GageNo));
    }
}
