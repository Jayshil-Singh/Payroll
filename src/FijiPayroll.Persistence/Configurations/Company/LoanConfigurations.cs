using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration for the Loan entity.
/// </summary>
internal sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("StaffLoans", "employee");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.LoanDescription)
               .HasColumnType("nvarchar(500)")
               .IsRequired();

        builder.Property(x => x.PrincipalAmount)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.InterestRate)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.TotalAmountToRepay)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.RemainingBalance)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.DeductionAmountPerPeriod)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.StartDate)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.Status)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasConversion<string>();

        builder.HasOne<Employee>()
               .WithMany()
               .HasForeignKey(x => x.EmployeeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.ConfigureSoftDeleteAndAudit();
    }
}

/// <summary>
/// EF Core configuration for the LoanRepayment entity.
/// </summary>
internal sealed class LoanRepaymentConfiguration : IEntityTypeConfiguration<LoanRepayment>
{
    public void Configure(EntityTypeBuilder<LoanRepayment> builder)
    {
        builder.ToTable("StaffLoanRepayments", "payroll");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Amount)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.RemainingBalanceAfter)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.TransactionDate)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.HasOne<Loan>()
               .WithMany(x => x.Repayments)
               .HasForeignKey(x => x.LoanId)
               .OnDelete(DeleteBehavior.Cascade); // If loan is soft-deleted, delete repayments? Actually cascade delete at DB level is OK since cascade is set, but restrict is also fine. Let's do Cascade.

        builder.HasOne<PayrollRun>()
               .WithMany()
               .HasForeignKey(x => x.PayrollRunId)
               .OnDelete(DeleteBehavior.Restrict);

        // Auditable Entity columns mapping (not SoftDelete, so configure auditing manually)
        builder.Property(x => x.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

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
