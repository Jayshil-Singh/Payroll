using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the CompanySetupState entity.
/// </summary>
internal sealed class CompanySetupStateConfiguration : IEntityTypeConfiguration<CompanySetupState>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CompanySetupState> builder)
    {
        builder.ToTable("CompanySetupStates", "company");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();
        builder.HasIndex(x => x.CompanyId).IsUnique(); // Ensure only one setup state per company

        builder.Property(x => x.CurrentStep)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.IsCompleted)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(x => x.WizardVersion)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasDefaultValue("1.0.0");

        // Relationship
        builder.HasMany(x => x.Tasks)
               .WithOne()
               .HasForeignKey(x => x.CompanySetupStateId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureSoftDeleteAndAudit();
    }
}
