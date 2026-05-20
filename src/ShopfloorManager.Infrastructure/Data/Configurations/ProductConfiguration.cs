using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasIndex(p => new { p.SerialNumber, p.JobId }).IsUnique();
        builder.Property(p => p.SerialNumber).HasMaxLength(10).IsRequired();

        builder.HasOne(p => p.Job).WithMany(j => j.Products)
            .HasForeignKey(p => p.JobId).OnDelete(DeleteBehavior.Cascade);
    }
}
