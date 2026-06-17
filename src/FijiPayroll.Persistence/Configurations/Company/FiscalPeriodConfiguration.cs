using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the FiscalPeriod entity.
/// </summary>
internal sealed class FiscalPeriodConfiguration : IEntityTypeConfiguration<FiscalPeriod>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FiscalPeriod> builder)
    {
        builder.ToTable("FiscalPeriods", "company");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.FiscalCalendarId).IsRequired();
        builder.Property(x => x.PeriodNumber).IsRequired();

        builder.Property(x => x.PeriodName)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.StartDate).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.EndDate).HasColumnType("datetime2").IsRequired();

        builder.Property(x => x.IsClosed)
               .IsRequired()
               .HasDefaultValue(false);

        builder.ConfigureSoftDeleteAndAudit();
    }
}
