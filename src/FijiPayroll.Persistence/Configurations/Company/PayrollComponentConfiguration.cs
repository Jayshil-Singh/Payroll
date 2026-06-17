using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="PayrollComponent"/> entity.
/// Maps to the <c>company.PayrollComponents</c> table as specified in Database.md §5.6.
/// </summary>
internal sealed class PayrollComponentConfiguration
    : IEntityTypeConfiguration<PayrollComponent>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<PayrollComponent> builder)
    {
        // ── Table and Schema ─────────────────────────────────────────────────────
        builder.ToTable("PayrollComponents", "company");

        // ── Primary Key ──────────────────────────────────────────────────────────
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
               .ValueGeneratedOnAdd();

        // ── Concurrency Token (ROWVERSION) ────────────────────────────────────────
        builder.Property(x => x.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // ── Foreign Keys ─────────────────────────────────────────────────────────
        builder.Property(x => x.CompanyId)
               .IsRequired();

        // Navigation to Company is not exposed on the entity but FK is still configured
        builder.HasIndex(x => x.CompanyId)
               .HasDatabaseName("IX_PayrollComponents_CompanyId");

        // ── Component Code ────────────────────────────────────────────────────────
        builder.Property(x => x.ComponentCode)
               .HasColumnType("nvarchar(20)")
               .IsRequired();

        // Unique per company
        builder.HasIndex(x => new { x.CompanyId, x.ComponentCode })
               .IsUnique()
               .HasDatabaseName("UX_PayrollComponents_CompanyId_ComponentCode")
               .HasFilter("[IsDeleted] = 0");       // Partial index — excludes soft-deleted

        // ── Component Name ────────────────────────────────────────────────────────
        builder.Property(x => x.ComponentName)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        // ── Component Type ────────────────────────────────────────────────────────
        builder.Property(x => x.ComponentType)
               .HasColumnType("nvarchar(20)")
               .IsRequired()
               .HasConversion<string>();             // Stored as string name in DB

        // ── Calculation Method ────────────────────────────────────────────────────
        builder.Property(x => x.CalculationMethod)
               .HasColumnType("nvarchar(20)")
               .IsRequired()
               .HasConversion<string>();

        // ── Calculation Value ─────────────────────────────────────────────────────
        builder.Property(x => x.CalculationValue)
               .HasColumnType("decimal(18,4)")
               .IsRequired(false);

        // ── Formula ───────────────────────────────────────────────────────────────
        builder.Property(x => x.Formula)
               .HasColumnType("nvarchar(max)")
               .IsRequired(false);

        // ── Flags ─────────────────────────────────────────────────────────────────
        builder.Property(x => x.IsSystemComponent)
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(x => x.IsTaxable)
               .HasDefaultValue(true)
               .IsRequired();

        builder.Property(x => x.IsFnpfApplicable)
               .HasDefaultValue(true)
               .IsRequired();

        builder.Property(x => x.DisplayOrder)
               .HasDefaultValue(0)
               .IsRequired();

        builder.Property(x => x.IsActive)
               .HasDefaultValue(true)
               .IsRequired();

        builder.Property(x => x.Status)
               .HasColumnType("nvarchar(20)")
               .HasDefaultValue(FijiPayroll.Domain.Enumerations.ComponentStatus.Active)
               .HasConversion<string>()
               .IsRequired();

        builder.HasMany(x => x.Rules)
               .WithOne(x => x.Component)
               .HasForeignKey(x => x.ComponentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Dependencies)
               .WithOne(x => x.ParentComponent)
               .HasForeignKey(x => x.ParentComponentId)
               .OnDelete(DeleteBehavior.Cascade);

        // ── Description ───────────────────────────────────────────────────────────
        builder.Property(x => x.Description)
               .HasColumnType("nvarchar(500)")
               .IsRequired(false);

        // ── Soft Delete ───────────────────────────────────────────────────────────
        builder.Property(x => x.IsDeleted)
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(x => x.DeletedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired(false);

        builder.Property(x => x.DeletedAt)
               .HasColumnType("datetime2")
               .IsRequired(false);

        // Global query filter: automatically exclude soft-deleted records from all queries
        builder.HasQueryFilter(x => !x.IsDeleted);

        // ── Audit Fields ──────────────────────────────────────────────────────────
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

        // ── Display Order Index ───────────────────────────────────────────────────
        builder.HasIndex(x => new { x.CompanyId, x.DisplayOrder })
               .HasDatabaseName("IX_PayrollComponents_CompanyId_DisplayOrder");
    }
}
