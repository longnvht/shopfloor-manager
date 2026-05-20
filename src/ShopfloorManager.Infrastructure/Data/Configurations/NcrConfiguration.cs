using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

public class NcrConfiguration : IEntityTypeConfiguration<Ncr>
{
    public void Configure(EntityTypeBuilder<Ncr> builder)
    {
        builder.Property(n => n.Id).UseIdentityByDefaultColumn();
        builder.Property(n => n.NcrNumber).HasMaxLength(20).IsRequired();
        builder.Property(n => n.Description).HasColumnType("text");
        builder.HasIndex(n => n.NcrNumber).IsUnique();

        builder.HasOne(n => n.Job).WithMany()
            .HasForeignKey(n => n.JobId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Product).WithMany()
            .HasForeignKey(n => n.ProductId).OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.PartOp).WithMany()
            .HasForeignKey(n => n.PartOpId).OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.Raiser).WithMany()
            .HasForeignKey(n => n.RaisedBy).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Closer).WithMany()
            .HasForeignKey(n => n.ClosedBy).OnDelete(DeleteBehavior.SetNull);
    }
}

public class NcrLogConfiguration : IEntityTypeConfiguration<NcrLog>
{
    public void Configure(EntityTypeBuilder<NcrLog> builder)
    {
        builder.Property(l => l.Note).HasMaxLength(1000);

        builder.HasOne(l => l.Ncr).WithMany(n => n.Logs)
            .HasForeignKey(l => l.NcrId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Actor).WithMany()
            .HasForeignKey(l => l.ActionBy).OnDelete(DeleteBehavior.Restrict);
    }
}
