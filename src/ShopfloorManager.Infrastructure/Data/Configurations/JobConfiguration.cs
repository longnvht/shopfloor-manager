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

        builder.HasOne(j => j.PartRev).WithMany()
            .HasForeignKey(j => j.PartRevId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.RoutingRev).WithMany()
            .HasForeignKey(j => j.RoutingRevId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.PoLine).WithMany(p => p.Jobs)
            .HasForeignKey(j => j.PoLineId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasIndex(p => new { p.JobId, p.SerialNumber }).IsUnique();
        builder.Property(p => p.SerialNumber).HasMaxLength(10).IsRequired();

        builder.HasOne(p => p.Job).WithMany(j => j.Products)
            .HasForeignKey(p => p.JobId).OnDelete(DeleteBehavior.Cascade);
    }
}
