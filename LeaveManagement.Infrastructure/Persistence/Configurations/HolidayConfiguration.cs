using LeaveManagement.Domain.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

public sealed class HolidayConfiguration
    : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays");

        builder.HasKey(h => h.HolidayId);

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(h => h.HolidayDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(h => h.Year)
            .IsRequired();

        builder.HasIndex(h => h.HolidayDate)
            .IsUnique();
    }
}