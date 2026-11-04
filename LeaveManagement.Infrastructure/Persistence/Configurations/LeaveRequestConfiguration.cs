using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

public sealed class LeaveRequestConfiguration
    : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");

        builder.HasKey(r => r.LeaveRequestId);

        builder.Property(r => r.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(r => r.EndDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(r => r.TotalDays)
            .HasPrecision(6, 2)
            .IsRequired();

        builder.Property(r => r.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.Status)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(r => r.DecisionComment)
            .HasMaxLength(500);

        builder.Property(r => r.CreatedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(r => r.DecidedAtUtc)
            .HasColumnType("datetime2");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LeaveType>()
            .WithMany()
            .HasForeignKey(r => r.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(r => r.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new
        {
            r.EmployeeId,
            r.Status
        });

        builder.HasIndex(r => new
        {
            r.StartDate,
            r.EndDate
        });
    }
}