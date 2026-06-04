using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

public class MachineConfiguration : IEntityTypeConfiguration<Machine>
{
    public void Configure(EntityTypeBuilder<Machine> builder)
    {
        builder.HasIndex(m => m.Code).IsUnique();
        builder.Property(m => m.Code).HasMaxLength(50).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(150);
        builder.Property(m => m.MachineType).HasMaxLength(50);
    }
}
