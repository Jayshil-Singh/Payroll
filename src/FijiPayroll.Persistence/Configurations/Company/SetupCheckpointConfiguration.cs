using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the SetupCheckpoint entity.
/// </summary>
internal sealed class SetupCheckpointConfiguration : IEntityTypeConfiguration<SetupCheckpoint>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SetupCheckpoint> builder)
    {
        builder.ToTable("SetupCheckpoints", "company");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.ExecutionId).IsRequired();

        builder.Property(x => x.Step)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.StartedUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.CompletedUtc).HasColumnType("datetime2").IsRequired(false);

        builder.Property(x => x.Status)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.Message)
               .HasColumnType("nvarchar(500)")
               .IsRequired();

        builder.ConfigureSoftDeleteAndAudit();
    }
}
