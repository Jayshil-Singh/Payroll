using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the BankBranch entity.
/// </summary>
internal sealed class BankBranchConfiguration : IEntityTypeConfiguration<BankBranch>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BankBranch> builder)
    {
        builder.ToTable("BankBranches", "company");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.BankMasterId).IsRequired();

        builder.Property(x => x.BranchCode)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.BranchName)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        builder.Property(x => x.BsbCode)
               .HasColumnType("nvarchar(20)")
               .IsRequired();

        builder.Property(x => x.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.ConfigureSoftDeleteAndAudit();
    }
}
