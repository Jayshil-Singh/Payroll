using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the CompanySetupTask entity.
/// </summary>
internal sealed class CompanySetupTaskConfiguration : IEntityTypeConfiguration<CompanySetupTask>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CompanySetupTask> builder)
    {
        builder.ToTable("CompanySetupTasks", "company");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.CompanySetupStateId).IsRequired();

        builder.Property(x => x.Step)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.Completed)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(x => x.CompletedUtc)
               .HasColumnType("datetime2")
               .IsRequired(false);

        builder.Property(x => x.CompletedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.Version)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasDefaultValue("1.0.0");

        builder.ConfigureSoftDeleteAndAudit();
    }
}
