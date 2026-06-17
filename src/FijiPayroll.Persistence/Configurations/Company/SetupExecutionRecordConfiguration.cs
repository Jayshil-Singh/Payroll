using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the SetupExecutionRecord entity.
/// </summary>
internal sealed class SetupExecutionRecordConfiguration : IEntityTypeConfiguration<SetupExecutionRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SetupExecutionRecord> builder)
    {
        builder.ToTable("SetupExecutionRecords", "company");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.ExecutionId).IsRequired();

        // Unique index for true idempotency
        builder.HasIndex(x => new { x.CompanyId, x.ExecutionId }).IsUnique();

        builder.Property(x => x.StartedUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.CompletedUtc).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.DurationMilliseconds).IsRequired(false);

        builder.Property(x => x.MachineName)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.ApplicationVersion)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.ErrorMessage)
               .HasColumnType("nvarchar(500)")
               .IsRequired(false);

        builder.Property(x => x.ErrorStackTrace)
               .HasColumnType("nvarchar(max)")
               .IsRequired(false);

        builder.Property(x => x.Status)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasConversion<string>();

        builder.ConfigureSoftDeleteAndAudit();
    }
}
