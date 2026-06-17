using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the PayPeriodSchedule entity.
/// </summary>
internal sealed class PayPeriodScheduleConfiguration : IEntityTypeConfiguration<PayPeriodSchedule>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PayPeriodSchedule> builder)
    {
        builder.ToTable("PayPeriodSchedules", "company");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.PayrollFrequencyDefinitionId).IsRequired();
        builder.Property(x => x.PeriodNumber).IsRequired();

        builder.Property(x => x.StartDate).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.EndDate).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.CutoffDate).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.PaymentDate).HasColumnType("datetime2").IsRequired();

        builder.Property(x => x.IsProcessed)
               .IsRequired()
               .HasDefaultValue(false);

        builder.ConfigureSoftDeleteAndAudit();
    }
}
