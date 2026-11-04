using LeaveManagement.Domain.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

public sealed class LeaveTypeConfiguration
    : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("LeaveTypes");

        builder.HasKey(lt => lt.LeaveTypeId);

        builder.Property(lt => lt.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(lt => lt.Name)
            .IsUnique();

        builder.Property(lt => lt.AnnualAllowance)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(lt => lt.IsActive)
            .IsRequired();

        builder.Property(lt => lt.RequiresManagerApproval)
            .IsRequired();
    }
}