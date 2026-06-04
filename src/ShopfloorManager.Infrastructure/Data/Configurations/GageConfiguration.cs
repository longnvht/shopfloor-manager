using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

public class GageTypeConfiguration : IEntityTypeConfiguration<GageType>
{
    public void Configure(EntityTypeBuilder<GageType> b)
    {
        b.HasIndex(g => g.Code).IsUnique();
        b.Property(g => g.Code).HasMaxLength(30).IsRequired();
        b.Property(g => g.Name).HasMaxLength(150).IsRequired();

        b.HasOne(g => g.Category).WithMany()
            .HasForeignKey(g => g.CategoryId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(g => g.DefaultProcedure).WithMany()
            .HasForeignKey(g => g.DefaultProcedureId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class GageLocationConfiguration : IEntityTypeConfiguration<GageLocation>
{
    public void Configure(EntityTypeBuilder<GageLocation> b)
    {
        b.HasIndex(l => l.Code).IsUnique();
        b.Property(l => l.Code).HasMaxLength(50).IsRequired();
        b.Property(l => l.Description).HasMaxLength(200).IsRequired();
    }
}

public class GageSlotConfiguration : IEntityTypeConfiguration<GageSlot>
{
    public void Configure(EntityTypeBuilder<GageSlot> b)
    {
        b.Property(s => s.Code).HasMaxLength(50).IsRequired();
        b.Property(s => s.Description).HasMaxLength(100);

        b.HasOne(s => s.Location).WithMany(l => l.Slots)
            .HasForeignKey(s => s.LocationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class GageConfiguration : IEntityTypeConfiguration<Gage>
{
    public void Configure(EntityTypeBuilder<Gage> b)
    {
        b.HasIndex(g => g.GageNo).IsUnique();
        b.HasQueryFilter(g => !g.IsDeleted);

        b.Property(g => g.GageNo).HasMaxLength(30).IsRequired();
        b.Property(g => g.SerialNo).HasMaxLength(50);
        b.Property(g => g.Description).HasMaxLength(150).IsRequired();
        b.Property(g => g.MeasuringRange).HasMaxLength(100);
        b.Property(g => g.Accuracy).HasMaxLength(50);
        b.Property(g => g.Unit).HasMaxLength(20).HasDefaultValue("mm");
        b.Property(g => g.Manufacturer).HasMaxLength(100);
        b.Property(g => g.StatusCode).HasMaxLength(10).HasDefaultValue("VALID");
        b.Property(g => g.Note).HasMaxLength(500);

        // Ignore computed properties
        b.Ignore(g => g.IsValid);
        b.Ignore(g => g.DueDate);
        b.Ignore(g => g.DaysRemaining);
        b.Ignore(g => g.IsDeleted);

        b.HasOne(g => g.GageType).WithMany(t => t.Gages)
            .HasForeignKey(g => g.GageTypeId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(g => g.DefaultLocation).WithMany()
            .HasForeignKey(g => g.DefaultLocationId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(g => g.DefaultSlot).WithMany()
            .HasForeignKey(g => g.DefaultSlotId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(g => g.CurrentLocation).WithMany()
            .HasForeignKey(g => g.CurrentLocationId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(g => g.CurrentSlot).WithMany()
            .HasForeignKey(g => g.CurrentSlotId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(g => g.Vendor).WithMany()
            .HasForeignKey(g => g.VendorId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class BorrowTransactionConfiguration : IEntityTypeConfiguration<BorrowTransaction>
{
    public void Configure(EntityTypeBuilder<BorrowTransaction> b)
    {
        b.Property(t => t.Note).HasMaxLength(500);

        b.HasOne(t => t.Gage).WithMany(g => g.BorrowTransactions)
            .HasForeignKey(t => t.GageId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.Borrower).WithMany()
            .HasForeignKey(t => t.BorrowerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.Manager).WithMany()
            .HasForeignKey(t => t.ManagerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.FromLocation).WithMany()
            .HasForeignKey(t => t.FromLocationId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(t => t.UseLocation).WithMany()
            .HasForeignKey(t => t.UseLocationId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class CalibVendorConfiguration : IEntityTypeConfiguration<CalibVendor>
{
    public void Configure(EntityTypeBuilder<CalibVendor> b)
    {
        b.Property(v => v.Name).HasMaxLength(150).IsRequired();
        b.Property(v => v.Contact).HasMaxLength(100);
        b.Property(v => v.Address).HasMaxLength(300);
        b.Property(v => v.Phone).HasMaxLength(50);
        b.Property(v => v.Email).HasMaxLength(100);
    }
}

public class CalibProcedureConfiguration : IEntityTypeConfiguration<CalibProcedure>
{
    public void Configure(EntityTypeBuilder<CalibProcedure> b)
    {
        b.Property(p => p.Name).HasMaxLength(200).IsRequired();
        b.Property(p => p.Revision).HasMaxLength(20);
        b.Property(p => p.DocLink).HasMaxLength(500);
    }
}

public class CalibRequestConfiguration : IEntityTypeConfiguration<CalibRequest>
{
    public void Configure(EntityTypeBuilder<CalibRequest> b)
    {
        b.HasOne(r => r.Gage).WithMany(g => g.CalibRequests)
            .HasForeignKey(r => r.GageId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Vendor).WithMany(v => v.CalibRequests)
            .HasForeignKey(r => r.VendorId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(r => r.Creator).WithMany()
            .HasForeignKey(r => r.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CalibRecordConfiguration : IEntityTypeConfiguration<CalibRecord>
{
    public void Configure(EntityTypeBuilder<CalibRecord> b)
    {
        b.Property(r => r.CalibratedBy).HasMaxLength(100);
        b.Property(r => r.AsFoundConditions).HasMaxLength(100);
        b.Property(r => r.AdjustmentMade).HasPrecision(8, 4);
        b.Property(r => r.Temperature).HasPrecision(6, 2);
        b.Property(r => r.Humidity).HasPrecision(6, 2);
        b.Property(r => r.StoragePath).HasMaxLength(500);

        b.HasOne(r => r.CalibRequest).WithOne(q => q.Record)
            .HasForeignKey<CalibRecord>(r => r.CalibRequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Procedure).WithMany(p => p.CalibRecords)
            .HasForeignKey(r => r.ProcedureId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(r => r.Creator).WithMany()
            .HasForeignKey(r => r.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
