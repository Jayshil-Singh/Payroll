using FijiPayroll.Domain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Payroll;

/// <summary>
/// EF Core mapping configuration for the PayrollRunEmployee entity.
/// </summary>
internal sealed class PayrollRunEmployeeConfiguration : IEntityTypeConfiguration<PayrollRunEmployee>
{
    public void Configure(EntityTypeBuilder<PayrollRunEmployee> builder)
    {
        builder.ToTable("PayrollRunEmployees", "payroll");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.PayrollRunId)
               .IsRequired();

        builder.HasOne(x => x.PayrollRun)
               .WithMany()
               .HasForeignKey(x => x.PayrollRunId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.EmployeeId)
               .IsRequired();

        builder.Property(x => x.EmployeeName)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        builder.Property(x => x.Tin)
               .HasColumnType("nvarchar(20)")
               .IsRequired();

        builder.Property(x => x.FnpfNumber)
               .HasColumnType("nvarchar(20)")
               .IsRequired();

        builder.Property(x => x.ResidencyStatus)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.Department)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.BaseSalary)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.GrossPay)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.TotalAllowances)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.TotalDeductions)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.NetPay)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.PayeTax)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.FnpfEmployeeContribution)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.FnpfEmployerContribution)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.TaxVersionUsed)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.IsSuperseded)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(x => x.CalculationRequestId)
               .IsRequired();

        // 1-to-1 trace navigation mapping
        builder.HasOne(x => x.Trace)
               .WithOne(x => x.PayrollRunEmployee)
               .HasForeignKey<PayrollRunEmployeeTrace>(x => x.PayrollRunEmployeeId)
               .OnDelete(DeleteBehavior.Restrict);

        // One-to-many relationship lines mapping with Cascade Delete
        builder.HasMany(x => x.LineItems)
               .WithOne(x => x.PayrollRunEmployee)
               .HasForeignKey(x => x.PayrollRunEmployeeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.PayrollRunId);
        builder.HasIndex(x => x.EmployeeId);
    }
}
