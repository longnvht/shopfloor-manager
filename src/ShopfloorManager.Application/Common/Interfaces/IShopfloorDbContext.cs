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
    DbSet<Part> Parts { get; }
    DbSet<PartRev> PartRevs { get; }
    DbSet<Routing> Routings { get; }
    DbSet<RoutingRev> RoutingRevs { get; }
    DbSet<OpType> OpTypes { get; }
    DbSet<PartOp> PartOps { get; }
    DbSet<PoLine> PoLines { get; }
    DbSet<Job> Jobs { get; }
    DbSet<Product> Products { get; }
    DbSet<FileType> FileTypes { get; }
    DbSet<TechDocument> TechDocuments { get; }

    // Phase 4 — Desktop MES
    DbSet<ProductionSession> ProductionSessions { get; }

    // Phase 3 — Quality
    DbSet<DimensionCategory> DimensionCategories { get; }
    DbSet<Dimension> Dimensions { get; }
    DbSet<MeasureValue> MeasureValues { get; }
    DbSet<NcrReason> NcrReasons { get; }
    DbSet<Ncr> Ncrs { get; }
    DbSet<NcrLog> NcrLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
