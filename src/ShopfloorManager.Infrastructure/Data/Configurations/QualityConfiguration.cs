using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

public class DimensionConfiguration : IEntityTypeConfiguration<Dimension>
{
    public void Configure(EntityTypeBuilder<Dimension> builder)
    {
        builder.Property(d => d.Id).UseIdentityByDefaultColumn();
        builder.HasIndex(d => new { d.PartOpId, d.BalloonNumber }).IsUnique();
        builder.Property(d => d.BalloonNumber).HasMaxLength(20).IsRequired();
        builder.Property(d => d.Code).HasMaxLength(20);
        builder.Property(d => d.Description).HasMaxLength(200);
        builder.Property(d => d.Unit).HasMaxLength(20).HasDefaultValue("mm");
        builder.Property(d => d.Nominal).HasPrecision(14, 4);
        builder.Property(d => d.UpperTol).HasPrecision(14, 4);
        builder.Property(d => d.LowerTol).HasPrecision(14, 4);
        builder.Ignore(d => d.UpperLimit);
        builder.Ignore(d => d.LowerLimit);

        builder.HasOne(d => d.PartOp).WithMany(o => o.Dimensions)
            .HasForeignKey(d => d.PartOpId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class MeasureValueConfiguration : IEntityTypeConfiguration<MeasureValue>
{
    public void Configure(EntityTypeBuilder<MeasureValue> builder)
    {
        builder.Property(m => m.Id).UseIdentityByDefaultColumn();
        builder.HasIndex(m => new { m.DimensionId, m.ProductId }).IsUnique();
        builder.Property(m => m.Value).HasPrecision(14, 4);
        builder.Property(m => m.Note).HasMaxLength(500);

        builder.HasOne(m => m.Dimension).WithMany(d => d.MeasureValues)
            .HasForeignKey(m => m.DimensionId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Product).WithMany(p => p.MeasureValues)
            .HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.PartOp).WithMany()
            .HasForeignKey(m => m.PartOpId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Inspector).WithMany()
            .HasForeignKey(m => m.MeasuredBy).OnDelete(DeleteBehavior.SetNull);
    }
}

public class NcrConfiguration : IEntityTypeConfiguration<Ncr>
{
    public void Configure(EntityTypeBuilder<Ncr> builder)
    {
        builder.Property(n => n.Id).UseIdentityByDefaultColumn();
        builder.HasIndex(n => n.NcrNumber).IsUnique();
        builder.Property(n => n.NcrNumber).HasMaxLength(20).IsRequired();
        builder.Property(n => n.Description).HasColumnType("text");

        builder.HasOne(n => n.Job).WithMany()
            .HasForeignKey(n => n.JobId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(n => n.Product).WithMany()
            .HasForeignKey(n => n.ProductId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(n => n.PartOp).WithMany()
            .HasForeignKey(n => n.PartOpId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(n => n.Raiser).WithMany()
            .HasForeignKey(n => n.RaisedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(n => n.Closer).WithMany()
            .HasForeignKey(n => n.ClosedBy).OnDelete(DeleteBehavior.SetNull);
    }
}

public class NcrLogConfiguration : IEntityTypeConfiguration<NcrLog>
{
    public void Configure(EntityTypeBuilder<NcrLog> builder)
    {
        builder.Property(l => l.Note).HasMaxLength(1000);

        builder.HasOne(l => l.Ncr).WithMany(n => n.Logs)
            .HasForeignKey(l => l.NcrId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(l => l.Actor).WithMany()
            .HasForeignKey(l => l.ActionBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TechDocumentConfiguration : IEntityTypeConfiguration<TechDocument>
{
    public void Configure(EntityTypeBuilder<TechDocument> builder)
    {
        builder.Property(t => t.Id).UseIdentityByDefaultColumn();
        builder.Property(t => t.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.Revision).HasMaxLength(50);
        builder.Property(t => t.Code).HasMaxLength(100);
        builder.Property(t => t.Segment).HasMaxLength(100);
        builder.Property(t => t.InspectNote).HasMaxLength(500);

        builder.HasOne(t => t.FileType).WithMany(f => f.TechDocuments)
            .HasForeignKey(t => t.FileTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.PartOp).WithMany(o => o.TechDocuments)
            .HasForeignKey(t => t.PartOpId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Job).WithMany()
            .HasForeignKey(t => t.JobId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(t => t.Inspector).WithMany()
            .HasForeignKey(t => t.InspectorId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(t => t.Creator).WithMany()
            .HasForeignKey(t => t.CreatedBy).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(t => t.DeletedAt == null);
    }
}
