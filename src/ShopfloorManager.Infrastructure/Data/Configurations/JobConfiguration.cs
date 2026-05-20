using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.HasIndex(j => j.JobNumber).IsUnique();
        builder.Property(j => j.JobNumber).HasMaxLength(20).IsRequired();

        builder.HasOne(j => j.Part).WithMany(p => p.Jobs)
            .HasForeignKey(j => j.PartId).OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);  // allow EF to handle soft-deleted Parts gracefully

        builder.HasOne(j => j.PoLine).WithMany(po => po.Jobs)
            .HasForeignKey(j => j.PoLineId).OnDelete(DeleteBehavior.SetNull);
    }
}
