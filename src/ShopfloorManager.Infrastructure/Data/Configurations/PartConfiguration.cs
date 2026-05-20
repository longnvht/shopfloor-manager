using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

public class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.HasIndex(p => p.PartNumber).IsUnique();
        builder.Property(p => p.PartNumber).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(300).IsRequired();
    }
}

public class PartRevConfiguration : IEntityTypeConfiguration<PartRev>
{
    public void Configure(EntityTypeBuilder<PartRev> builder)
    {
        builder.HasIndex(r => new { r.PartId, r.RevCode }).IsUnique();
        builder.Property(r => r.RevCode).HasMaxLength(10).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);

        builder.HasOne(r => r.Part).WithMany(p => p.Revisions)
            .HasForeignKey(r => r.PartId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoutingConfiguration : IEntityTypeConfiguration<Routing>
{
    public void Configure(EntityTypeBuilder<Routing> builder)
    {
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);

        builder.HasOne(r => r.PartRev).WithMany(pr => pr.Routings)
            .HasForeignKey(r => r.PartRevId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoutingRevConfiguration : IEntityTypeConfiguration<RoutingRev>
{
    public void Configure(EntityTypeBuilder<RoutingRev> builder)
    {
        builder.HasIndex(r => new { r.RoutingId, r.RevCode }).IsUnique();
        builder.Property(r => r.RevCode).HasMaxLength(10).IsRequired();
        builder.Property(r => r.ChangeNote).HasMaxLength(500);

        builder.HasOne(r => r.Routing).WithMany(rt => rt.Revisions)
            .HasForeignKey(r => r.RoutingId).OnDelete(DeleteBehavior.Restrict);
    }
}
