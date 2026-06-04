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
        builder.Property(m => m.SerialNumber).HasMaxLength(100);

        // MachineGroup: logical relationship via MachineType=GroupCode string match
        // No hard FK — mirrors legacy system (string code matching, not FK constraint)
        builder.Ignore(m => m.MachineGroup);
    }
}
