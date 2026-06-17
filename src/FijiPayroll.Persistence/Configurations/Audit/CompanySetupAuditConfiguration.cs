using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Audit;

/// <summary>
/// EF Core configuration mapping for the CompanySetupAudit entity.
/// </summary>
internal sealed class CompanySetupAuditConfiguration : IEntityTypeConfiguration<CompanySetupAudit>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CompanySetupAudit> builder)
    {
        builder.ToTable("CompanySetupAudits", "audit");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();

        builder.Property(x => x.Step)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.Action)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        builder.Property(x => x.Timestamp)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.Result)
               .HasColumnType("nvarchar(500)")
               .IsRequired();

        builder.Property(x => x.Status)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.IPAddress)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.MachineName)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.ApplicationVersion)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.CorrelationId).IsRequired();
        builder.Property(x => x.ExecutionId).IsRequired();

        builder.ConfigureSoftDeleteAndAudit();
    }
}
