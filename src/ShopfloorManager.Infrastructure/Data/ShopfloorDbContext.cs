using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Infrastructure.Data.Configurations;

namespace ShopfloorManager.Infrastructure.Data;

public class ShopfloorDbContext(DbContextOptions<ShopfloorDbContext> options)
    : DbContext(options), IShopfloorDbContext
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<UserType> UserTypes => Set<UserType>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<WorkStatus> WorkStatuses => Set<WorkStatus>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new MenuConfiguration());
        modelBuilder.ApplyConfiguration(new RoleMenuConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        SeedStaticData(modelBuilder);
    }

    private static void SeedStaticData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Administrator" },
            new Role { Id = 2, Name = "Manager" },
            new Role { Id = 3, Name = "Engineer" },
            new Role { Id = 4, Name = "QC Inspector" },
            new Role { Id = 5, Name = "Operator" },
            new Role { Id = 6, Name = "Planner" }
        );

        modelBuilder.Entity<Department>().HasData(
            new Department { Id = 1, Code = "ADMIN", Name = "Administration", CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new Department { Id = 2, Code = "QC", Name = "Quality Control", CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new Department { Id = 3, Code = "PROD", Name = "Production", CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new Department { Id = 4, Code = "ENG", Name = "Engineering", CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) }
        );

        modelBuilder.Entity<WorkStatus>().HasData(
            new WorkStatus { Id = 1, Name = "Active", IsWorking = true },
            new WorkStatus { Id = 2, Name = "On Leave", IsWorking = false },
            new WorkStatus { Id = 3, Name = "Resigned", IsWorking = false }
        );
    }
}
