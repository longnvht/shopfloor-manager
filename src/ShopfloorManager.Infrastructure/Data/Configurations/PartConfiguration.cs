using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

public class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.HasIndex(p => new { p.PartNumber, p.Revision }).IsUnique();
        builder.Property(p => p.PartNumber).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Revision).HasMaxLength(10);
        builder.Property(p => p.RoutingRevision).HasMaxLength(100);

        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
