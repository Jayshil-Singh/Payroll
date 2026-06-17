using FijiPayroll.Domain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Payroll;

/// <summary>
/// EF Core mapping configuration for the PayrollRunLineItem entity.
/// </summary>
internal sealed class PayrollRunLineItemConfiguration : IEntityTypeConfiguration<PayrollRunLineItem>
{
    public void Configure(EntityTypeBuilder<PayrollRunLineItem> builder)
    {
        builder.ToTable("PayrollRunLineItems", "payroll");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.PayrollRunEmployeeId)
               .IsRequired();

        builder.Property(x => x.ComponentId)
               .IsRequired();

        builder.Property(x => x.ComponentCode)
               .HasColumnType("nvarchar(20)")
               .IsRequired();

        builder.Property(x => x.ComponentName)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        builder.Property(x => x.ComponentType)
               .HasColumnType("nvarchar(20)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.Amount)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.IsTaxable)
               .IsRequired();

        builder.Property(x => x.AffectsFnpf)
               .IsRequired();

        builder.Property(x => x.EmployerContributionFlag)
               .IsRequired();

        builder.Property(x => x.ReferenceComponentId)
               .IsRequired();

        builder.HasIndex(x => x.PayrollRunEmployeeId);
        builder.HasIndex(x => x.ComponentId);
    }
}
