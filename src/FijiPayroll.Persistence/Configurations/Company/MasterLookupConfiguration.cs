using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the MasterLookup entity.
/// </summary>
internal sealed class MasterLookupConfiguration : IEntityTypeConfiguration<MasterLookup>
{
    public void Configure(EntityTypeBuilder<MasterLookup> builder)
    {
        builder.ToTable("MasterLookups", "company");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Concurrency token
        builder.Property(x => x.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // Profile Properties
        builder.Property(x => x.Category)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.Code)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.Name)
               .HasColumnType("nvarchar(250)")
               .IsRequired();

        builder.Property(x => x.EffectiveFrom)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.EffectiveTo)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.ParentId)
               .IsRequired(false);

        builder.Property(x => x.DisplayOrder)
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(x => x.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

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

        // Global query filters automatically hide soft-deleted lookups
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

        // Indexes for performance
        builder.HasIndex(x => new { x.CompanyId, x.Category, x.Code })
               .HasDatabaseName("IX_MasterLookups_CompanyId_Category_Code");
    }
}
