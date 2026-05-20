using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

public class RoleMenuConfiguration : IEntityTypeConfiguration<RoleMenu>
{
    public void Configure(EntityTypeBuilder<RoleMenu> builder)
    {
        builder.HasIndex(rm => new { rm.RoleId, rm.MenuId }).IsUnique();

        builder.HasOne(rm => rm.Role).WithMany(r => r.RoleMenus)
            .HasForeignKey(rm => rm.RoleId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rm => rm.Menu).WithMany(m => m.RoleMenus)
            .HasForeignKey(rm => rm.MenuId).OnDelete(DeleteBehavior.Cascade);
    }
}
