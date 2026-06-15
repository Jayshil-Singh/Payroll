using FijiPayroll.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Audit;

/// <summary>
/// EF Core configuration mapping for the ImportJob entity.
/// </summary>
internal sealed class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.ToTable("ImportJobs", "audit");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.JobId)
               .IsRequired();

        builder.HasIndex(x => x.JobId)
               .IsUnique();

        builder.Property(x => x.CompanyId)
               .IsRequired();

        builder.Property(x => x.ModuleName)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.FileName)
               .HasColumnType("nvarchar(255)")
               .IsRequired();

        builder.Property(x => x.Status)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.ProcessedCount)
               .IsRequired();

        builder.Property(x => x.SuccessCount)
               .IsRequired();

        builder.Property(x => x.FailureCount)
               .IsRequired();

        builder.Property(x => x.Payload)
               .HasColumnType("nvarchar(max)")
               .IsRequired();

        builder.Property(x => x.ErrorMessage)
               .HasColumnType("nvarchar(max)")
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
