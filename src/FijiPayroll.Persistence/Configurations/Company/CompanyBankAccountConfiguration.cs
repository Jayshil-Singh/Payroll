using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the CompanyBankAccount entity.
/// </summary>
internal sealed class CompanyBankAccountConfiguration : IEntityTypeConfiguration<CompanyBankAccount>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CompanyBankAccount> builder)
    {
        builder.ToTable("CompanyBankAccounts", "company");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();

        builder.Property(x => x.AccountName)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        builder.Property(x => x.BankMasterId).IsRequired();
        builder.Property(x => x.BankBranchId).IsRequired();

        builder.Property(x => x.AccountType)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasConversion<string>();

        // Apply encryption value converter
        builder.Property(x => x.EncryptedAccountNumber)
               .HasColumnType("nvarchar(1000)")
               .IsRequired()
               .HasConversion<TenantEncryptionValueConverter>();

        builder.Property(x => x.AccountNumberHash)
               .HasColumnType("nvarchar(128)")
               .IsRequired();

        // Unique index per company to prevent duplicate bank accounts
        builder.HasIndex(x => new { x.CompanyId, x.AccountNumberHash }).IsUnique();

        builder.Property(x => x.Last4Digits)
               .HasColumnType("nvarchar(10)")
               .IsRequired();

        builder.Property(x => x.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.ConfigureSoftDeleteAndAudit();
    }
}
