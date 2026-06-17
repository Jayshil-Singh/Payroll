using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the PayrollFrequencyDefinition entity.
/// </summary>
internal sealed class PayrollFrequencyDefinitionConfiguration : IEntityTypeConfiguration<PayrollFrequencyDefinition>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PayrollFrequencyDefinition> builder)
    {
        builder.ToTable("PayrollFrequencyDefinitions", "company");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();

        builder.Property(x => x.FrequencyName)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.FrequencyType)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.FrequencyCode)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.PayDay)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.PeriodsPerYear).IsRequired();

        builder.Property(x => x.Description)
               .HasColumnType("nvarchar(500)")
               .IsRequired();

        builder.Property(x => x.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        // One-to-many relationship with PayPeriodSchedule
        builder.HasMany(x => x.Schedules)
               .WithOne()
               .HasForeignKey(x => x.PayrollFrequencyDefinitionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureSoftDeleteAndAudit();
    }
}
