using FijiPayroll.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Audit;

/// <summary>
/// EF Core configuration mapping for the AuditLog entity.
/// </summary>
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "audit");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId)
               .IsRequired();

        builder.Property(x => x.UserId)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.EntityName)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        builder.Property(x => x.EntityId)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.Action)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.Changes)
               .HasColumnType("nvarchar(max)")
               .IsRequired();

        builder.Property(x => x.Timestamp)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.CorrelationId)
               .IsRequired();
    }
}
