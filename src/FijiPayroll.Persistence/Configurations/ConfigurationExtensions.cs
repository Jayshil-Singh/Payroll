using FijiPayroll.Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations;

/// <summary>
/// Contains extension methods for EF Core entity configurations.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Configures the standard SoftDelete, Auditing, and Concurrency properties for entities inheriting from SoftDeleteEntity.
    /// </summary>
    public static void ConfigureSoftDeleteAndAudit<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : SoftDeleteEntity
    {
        // Concurrency token
        builder.Property(x => x.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // Soft Delete
        builder.Property(x => x.IsDeleted)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(x => x.DeletedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired(false);

        builder.Property(x => x.DeletedAt)
               .HasColumnType("datetime2")
               .IsRequired(false);

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
