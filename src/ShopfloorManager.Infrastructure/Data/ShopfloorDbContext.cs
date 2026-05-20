using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.Infrastructure.Data.Configurations;

namespace ShopfloorManager.Infrastructure.Data;

public class ShopfloorDbContext(DbContextOptions<ShopfloorDbContext> options)
    : DbContext(options), IShopfloorDbContext
{
    // ── Phase 1: Auth & HR ────────────────────────────────────
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<UserType> UserTypes => Set<UserType>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<WorkStatus> WorkStatuses => Set<WorkStatus>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // ── Phase 2: Production Core ──────────────────────────────
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<PartRev> PartRevs => Set<PartRev>();
    public DbSet<Routing> Routings => Set<Routing>();
    public DbSet<RoutingRev> RoutingRevs => Set<RoutingRev>();
    public DbSet<OpType> OpTypes => Set<OpType>();
    public DbSet<PartOp> PartOps => Set<PartOp>();
    public DbSet<PoLine> PoLines => Set<PoLine>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<FileType> FileTypes => Set<FileType>();
    public DbSet<TechDocument> TechDocuments => Set<TechDocument>();

    // ── Phase 3: Quality ──────────────────────────────────────
    public DbSet<Dimension> Dimensions => Set<Dimension>();
    public DbSet<MeasureValue> MeasureValues => Set<MeasureValue>();
    public DbSet<Ncr> Ncrs => Set<Ncr>();
    public DbSet<NcrLog> NcrLogs => Set<NcrLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Phase 1
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new MenuConfiguration());
        modelBuilder.ApplyConfiguration(new RoleMenuConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());

        // Phase 2
        modelBuilder.ApplyConfiguration(new PartConfiguration());
        modelBuilder.ApplyConfiguration(new PartRevConfiguration());
        modelBuilder.ApplyConfiguration(new RoutingConfiguration());
        modelBuilder.ApplyConfiguration(new RoutingRevConfiguration());
        modelBuilder.ApplyConfiguration(new PartOpConfiguration());
        modelBuilder.ApplyConfiguration(new JobConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new TechDocumentConfiguration());

        // Phase 3
        modelBuilder.ApplyConfiguration(new DimensionConfiguration());
        modelBuilder.ApplyConfiguration(new MeasureValueConfiguration());
        modelBuilder.ApplyConfiguration(new NcrConfiguration());
        modelBuilder.ApplyConfiguration(new NcrLogConfiguration());

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
            new Department { Id = 2, Code = "QC",    Name = "Quality Control",  CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new Department { Id = 3, Code = "PROD",  Name = "Production",        CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new Department { Id = 4, Code = "ENG",   Name = "Engineering",       CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) }
        );

        modelBuilder.Entity<WorkStatus>().HasData(
            new WorkStatus { Id = 1, Name = "Active",   IsWorking = true  },
            new WorkStatus { Id = 2, Name = "On Leave", IsWorking = false },
            new WorkStatus { Id = 3, Name = "Resigned", IsWorking = false }
        );

        modelBuilder.Entity<OpType>().HasData(
            new OpType { Id = 1, Code = "CNC",   Name = "CNC Machining",  Description = "CNC milling/turning"         },
            new OpType { Id = 2, Code = "INSP",  Name = "Inspection",     Description = "Quality inspection"          },
            new OpType { Id = 3, Code = "GRIND", Name = "Grinding",       Description = "Surface/cylindrical grinding"},
            new OpType { Id = 4, Code = "WIRE",  Name = "Wire EDM",       Description = "Wire electrical discharge"   },
            new OpType { Id = 5, Code = "MILL",  Name = "Milling",        Description = "Manual milling"              },
            new OpType { Id = 6, Code = "TURN",  Name = "Turning",        Description = "Manual turning"              }
        );

        // FileType seed — flags phân tích từ bảng filestype legacy (FormUpdateTechnology.cs)
        // IsPartNumber/IsRevision/IsOpNumber/IsJobNumber điều khiển MinIO path và naming convention
        modelBuilder.Entity<FileType>().HasData(
            // Drawing — gắn PartRev, không cần OPNumber
            new FileType { Id = 1, Code = "DRW",       Name = "Drawing",         Folder = "drawings",
                IsPartNumber = true, IsRevision = true,  IsOpNumber = false, IsJobNumber = false, SortOrder = 1 },
            // Route Card — gắn PartOp (cần OPNumber)
            new FileType { Id = 2, Code = "RC",        Name = "Route Card",      Folder = "routecards",
                IsPartNumber = true, IsRevision = true,  IsOpNumber = true,  IsJobNumber = false, SortOrder = 2 },
            // Fixture Drawing — gắn PartOp
            new FileType { Id = 3, Code = "FD",        Name = "Fixture Drawing", Folder = "fixtures",
                IsPartNumber = true, IsRevision = true,  IsOpNumber = true,  IsJobNumber = false, SortOrder = 3 },
            // G-code Fanuc — có segment, gắn PartOp
            new FileType { Id = 4, Code = "GC",        Name = "G-code (Fanuc)",  Folder = "gcodes",
                IsGcode = true, IsSegment = true,
                IsPartNumber = true, IsRevision = true,  IsOpNumber = true,  IsJobNumber = true,  SortOrder = 4 },
            // G-code MAZAK
            new FileType { Id = 5, Code = "MAZAK",     Name = "G-code (MAZAK)",  Folder = "gcodes",
                IsGcode = true, IsSegment = true,
                IsPartNumber = true, IsRevision = true,  IsOpNumber = true,  IsJobNumber = true,  SortOrder = 5 },
            // G-code Wire EDM
            new FileType { Id = 6, Code = "WC",        Name = "G-code (Wire EDM)", Folder = "gcodes",
                IsGcode = true, IsSegment = true,
                IsPartNumber = true, IsRevision = true,  IsOpNumber = true,  IsJobNumber = true,  SortOrder = 6 },
            // Setup Sheet — gắn PartOp
            new FileType { Id = 7, Code = "SETUP",     Name = "Setup Sheet",     Folder = "setups",
                IsPartNumber = true, IsRevision = true,  IsOpNumber = true,  IsJobNumber = false, SortOrder = 7 },
            // CAM file
            new FileType { Id = 8, Code = "CAM",       Name = "CAM File",        Folder = "cam",
                IsPartNumber = true, IsRevision = true,  IsOpNumber = true,  IsJobNumber = false, SortOrder = 8 }
        );
    }
}
