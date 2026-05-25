using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

public class ProductionSessionConfiguration : IEntityTypeConfiguration<ProductionSession>
{
    public void Configure(EntityTypeBuilder<ProductionSession> builder)
    {
        builder.Property(s => s.MachineCode).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Status).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Note).HasMaxLength(500);

        builder.HasIndex(s => s.ProductId);
        builder.HasIndex(s => s.PartOpId);
        builder.HasIndex(s => s.MachineCode);
        builder.HasIndex(s => new { s.MachineCode, s.Status });

        builder.HasOne(s => s.Product).WithMany()
            .HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.PartOp).WithMany()
            .HasForeignKey(s => s.PartOpId).OnDelete(DeleteBehavior.Restrict);

        // Map the explicit int FK props to their navigation properties
        builder.HasOne(s => s.ClaimedByUser).WithMany()
            .HasForeignKey(s => s.ClaimedBy).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CancelledByUser).WithMany()
            .HasForeignKey(s => s.CancelledBy).OnDelete(DeleteBehavior.SetNull);
    }
}
