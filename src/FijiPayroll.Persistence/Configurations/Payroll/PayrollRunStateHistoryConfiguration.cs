using FijiPayroll.Domain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Payroll;

/// <summary>
/// EF Core mapping configuration for the PayrollRunStateHistory audit entity.
/// </summary>
internal sealed class PayrollRunStateHistoryConfiguration : IEntityTypeConfiguration<PayrollRunStateHistory>
{
    public void Configure(EntityTypeBuilder<PayrollRunStateHistory> builder)
    {
        builder.ToTable("PayrollRunStateHistories", "payroll");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.PayrollRunId)
               .IsRequired();

        builder.Property(x => x.FromStatus)
               .HasColumnType("nvarchar(20)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.ToStatus)
               .HasColumnType("nvarchar(20)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.ChangedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.ChangedAt)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.Notes)
               .HasColumnType("nvarchar(500)")
               .IsRequired(false);

        builder.HasOne(x => x.PayrollRun)
               .WithMany()
               .HasForeignKey(x => x.PayrollRunId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
