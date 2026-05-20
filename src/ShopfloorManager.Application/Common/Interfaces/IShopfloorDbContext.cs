using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Common.Interfaces;

public interface IShopfloorDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<UserType> UserTypes { get; }
    DbSet<Position> Positions { get; }
    DbSet<WorkStatus> WorkStatuses { get; }
    DbSet<Role> Roles { get; }
    DbSet<Menu> Menus { get; }
    DbSet<RoleMenu> RoleMenus { get; }
    DbSet<User> Users { get; }
    DbSet<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
