using FijiPayroll.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Audit;

internal sealed class ImportSessionConfiguration : IEntityTypeConfiguration<ImportSession>
{
    public void Configure(EntityTypeBuilder<ImportSession> builder)
    {
        builder.ToTable("ImportSessions", "audit");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.SessionId)
               .IsRequired();
        builder.HasIndex(x => x.SessionId).IsUnique();

        builder.Property(x => x.OriginalFileName)
               .HasColumnType("nvarchar(255)")
               .IsRequired();

        builder.Property(x => x.UploadedSize)
               .IsRequired();

        builder.Property(x => x.MimeType)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.Started)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.Validated)
               .HasColumnType("datetime2")
               .IsRequired(false);

        builder.Property(x => x.Approved)
               .HasColumnType("datetime2")
               .IsRequired(false);

        builder.Property(x => x.Committed)
               .HasColumnType("datetime2")
               .IsRequired(false);

        builder.Property(x => x.Archived)
               .HasColumnType("datetime2")
               .IsRequired(false);

        builder.Property(x => x.ImportedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.ImportSource)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.ImportHash)
               .HasColumnType("nvarchar(64)")
               .IsRequired();

        builder.Property(x => x.Status)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.SuccessCount)
               .IsRequired();

        builder.Property(x => x.FailureCount)
               .IsRequired();

        builder.Property(x => x.RollbackSupported)
               .IsRequired();

        // Navigation
        builder.HasMany(x => x.Rows)
               .WithOne(x => x.ImportSession)
               .HasForeignKey(x => x.ImportSessionId)
               .OnDelete(DeleteBehavior.Cascade);

        // Auditable fields
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

        builder.Property(x => x.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        builder.HasIndex(x => x.ImportHash).IsUnique();
    }
}
