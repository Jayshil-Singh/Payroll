using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the Company tenant aggregate.
/// </summary>
internal sealed class CompanyConfiguration : IEntityTypeConfiguration<FijiPayroll.Domain.Entities.Company.Company>
{
    public void Configure(EntityTypeBuilder<FijiPayroll.Domain.Entities.Company.Company> builder)
    {
        builder.ToTable("Companies", "company");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Concurrency token
        builder.Property(x => x.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // Profile Properties
        builder.Property(x => x.LegalName)
               .HasColumnType("nvarchar(250)")
               .IsRequired();

        builder.Property(x => x.SecurityIsolatorKey)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.TimeZone)
               .HasColumnType("nvarchar(100)")
               .HasDefaultValue("Fiji Standard Time")
               .IsRequired();

        builder.Property(x => x.DefaultCurrency)
               .HasColumnType("nvarchar(10)")
               .HasDefaultValue("FJD")
               .IsRequired();

        builder.Property(x => x.NegativeNetPayPolicy)
               .HasConversion<int>()
               .HasDefaultValue(NegativeNetPayPolicy.PartialDeduction)
               .IsRequired();

        // Archiving Properties
        builder.Property(x => x.IsArchived)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(x => x.ArchivedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired(false);

        builder.Property(x => x.ArchivedDate)
               .HasColumnType("datetime2")
               .IsRequired(false);

        builder.Property(x => x.ArchiveReason)
               .HasColumnType("nvarchar(500)")
               .IsRequired(false);

        // Soft Delete Properties
        builder.Property(x => x.IsDeleted)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(x => x.DeletedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired(false);

        builder.Property(x => x.DeletedAt)
               .HasColumnType("datetime2")
               .IsRequired(false);

        // Global query filters automatically hide soft-deleted companies
        builder.HasQueryFilter(x => !x.IsDeleted);

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
