using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for the ApprovalConfig entity.
/// </summary>
internal sealed class ApprovalConfigConfiguration : IEntityTypeConfiguration<ApprovalConfig>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ApprovalConfig> builder)
    {
        builder.ToTable("ApprovalConfigs", "company");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();

        builder.Property(x => x.UserId)
               .HasColumnType("nvarchar(100)")
               .IsRequired(false);

        builder.Property(x => x.EmployeeId)
               .IsRequired(false);

        builder.Property(x => x.ApprovalLevel).IsRequired();

        builder.Property(x => x.Role)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.ConfigureSoftDeleteAndAudit();
    }
}
