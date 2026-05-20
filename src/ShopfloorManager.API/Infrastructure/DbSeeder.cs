using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Infrastructure.Data;

namespace ShopfloorManager.API.Infrastructure;

/// <summary>
/// Tạo admin user mặc định nếu chưa có user nào trong DB.
/// Chỉ chạy trong Development.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAdminAsync(ShopfloorDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
        if (adminRole is null) return;

        var admin = new User
        {
            UserLogin = "admin",
            // Admin@123
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 4),
            Name = "System Administrator",
            RoleId = adminRole.Id,
            IsActive = true,
            FirstLogin = false
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}
