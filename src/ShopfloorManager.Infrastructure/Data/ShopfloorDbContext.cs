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
    public DbSet<DimensionCategory> DimensionCategories => Set<DimensionCategory>();
    public DbSet<Dimension> Dimensions => Set<Dimension>();
    public DbSet<MeasureValue> MeasureValues => Set<MeasureValue>();
    public DbSet<NcrReason> NcrReasons => Set<NcrReason>();
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
        modelBuilder.ApplyConfiguration(new DimensionCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new DimensionConfiguration());
        modelBuilder.ApplyConfiguration(new MeasureValueConfiguration());
        modelBuilder.ApplyConfiguration(new NcrReasonConfiguration());
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

        // FileType seed — path theo spec 05_technical_documents.md
        // IsPartNumber/IsRevision/IsOpNumber/IsJobNumber điều khiển MinIO path:
        //   Part-level  : {folder}/{part_number}/{revision}/{filename}
        //   Standard OP : {folder}/{part_number}/{op_number}/{revision}/{filename}
        //   Job+OP      : {folder}/{job_number}/{op_number}/{filename}
        modelBuilder.Entity<FileType>().HasData(
            // Part-level: drawings/{part_number}/{revision}/...
            new FileType { Id = 1, Code = "DRW", Name = "Drawing",         Folder = "drawings",
                IsPartNumber = true,  IsRevision = true,  IsOpNumber = false, IsJobNumber = false, SortOrder = 1 },
            // Standard OP (Part+OP): gcodes/{part_number}/{op_number}/{revision}/...
            new FileType { Id = 2, Code = "GCD", Name = "G-Code",          Folder = "gcodes",
                IsGcode = true, IsSegment = true,
                IsPartNumber = true,  IsRevision = true,  IsOpNumber = true,  IsJobNumber = false, SortOrder = 2 },
            // Job+OP: routecards/{job_number}/{op_number}/...
            new FileType { Id = 3, Code = "RTC", Name = "Route Card",      Folder = "routecards",
                IsPartNumber = false, IsRevision = false, IsOpNumber = true,  IsJobNumber = true,  SortOrder = 3 },
            // Job+OP: fixtures/{job_number}/{op_number}/...
            new FileType { Id = 4, Code = "FXT", Name = "Fixture Drawing", Folder = "fixtures",
                IsPartNumber = false, IsRevision = false, IsOpNumber = true,  IsJobNumber = true,  SortOrder = 4 },
            // Standard OP (Part+OP): threads/{part_number}/{op_number}/{revision}/...
            new FileType { Id = 5, Code = "THD", Name = "Thread Drawing",  Folder = "threads",
                IsPartNumber = true,  IsRevision = true,  IsOpNumber = true,  IsJobNumber = false, SortOrder = 5 },
            new FileType { Id = 6, Code = "TLS", Name = "Tool List",       Folder = "tools",
                IsPartNumber = true,  IsRevision = true,  IsOpNumber = true,  IsJobNumber = false, SortOrder = 6 },
            new FileType { Id = 7, Code = "CAM", Name = "CAM File",        Folder = "cam",
                IsPartNumber = true,  IsRevision = true,  IsOpNumber = true,  IsJobNumber = false, SortOrder = 7 },
            // Part-level: cad/{part_number}/{revision}/...
            new FileType { Id = 8, Code = "CAD", Name = "CAD Drawing",     Folder = "cad",
                IsPartNumber = true,  IsRevision = true,  IsOpNumber = false, IsJobNumber = false, SortOrder = 8 }
        );

        // DimensionCategory seed (từ 06_dimensions_fai.md)
        modelBuilder.Entity<DimensionCategory>().HasData(
            new DimensionCategory { Id = 1, Code = "LIN", Name = "Linear",    Description = "Thước cặp, panme" },
            new DimensionCategory { Id = 2, Code = "ANG", Name = "Angular",   Description = "Thước góc" },
            new DimensionCategory { Id = 3, Code = "THD", Name = "Thread",    Description = "Dưỡng ren, ring gauge" },
            new DimensionCategory { Id = 4, Code = "GEO", Name = "Geometric", Description = "CMM, dial indicator" },
            new DimensionCategory { Id = 5, Code = "SFC", Name = "Surface",   Description = "Surface tester" }
        );

        // NcrReason seed (từ 07_ncr.md)
        var seedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        modelBuilder.Entity<NcrReason>().HasData(
            new NcrReason { Id = 1, Name = "Tool wear",      Tag = "TOOL",  SortOrder = 1,  CreatedAt = seedDate },
            new NcrReason { Id = 2, Name = "Setup error",    Tag = "SETUP", SortOrder = 2,  CreatedAt = seedDate },
            new NcrReason { Id = 3, Name = "Drawing error",  Tag = "DRW",   SortOrder = 3,  CreatedAt = seedDate },
            new NcrReason { Id = 4, Name = "Wrong material", Tag = "MAT",   SortOrder = 4,  CreatedAt = seedDate },
            new NcrReason { Id = 5, Name = "Machine error",  Tag = "MACH",  SortOrder = 5,  CreatedAt = seedDate },
            new NcrReason { Id = 6, Name = "CMM error",      Tag = "CMM",   SortOrder = 6,  CreatedAt = seedDate },
            new NcrReason { Id = 7, Name = "Other",          Tag = "OTHER", SortOrder = 99, CreatedAt = seedDate }
        );
    }
}
