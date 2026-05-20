using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(u => u.UserLogin).IsUnique();
        builder.Property(u => u.UserLogin).HasMaxLength(50).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Sex).HasMaxLength(10);
        builder.Property(u => u.Email).HasMaxLength(100);
        builder.Property(u => u.ResetCode).HasMaxLength(10);

        builder.HasOne(u => u.UserType).WithMany(t => t.Users)
            .HasForeignKey(u => u.UserTypeId).OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.Position).WithMany(p => p.Users)
            .HasForeignKey(u => u.PositionId).OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.WorkStatus).WithMany(w => w.Users)
            .HasForeignKey(u => u.WorkStatusId).OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.Role).WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.SetNull);

        // MesRoleId: FK to mes_role_menus (Phase 4) — no navigation property yet
        builder.Property(u => u.MesRoleId).HasColumnName("mes_role_id");
    }
}
