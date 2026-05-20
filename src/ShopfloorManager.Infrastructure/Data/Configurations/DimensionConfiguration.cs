using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

public class DimensionConfiguration : IEntityTypeConfiguration<Dimension>
{
    public void Configure(EntityTypeBuilder<Dimension> builder)
    {
        builder.Property(d => d.Id).UseIdentityByDefaultColumn();
        builder.Property(d => d.Code).HasMaxLength(20).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(200);
        builder.Property(d => d.Unit).HasMaxLength(20).HasDefaultValue("mm");
        builder.Property(d => d.Nominal).HasPrecision(14, 4);
        builder.Property(d => d.UpperTol).HasPrecision(14, 4);
        builder.Property(d => d.LowerTol).HasPrecision(14, 4);

        // Computed props are ignored by EF Core
        builder.Ignore(d => d.UpperLimit);
        builder.Ignore(d => d.LowerLimit);

        builder.HasOne(d => d.PartOp).WithMany()
            .HasForeignKey(d => d.PartOpId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.PartOpId, d.Code }).IsUnique();
    }
}
