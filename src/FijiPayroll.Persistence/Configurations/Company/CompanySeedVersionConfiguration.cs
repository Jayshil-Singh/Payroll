using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the CompanySeedVersion entity.
/// </summary>
internal sealed class CompanySeedVersionConfiguration : IEntityTypeConfiguration<CompanySeedVersion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CompanySeedVersion> builder)
    {
        builder.ToTable("CompanySeedVersions", "company");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();

        builder.Property(x => x.SeedVersion)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.AppliedUtc)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.Description)
               .HasColumnType("nvarchar(500)")
               .IsRequired();

        builder.Property(x => x.SeedCategory)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasConversion<string>();

        builder.ConfigureSoftDeleteAndAudit();
    }
}
