using FijiPayroll.Domain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Payroll;

/// <summary>
/// EF Core configuration mapping for RetroactiveAdjustment entity.
/// </summary>
internal sealed class RetroactiveAdjustmentConfiguration : IEntityTypeConfiguration<RetroactiveAdjustment>
{
    public void Configure(EntityTypeBuilder<RetroactiveAdjustment> builder)
    {
        builder.ToTable("RetroactiveAdjustments", "payroll");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Concurrency token
        builder.Property(x => x.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // Columns
        builder.Property(x => x.CompanyId)
               .IsRequired();

        builder.Property(x => x.EmployeeId)
               .IsRequired();

        builder.Property(x => x.Amount)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.ComponentType)
               .HasColumnType("nvarchar(20)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.ComponentName)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        builder.Property(x => x.Description)
               .HasColumnType("nvarchar(500)")
               .IsRequired(false);

        builder.Property(x => x.IsApplied)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(x => x.AppliedInPayrollRunId)
               .IsRequired(false);

        builder.Property(x => x.AppliedAt)
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

        // Indexes
        builder.HasIndex(x => new { x.CompanyId, x.EmployeeId });
        builder.HasIndex(x => x.IsApplied);
    }
}
