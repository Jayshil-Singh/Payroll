using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the FnpfConfiguration entity.
/// </summary>
internal sealed class FnpfConfigurationConfiguration : IEntityTypeConfiguration<FnpfConfiguration>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FnpfConfiguration> builder)
    {
        builder.ToTable("FnpfConfigurations", "company");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();

        builder.Property(x => x.EmployerRate)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.EmployeeRate)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.EffectiveDate)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.ConfigureSoftDeleteAndAudit();
    }
}
