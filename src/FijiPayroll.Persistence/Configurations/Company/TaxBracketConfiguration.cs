using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core mapping configuration for the TaxBracket configuration ruleset entity.
/// </summary>
internal sealed class TaxBracketConfiguration : IEntityTypeConfiguration<TaxBracket>
{
    public void Configure(EntityTypeBuilder<TaxBracket> builder)
    {
        builder.ToTable("TaxBrackets", "company");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Concurrency token
        builder.Property(x => x.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // Properties mapping
        builder.Property(x => x.TaxVersion)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.ResidencyStatus)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.Frequency)
               .HasColumnType("nvarchar(20)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.LowerLimit)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.UpperLimit)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.TaxRate)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.FixedTaxAmount)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(x => x.EffectiveDate)
               .HasColumnType("datetime2")
               .IsRequired();

        // Audit Columns
        builder.Property(x => x.CreatedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.CreatedAt)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.ModifiedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired(false);

        builder.Property(x => x.ModifiedAt)
               .HasColumnType("datetime2")
               .IsRequired(false);
    }
}
