using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

public class PartOpConfiguration : IEntityTypeConfiguration<PartOp>
{
    public void Configure(EntityTypeBuilder<PartOp> builder)
    {
        builder.Property(o => o.OpNumber).HasMaxLength(10).IsRequired();
        builder.Property(o => o.Description).HasColumnType("text");
        builder.Property(o => o.Note).HasColumnType("text");
        builder.Property(o => o.SetupTime).HasPrecision(8, 2);
        builder.Property(o => o.ProdTime).HasPrecision(8, 2);
        builder.Property(o => o.OpNumberSort).HasPrecision(8, 2);

        builder.HasOne(o => o.Part).WithMany(p => p.PartOps)
            .HasForeignKey(o => o.PartId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.OpType).WithMany(t => t.PartOps)
            .HasForeignKey(o => o.OpTypeId).OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Job).WithMany(j => j.PartOps)
            .HasForeignKey(o => o.JobId).OnDelete(DeleteBehavior.Cascade);
    }
}
