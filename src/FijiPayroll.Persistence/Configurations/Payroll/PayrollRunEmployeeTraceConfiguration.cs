using FijiPayroll.Domain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Payroll;

/// <summary>
/// EF Core mapping configuration for the PayrollRunEmployeeTrace entity.
/// </summary>
internal sealed class PayrollRunEmployeeTraceConfiguration : IEntityTypeConfiguration<PayrollRunEmployeeTrace>
{
    public void Configure(EntityTypeBuilder<PayrollRunEmployeeTrace> builder)
    {
        builder.ToTable("PayrollRunEmployeeTraces", "payroll");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.PayrollRunEmployeeId)
               .IsRequired()
               .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);

        // Trace text payload mapping
        builder.Property(x => x.TraceText)
               .HasColumnType("nvarchar(max)")
               .IsRequired()
               .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
    }
}
