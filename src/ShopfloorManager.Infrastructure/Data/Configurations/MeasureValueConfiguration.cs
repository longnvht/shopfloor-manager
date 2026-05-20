using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

public class MeasureValueConfiguration : IEntityTypeConfiguration<MeasureValue>
{
    public void Configure(EntityTypeBuilder<MeasureValue> builder)
    {
        builder.Property(m => m.Id).UseIdentityByDefaultColumn();
        builder.Property(m => m.Value).HasPrecision(14, 4);
        builder.Property(m => m.Note).HasMaxLength(500);

        builder.HasOne(m => m.Dimension).WithMany(d => d.MeasureValues)
            .HasForeignKey(m => m.DimensionId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Product).WithMany()
            .HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Inspector).WithMany()
            .HasForeignKey(m => m.MeasuredBy).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => new { m.DimensionId, m.ProductId }).IsUnique();
    }
}
