using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

internal sealed class PayrollComponentVersionConfiguration : IEntityTypeConfiguration<PayrollComponentVersion>
{
    public void Configure(EntityTypeBuilder<PayrollComponentVersion> builder)
    {
        builder.ToTable("PayrollComponentVersions", "company");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.PayrollComponentId)
               .IsRequired();

        builder.Property(x => x.VersionNumber)
               .IsRequired();

        builder.Property(x => x.VersionHash)
               .HasColumnType("nvarchar(64)")
               .IsRequired();

        builder.Property(x => x.ExpressionText)
               .HasColumnType("nvarchar(max)")
               .IsRequired();

        builder.Property(x => x.CalculationMethod)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.Taxable)
               .IsRequired();

        builder.Property(x => x.SubjectToFNPF)
               .IsRequired();

        builder.Property(x => x.Recurring)
               .IsRequired();

        builder.Property(x => x.Priority)
               .IsRequired();

        builder.Property(x => x.EffectiveFrom)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.EffectiveTo)
               .HasColumnType("datetime2")
               .IsRequired(false);

        builder.Property(x => x.CreatedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.CreatedDate)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.CreatedFromPayrollRunId)
               .IsRequired(false);

        builder.HasOne(x => x.PayrollComponent)
               .WithMany()
               .HasForeignKey(x => x.PayrollComponentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.PayrollComponentId, x.VersionNumber }).IsUnique();
    }
}
