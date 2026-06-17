using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the FiscalCalendar entity.
/// </summary>
internal sealed class FiscalCalendarConfiguration : IEntityTypeConfiguration<FiscalCalendar>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FiscalCalendar> builder)
    {
        builder.ToTable("FiscalCalendars", "company");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.FiscalYear).IsRequired();

        builder.Property(x => x.StartDate).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.EndDate).HasColumnType("datetime2").IsRequired();

        builder.Property(x => x.IsClosed)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(x => x.CalendarType)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.GeneratedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.GeneratedUtc).HasColumnType("datetime2").IsRequired();

        builder.Property(x => x.IsLocked)
               .IsRequired()
               .HasDefaultValue(false);

        // One-to-many relationship with FiscalPeriod
        builder.HasMany(x => x.Periods)
               .WithOne()
               .HasForeignKey(x => x.FiscalCalendarId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureSoftDeleteAndAudit();
    }
}
