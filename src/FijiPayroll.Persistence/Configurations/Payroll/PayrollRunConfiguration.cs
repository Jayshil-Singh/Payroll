using FijiPayroll.Domain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Payroll;

/// <summary>
/// EF Core mapping configuration for the PayrollRun aggregate root entity.
/// </summary>
internal sealed class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.ToTable("PayrollRuns", "payroll");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Concurrency token
        builder.Property(x => x.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        builder.Property(x => x.CompanyId)
               .IsRequired();

        builder.Property(x => x.RunCode)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.PeriodName)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        builder.Property(x => x.StartDate)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.EndDate)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.PaymentDate)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.Frequency)
               .HasColumnType("nvarchar(20)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.Status)
               .HasColumnType("nvarchar(20)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.Description)
               .HasColumnType("nvarchar(500)")
               .IsRequired(false);

        builder.Property(x => x.LockedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired(false);

        builder.Property(x => x.LockedAt)
               .HasColumnType("datetime2")
               .IsRequired(false);

        builder.Property(x => x.CurrentRequestId)
               .IsRequired(false);

        builder.Property(x => x.SnapshotHash)
               .HasColumnType("nvarchar(100)")
               .IsRequired(false);

        // One-to-many relationship mappings with cascade delete
        builder.HasMany(x => x.Employees)
               .WithOne()
               .HasForeignKey(x => x.PayrollRunId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.StateHistory)
               .WithOne()
               .HasForeignKey(x => x.PayrollRunId)
               .OnDelete(DeleteBehavior.Cascade);

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
