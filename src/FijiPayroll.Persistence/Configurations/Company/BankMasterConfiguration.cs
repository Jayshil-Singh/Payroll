using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the BankMaster entity.
/// </summary>
internal sealed class BankMasterConfiguration : IEntityTypeConfiguration<BankMaster>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BankMaster> builder)
    {
        builder.ToTable("BankMasters", "company");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();

        builder.Property(x => x.BankCode)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.BankName)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        builder.Property(x => x.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        // One-to-many relationship with BankBranch
        builder.HasMany(x => x.Branches)
               .WithOne(x => x.BankMaster!)
               .HasForeignKey(x => x.BankMasterId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureSoftDeleteAndAudit();
    }
}
