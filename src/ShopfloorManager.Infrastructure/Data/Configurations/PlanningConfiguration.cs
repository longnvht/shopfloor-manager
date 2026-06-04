using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Infrastructure.Data.Configurations;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> b)
    {
        b.Property(s => s.Name).HasMaxLength(50).IsRequired();
    }
}

public class BreakTimeConfiguration : IEntityTypeConfiguration<BreakTime>
{
    public void Configure(EntityTypeBuilder<BreakTime> b)
    {
        b.Property(t => t.Label).HasMaxLength(50);
    }
}

public class PlanningItemConfiguration : IEntityTypeConfiguration<PlanningItem>
{
    public void Configure(EntityTypeBuilder<PlanningItem> b)
    {
        b.Property(p => p.Note).HasMaxLength(500);
        b.HasIndex(p => new { p.MachineId, p.StartTime });

        b.HasOne(p => p.Job).WithMany()
            .HasForeignKey(p => p.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.PartOp).WithMany()
            .HasForeignKey(p => p.PartOpId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Machine).WithMany()
            .HasForeignKey(p => p.MachineId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Operator).WithMany()
            .HasForeignKey(p => p.OperatorId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(p => p.Shift).WithMany(s => s.PlanningItems)
            .HasForeignKey(p => p.ShiftId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(p => p.Creator).WithMany()
            .HasForeignKey(p => p.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ShiftAssignmentConfiguration : IEntityTypeConfiguration<ShiftAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftAssignment> b)
    {
        b.HasIndex(a => new { a.MachineId, a.AssignedDate });

        b.HasOne(a => a.User).WithMany()
            .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.Machine).WithMany()
            .HasForeignKey(a => a.MachineId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.Shift).WithMany(s => s.ShiftAssignments)
            .HasForeignKey(a => a.ShiftId).OnDelete(DeleteBehavior.Restrict);
    }
}
