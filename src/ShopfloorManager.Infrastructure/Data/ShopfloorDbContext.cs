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

    // ── Phase 4: Desktop MES ─────────────────────────────────
    public DbSet<ProductionSession> ProductionSessions => Set<ProductionSession>();

    // ── Master Data ───────────────────────────────────────────
    public DbSet<Machine>      Machines      => Set<Machine>();
    public DbSet<MachineEvent> MachineEvents => Set<MachineEvent>();

    // ── Gage Management ───────────────────────────────────────
    public DbSet<GageType>          GageTypes          => Set<GageType>();
    public DbSet<GageLocation>      GageLocations      => Set<GageLocation>();
    public DbSet<GageSlot>          GageSlots          => Set<GageSlot>();
    public DbSet<Gage>              Gages              => Set<Gage>();
    public DbSet<BorrowTransaction> BorrowTransactions => Set<BorrowTransaction>();

    // ── Calibration ───────────────────────────────────────────
    public DbSet<CalibVendor>    CalibVendors    => Set<CalibVendor>();
    public DbSet<CalibProcedure> CalibProcedures => Set<CalibProcedure>();
    public DbSet<CalibRequest>   CalibRequests   => Set<CalibRequest>();
    public DbSet<CalibRecord>    CalibRecords    => Set<CalibRecord>();

    // ── Planning ──────────────────────────────────────────────
    public DbSet<Shift>           Shifts           => Set<Shift>();
    public DbSet<BreakTime>       BreakTimes       => Set<BreakTime>();
    public DbSet<PlanningItem>    PlanningItems    => Set<PlanningItem>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();

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

        // Phase 4
        modelBuilder.ApplyConfiguration(new ProductionSessionConfiguration());

        // Master Data
        modelBuilder.ApplyConfiguration(new MachineConfiguration());
        modelBuilder.Entity<MachineEvent>(b => {
            b.Property(e => e.Id).UseIdentityByDefaultColumn();
            b.Property(e => e.TmMode).HasMaxLength(20);
            b.Property(e => e.AtMode).HasMaxLength(20);
            b.Property(e => e.RunMode).HasMaxLength(20);
            b.Property(e => e.Alarm).HasMaxLength(100);
            b.Property(e => e.AlarmMessage).HasMaxLength(500);
            b.HasOne(e => e.Machine).WithMany()
                .HasForeignKey(e => e.MachineId).OnDelete(DeleteBehavior.Restrict);
        });

        // Gage Management
        modelBuilder.ApplyConfiguration(new GageTypeConfiguration());
        modelBuilder.ApplyConfiguration(new GageLocationConfiguration());
        modelBuilder.ApplyConfiguration(new GageSlotConfiguration());
        modelBuilder.ApplyConfiguration(new GageConfiguration());
        modelBuilder.ApplyConfiguration(new BorrowTransactionConfiguration());

        // Calibration
        modelBuilder.ApplyConfiguration(new CalibVendorConfiguration());
        modelBuilder.ApplyConfiguration(new CalibProcedureConfiguration());
        modelBuilder.ApplyConfiguration(new CalibRequestConfiguration());
        modelBuilder.ApplyConfiguration(new CalibRecordConfiguration());

        // Planning
        modelBuilder.ApplyConfiguration(new ShiftConfiguration());
        modelBuilder.ApplyConfiguration(new BreakTimeConfiguration());
        modelBuilder.ApplyConfiguration(new PlanningItemConfiguration());
        modelBuilder.ApplyConfiguration(new ShiftAssignmentConfiguration());

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
            new Role { Id = 6, Name = "Planner" },
            new Role { Id = 7, Name = "Leader" }     // Tổ trưởng — quản lý ca, force-finish session
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

        // NcrReason seed — gắn DepartmentId để NCR dialog filter đúng
        // PROD=3, QC=2, ENG=4; id=7 (Other) giữ null — code sentinel "Khác" xử lý
        var seedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        modelBuilder.Entity<NcrReason>().HasData(
            // PROD — Sản xuất
            new NcrReason { Id = 1,  Name = "Mòn dụng cụ cắt",         Tag = "TOOL",  DepartmentId = 3, SortOrder = 1,  CreatedAt = seedDate },
            new NcrReason { Id = 2,  Name = "Lỗi gá đặt",              Tag = "SETUP", DepartmentId = 3, SortOrder = 2,  CreatedAt = seedDate },
            new NcrReason { Id = 5,  Name = "Lỗi máy gia công",         Tag = "MACH",  DepartmentId = 3, SortOrder = 3,  CreatedAt = seedDate },
            new NcrReason { Id = 8,  Name = "Lỗi đồ gá / fixture",      Tag = "FXT",   DepartmentId = 3, SortOrder = 4,  CreatedAt = seedDate },
            new NcrReason { Id = 9,  Name = "Lỗi vận hành / thao tác",  Tag = "OPR",   DepartmentId = 3, SortOrder = 5,  CreatedAt = seedDate },
            new NcrReason { Id = 10, Name = "Sai thông số cắt gọt",     Tag = "PARAM", DepartmentId = 3, SortOrder = 6,  CreatedAt = seedDate },

            // QC — Kiểm tra chất lượng
            new NcrReason { Id = 6,  Name = "Lỗi thiết bị đo CMM",      Tag = "CMM",   DepartmentId = 2, SortOrder = 1,  CreatedAt = seedDate },
            new NcrReason { Id = 11, Name = "Dụng cụ đo chưa hiệu chuẩn", Tag = "CALIB", DepartmentId = 2, SortOrder = 2,  CreatedAt = seedDate },
            new NcrReason { Id = 12, Name = "Sai phương pháp kiểm tra",  Tag = "INSP",  DepartmentId = 2, SortOrder = 3,  CreatedAt = seedDate },

            // ENG — Kỹ thuật công nghệ
            new NcrReason { Id = 3,  Name = "Lỗi bản vẽ / dung sai",    Tag = "DRW",   DepartmentId = 4, SortOrder = 1,  CreatedAt = seedDate },
            new NcrReason { Id = 4,  Name = "Sai vật liệu",              Tag = "MAT",   DepartmentId = 4, SortOrder = 2,  CreatedAt = seedDate },
            new NcrReason { Id = 13, Name = "Lỗi lập trình CAM/G-code",  Tag = "CAM",   DepartmentId = 4, SortOrder = 3,  CreatedAt = seedDate },
            new NcrReason { Id = 14, Name = "Lỗi quy trình công nghệ",   Tag = "PROC",  DepartmentId = 4, SortOrder = 4,  CreatedAt = seedDate },
            new NcrReason { Id = 15, Name = "Dung sai thiết kế quá chặt", Tag = "TOL",  DepartmentId = 4, SortOrder = 5,  CreatedAt = seedDate },

            // Không gắn phòng ban — fallback (không hiện trong ComboBox filter)
            new NcrReason { Id = 7,  Name = "Other",                     Tag = "OTHER", SortOrder = 99, CreatedAt = seedDate }
        );

        // Machine seed — migrated from legacy MySQL (120 machines)
        modelBuilder.Entity<Machine>().HasData(
            new Machine { Id = 1,   Code = "5MI",           Name = "5 AX MILLING MACHINE",                  MachineType = "5MI",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 2,   Code = "BTA1",          Name = "DEEP HOLE DRILLING 1",                  MachineType = "BTA",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 3,   Code = "300L",          Name = "300L LONG LATHE MACHINE",               MachineType = "LLA40", IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 4,   Code = "3100L",         Name = "3100L LONG LATHE MACHINE",              MachineType = "LLA40", IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 5,   Code = "3100XL",        Name = "3100XL LONG LATHE MACHINE",             MachineType = "LLA40", IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 6,   Code = "400LLT",        Name = "400LLT LONG LATHE MACHINE",             MachineType = "LLA60", IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 7,   Code = "MORISEIKI",     Name = "MORISEIKI LONG LATHE MACHINE",          MachineType = "LLA35", IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 8,   Code = "MAC",           Name = "MACHIINING",                            MachineType = "MAC",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 9,   Code = "MAL1",          Name = "MANUAL LATHE 1 (LEMANTHE)",             MachineType = "MAL",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 10,  Code = "MAL2",          Name = "MANUAL LATHE 2 (IKEGAI DAB-36)",        MachineType = "MAL",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 11,  Code = "MAM1",          Name = "MANUAL MILLING",                        MachineType = "MAM",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 12,  Code = "DMV650",        Name = "DMV650 MILLING MACHINE",                MachineType = "MIL",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 13,  Code = "DNM5700",       Name = "DNM5700 MILLING MACHINE",               MachineType = "MIL",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 14,  Code = "MV6030",        Name = "MV6030 MILLING MACHINE",                MachineType = "MIL",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 15,  Code = "VTV-200C",      Name = "VTV-200C MILLING MACHINE",              MachineType = "MIL",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 16,  Code = "400ML1",        Name = "400ML1 TURN LATHE MACHINE",             MachineType = "MLA60", IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 17,  Code = "400ML2",        Name = "400ML2 TURN LATHE MACHINE",             MachineType = "MLA36", IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 18,  Code = "400ML3",        Name = "400ML3 TURN LATHE MACHINE",             MachineType = "MLA36", IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 19,  Code = "5100",          Name = "5100 TURN LATHE MACHINE",               MachineType = "MLA60", IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 20,  Code = "GT3100",        Name = "GT3100 TURN LATHE MACHINE",             MachineType = "LLA",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 21,  Code = "4100",          Name = "4100-3 TURN LATHE MACHINE",             MachineType = "MLA60", IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 22,  Code = "4100-1",        Name = "4100-1 TURN LATHE MACHINE",             MachineType = "MLA60", IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 23,  Code = "4100-2",        Name = "4100-2 TURN LATHE MACHINE",             MachineType = "MLA60", IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 24,  Code = "4100-3",        Name = "4100-3 TURN LATHE MACHINE",             MachineType = "MLA60", IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 25,  Code = "4100-4",        Name = "4100-4 TURN LATHE MACHINE",             MachineType = "MLA60", IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 26,  Code = "5100L",         Name = "5100L LONG LATHE MACHINE",              MachineType = "LLA60", IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 27,  Code = "300LT1",        Name = "300LT1 CNC Lathe Machine",              MachineType = "SLA",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 28,  Code = "300LT2",        Name = "300LT2 CNC Lathe Machine",              MachineType = "SLA",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 29,  Code = "QT-250II",      Name = "QT NEXUS 250 II LATHE MACHINE",         MachineType = "SLA",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 30,  Code = "QT-250II-M",    Name = "QT NEXUS 250 II M LATHE MACHINE",       MachineType = "SLA",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 31,  Code = "QT-350II",      Name = "QT NEXUS 350 II LATHE MACHINE",         MachineType = "SLA",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 32,  Code = "GT2100",        Name = "GT2100 LATHE MACHINE",                  MachineType = "TLA",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 33,  Code = "QTS-150-S",     Name = "QTS-150-S LATHE MACHINE",               MachineType = "TLA",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 34,  Code = "LEO1600",       Name = "LEO1600 LATHE MACHINE",                 MachineType = "TLA",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 35,  Code = "MTV-515-40N",   Name = "MTV-515/40N MILLING MACHINE",           MachineType = "MIL",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 36,  Code = "MTV-655-60N",   Name = "MTV-655/60N MILLING MACHINE",           MachineType = "MIL",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 37,  Code = "WED",           Name = "WIRECUT EDM MACHINE",                   MachineType = "WED",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 38,  Code = "WED1",          Name = "WIRECUT EDM MACHINE 1",                 MachineType = "WED",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 39,  Code = "WED2",          Name = "WIRECUT EDM MACHINE 2",                 MachineType = "WED",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 40,  Code = "EDM",           Name = "EDM MACHINE",                           MachineType = "EDM",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 41,  Code = "GDM1",          Name = "GUN DRILLING MACHINE 1",                MachineType = "GDM",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 42,  Code = "HNG1",          Name = "HONING MACHINE 1",                      MachineType = "HNG",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 43,  Code = "SAW",           Name = "BANDSAW MACHINE",                       MachineType = "SAW",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 44,  Code = "VAC1",          Name = "VACUUM TEST MACHINE",                   MachineType = "VAC",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 45,  Code = "FRT",           Name = "FORCE TESTING PROCESS",                 MachineType = "FRT",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 46,  Code = "PRT",           Name = "PRESURE TESTING PROCESS",               MachineType = "PRT",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 47,  Code = "INS",           Name = "INSPECTION",                            MachineType = "INS",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 48,  Code = "INS1",          Name = "INSPECTOR 1",                           MachineType = "INS",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 49,  Code = "QC-01",         Name = "QC COMPUTER 01",                        MachineType = "INS",   IsCnc = false, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 50,  Code = "QC-02",         Name = "QC COMPUTER 02",                        MachineType = "INS",   IsCnc = false, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 51,  Code = "QC-03",         Name = "QC COMPUTER 03",                        MachineType = "INS",   IsCnc = false, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 52,  Code = "QC-04",         Name = "QC COMPUTER 04",                        MachineType = "INS",   IsCnc = false, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 53,  Code = "QC-05",         Name = "QC COMPUTER 05",                        MachineType = "INS",   IsCnc = false, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 54,  Code = "QC-06",         Name = "QC COMPUTER 06",                        MachineType = "INS",   IsCnc = false, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 55,  Code = "QC-07",         Name = "QC COMPUTER 07",                        MachineType = "INS",   IsCnc = false, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 56,  Code = "QC-08",         Name = "QC COMPUTER 08",                        MachineType = "INS",   IsCnc = false, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 57,  Code = "FTR1",          Name = "FIXTURE MARKER 1",                      MachineType = "FTR",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 58,  Code = "FTR2",          Name = "FIXTURE MARKER 2",                      MachineType = "FTR",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 59,  Code = "IST1",          Name = "ISSUE TOOLINGS 1",                      MachineType = "IST",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 60,  Code = "IST2",          Name = "ISSUE TOOLINGS 2",                      MachineType = "IST",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 61,  Code = "IST3",          Name = "ISSUE TOOLINGS 3",                      MachineType = "IST",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 62,  Code = "IST4",          Name = "ISSUE TOOLINGS 4",                      MachineType = "IST",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 63,  Code = "ISS",           Name = "ISSUE RAW MATERIAL",                    MachineType = "ISS",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 64,  Code = "ENG",           Name = "ENGINEERING",                           MachineType = "ENG",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 65,  Code = "ENG2",          Name = "ENGINEERING 2",                         MachineType = "ENG",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 66,  Code = "ENG3",          Name = "ENGINEERING 3",                         MachineType = "ENG",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 67,  Code = "ENG4",          Name = "ENGINEERING 4",                         MachineType = "ENG",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 68,  Code = "ENG5",          Name = "ENGINEERING 5",                         MachineType = "ENG",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 69,  Code = "ENG6",          Name = "ENGINEERING 6",                         MachineType = "ENG",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 70,  Code = "ASY",           Name = "ASSEMBLY",                              MachineType = "ASY",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 71,  Code = "ASY1",          Name = "ASSEMBLY 1",                            MachineType = "ASY",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 72,  Code = "GMO",           Name = "General Machine Operation",             MachineType = "GMO",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 73,  Code = "WDP",           Name = "WELDING PROCESS",                       MachineType = "WDP",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 74,  Code = "LAP",           Name = "LAPPING PROCESS",                       MachineType = "LAP",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 75,  Code = "GRP",           Name = "GRINDING PROCESS",                      MachineType = "GRP",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 76,  Code = "PNG",           Name = "PEENING PROCESS",                       MachineType = "PNG",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 77,  Code = "SRP",           Name = "STRESS RELIEF PROCESS",                 MachineType = "SRP",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 78,  Code = "STP",           Name = "STAMPING PROCESS",                      MachineType = "STP",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 79,  Code = "HTP",           Name = "HAND TAPPING",                          MachineType = "HTP",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 80,  Code = "HMP",           Name = "HAND MADE PROCESS",                     MachineType = "HMP",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 81,  Code = "IDH",           Name = "INDUCTION HARDENING PROCESS",           MachineType = "IDH",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 82,  Code = "HTR",           Name = "HEAT TREAT OUTSIDE PROCESS",            MachineType = "HTR",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 83,  Code = "MLK",           Name = "MOLYKOTE COATING",                      MachineType = "MLK",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 84,  Code = "QPQ",           Name = "QPQ OUTSIDE PROCESS",                   MachineType = "QPQ",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 85,  Code = "XYL",           Name = "XYLAN COATING",                         MachineType = "XYL",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 86,  Code = "FLUO",          Name = "FLUOROPOLYMER",                         MachineType = "FLUO",  IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 87,  Code = "DMC",           Name = "DRY MOLY COATING",                      MachineType = "DMC",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 88,  Code = "CGP",           Name = "COATING PROCESS",                       MachineType = "CGP",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 89,  Code = "COP",           Name = "COPPER PLATING",                        MachineType = "COP",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 90,  Code = "CPO",           Name = "COATING PROCESS OUTSIDE",               MachineType = "CPO",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 91,  Code = "NCO",           Name = "NICKEL COATING OUTSOURCE",              MachineType = "NCO",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 92,  Code = "PPG",           Name = "PHOSPHATING",                           MachineType = "PPG",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 93,  Code = "PPG1",          Name = "PHOSPHATE 1",                           MachineType = "PPG",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 94,  Code = "PPG2",          Name = "PHOSPHATE 2",                           MachineType = "PPG",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 95,  Code = "PTH",           Name = "PREMIUM THREAD OUTSIDE PROCESS",        MachineType = "PTH",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 96,  Code = "NDE",           Name = "NDE OUTSIDE PROCESS",                   MachineType = "NDE",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 97,  Code = "UTO",           Name = "UT TEST OUTSIDE PROCESS",               MachineType = "UTO",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 98,  Code = "PMP",           Name = "PROTECTOR MAKING PROCESS",              MachineType = "PMP",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 99,  Code = "POP",           Name = "POLISHING OUTSIDE PROCESS",             MachineType = "POP",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 100, Code = "BDG",           Name = "RUBBER BONDING OUTSIDE PROCESS",        MachineType = "BDG",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 101, Code = "WIT",           Name = "WITNESS",                               MachineType = "WIT",   IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 102, Code = "Vturn40-220-1", Name = "Victor Turning Machine 40-220-1",       MachineType = "VML2",  IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 103, Code = "Vturn40-220-2", Name = "Victor Turning Machine 40-220-2",       MachineType = "VML2",  IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 104, Code = "Vturn40-220-3", Name = "Victor Turning Machine 40-220-3",       MachineType = "VML2",  IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 105, Code = "Vturn40-220-4", Name = "Victor Turning Machine 40-220-4",       MachineType = "VML2",  IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 106, Code = "Vturn40-325-1", Name = "Victor Turning Machine 40-325-1",       MachineType = "VML2",  IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 107, Code = "Vturn40-325-2", Name = "Victor Turning Machine 40-325-2",       MachineType = "VML2",  IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 108, Code = "Vturn45-125-1", Name = "Victor Turning Machine 45-125-1",       MachineType = "VMM2",  IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 109, Code = "Vturn45-125-2", Name = "Victor Turning Machine 45-125-2",       MachineType = "VMM2",  IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 110, Code = "Vturn45-125-3", Name = "Victor Turning Machine 45-125-3",       MachineType = "VMM2",  IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 111, Code = "Vturn45-125-4", Name = "Victor Turning Machine 45-125-4",       MachineType = "VMM2",  IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 112, Code = "Vturn45-125-5", Name = "Victor Turning Machine 45-125-5",       MachineType = "VMM2",  IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 113, Code = "Vturn45-125-6", Name = "Victor Turning Machine 45-125-6",       MachineType = "VMM2",  IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 114, Code = "Vturn45-125-7", Name = "Victor Turning Machine 45-125-7",       MachineType = "VMM2",  IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 115, Code = "Vturn45-125-8", Name = "Victor Turning Machine 45-125-8",       MachineType = "VMM2",  IsCnc = true,  CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 116, Code = "190901",        Name = "KHIEM TRAN VAN",                        MachineType = "INS",   IsCnc = false, IsActive = false, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 117, Code = "190904",        Name = "TAM NGO THI THU",                       MachineType = "INS",   IsCnc = false, IsActive = false, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 118, Code = "R&D-TEST",      Name = "R&D TEST MACHINE",                      MachineType = "LLA",   IsCnc = false, IsActive = false, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 119, Code = "TEST",          Name = "TESTR&D",                               MachineType = "5MI",   IsCnc = true,  IsActive = false, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Machine { Id = 120, Code = "TESTR&DD",      Name = "TESTR&DD",                              MachineType = "5MI",   IsCnc = false, IsActive = false, CreatedAt = seedDate, UpdatedAt = seedDate }
        );
    }
}
