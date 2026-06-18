using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Infrastructure.Data;

namespace ShopfloorManager.Application.Tests;

public static class TestDbContextFactory
{
    public static ShopfloorDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ShopfloorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ShopfloorDbContext(options);
    }
}
