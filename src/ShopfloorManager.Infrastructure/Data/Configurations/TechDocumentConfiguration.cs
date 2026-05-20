using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

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

        builder.HasOne(t => t.Job).WithMany()
            .HasForeignKey(t => t.JobId).OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Part).WithMany()
            .HasForeignKey(t => t.PartId).OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.PartOp).WithMany()
            .HasForeignKey(t => t.PartOpId).OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Inspector).WithMany()
            .HasForeignKey(t => t.InspectorId).OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Creator).WithMany()
            .HasForeignKey(t => t.CreatedBy).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(t => t.DeletedAt == null);

        builder.HasIndex(t => t.JobId).HasFilter("deleted_at IS NULL");
        builder.HasIndex(t => t.PartOpId).HasFilter("deleted_at IS NULL");
    }
}
