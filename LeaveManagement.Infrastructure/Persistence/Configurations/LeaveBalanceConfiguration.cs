using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

public sealed class LeaveBalanceConfiguration
    : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ToTable("LeaveBalances");

        builder.HasKey(b => b.BalanceId);

        builder.Property(b => b.Year)
            .IsRequired();

        builder.Property(b => b.AvailableDays)
            .HasPrecision(6, 2)
            .IsRequired();

        builder.Property(b => b.PendingDays)
            .HasPrecision(6, 2)
            .IsRequired();

        builder.HasIndex(b => new
        {
            b.EmployeeId,
            b.LeaveTypeId,
            b.Year
        }).IsUnique();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(b => b.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LeaveType>()
            .WithMany()
            .HasForeignKey(b => b.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}