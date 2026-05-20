using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Common.Interfaces;

public interface IShopfloorDbContext
{
    // Phase 1 — Auth & HR
    DbSet<Department> Departments { get; }
    DbSet<UserType> UserTypes { get; }
    DbSet<Position> Positions { get; }
    DbSet<WorkStatus> WorkStatuses { get; }
    DbSet<Role> Roles { get; }
    DbSet<Menu> Menus { get; }
    DbSet<RoleMenu> RoleMenus { get; }
    DbSet<User> Users { get; }
    DbSet<AuditLog> AuditLogs { get; }

    // Phase 2 — Production Core
    DbSet<OpType> OpTypes { get; }
    DbSet<PoLine> PoLines { get; }
    DbSet<Part> Parts { get; }
    DbSet<Job> Jobs { get; }
    DbSet<PartOp> PartOps { get; }
    DbSet<Product> Products { get; }
    DbSet<FileType> FileTypes { get; }
    DbSet<TechDocument> TechDocuments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
