using FijiPayroll.Domain.Entities.Leave;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration for the LeaveType entity.
/// </summary>
internal sealed class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("LeaveTypes", "leave");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.TypeName)
               .HasColumnType("nvarchar(150)")
               .IsRequired();

        builder.Property(x => x.Category)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.EntitlementDays)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.IsPaid)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(x => x.ApplyLeaveLoading)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(x => x.MaxCarryOverDays)
               .HasColumnType("decimal(18,4)")
               .IsRequired(false);

        builder.Property(x => x.RequiresMedicalCertificate)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(x => x.MedicalCertificateAfterDays)
               .IsRequired(false);

        builder.Property(x => x.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(x => x.Description)
               .HasColumnType("nvarchar(500)")
               .IsRequired(false);

        builder.ConfigureSoftDeleteAndAudit();
    }
}

/// <summary>
/// EF Core configuration for the LeaveBalance entity.
/// </summary>
internal sealed class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ToTable("EmployeeLeaveBalances", "employee");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Entitlement)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.Accrued)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.CarriedForward)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.Taken)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.Pending)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.ClosingBalance)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.HasOne(x => x.LeaveType)
               .WithMany()
               .HasForeignKey(x => x.LeaveTypeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
               .WithMany()
               .HasForeignKey(x => x.EmployeeId)
               .OnDelete(DeleteBehavior.Restrict);

        // Auditable Entity columns mapping
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

/// <summary>
/// EF Core configuration for the LeaveRequest entity.
/// </summary>
internal sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests", "leave");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.StartDate)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.EndDate)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.TotalDays)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.Status)
               .HasColumnType("nvarchar(50)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.Notes)
               .HasColumnType("nvarchar(1000)")
               .IsRequired(false);

        builder.Property(x => x.ApprovedRejectedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired(false);

        builder.Property(x => x.ApprovedRejectedAt)
               .HasColumnType("datetime2")
               .IsRequired(false);

        builder.Property(x => x.RejectionReason)
               .HasColumnType("nvarchar(500)")
               .IsRequired(false);

        builder.Property(x => x.CancellationReason)
               .HasColumnType("nvarchar(500)")
               .IsRequired(false);

        builder.Property(x => x.MedicalCertificateRequired)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(x => x.MedicalCertificateProvided)
               .IsRequired()
               .HasDefaultValue(false);

        builder.HasOne(x => x.LeaveType)
               .WithMany()
               .HasForeignKey(x => x.LeaveTypeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
               .WithMany()
               .HasForeignKey(x => x.EmployeeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.ConfigureSoftDeleteAndAudit();
    }
}

/// <summary>
/// EF Core configuration for the LeaveTransaction entity.
/// </summary>
internal sealed class LeaveTransactionConfiguration : IEntityTypeConfiguration<LeaveTransaction>
{
    public void Configure(EntityTypeBuilder<LeaveTransaction> builder)
    {
        builder.ToTable("LeaveTransactions", "payroll");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.PeriodStart)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.PeriodEnd)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.DaysDeducted)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.LeavePay)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.LeaveLoading)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.HasOne(x => x.LeaveType)
               .WithMany()
               .HasForeignKey(x => x.LeaveTypeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
               .WithMany()
               .HasForeignKey(x => x.EmployeeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PayrollRun>()
               .WithMany()
               .HasForeignKey(x => x.PayrollRunId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LeaveRequest>()
               .WithMany()
               .HasForeignKey(x => x.LeaveRequestId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);

        // Auditable Entity columns mapping
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

/// <summary>
/// EF Core configuration for the LeaveAccrualPolicy entity.
/// </summary>
internal sealed class LeaveAccrualPolicyConfiguration : IEntityTypeConfiguration<LeaveAccrualPolicy>
{
    public void Configure(EntityTypeBuilder<LeaveAccrualPolicy> builder)
    {
        builder.ToTable("LeaveAccrualPolicies", "leave");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.PolicyName)
               .HasColumnType("nvarchar(150)")
               .IsRequired();

        builder.Property(x => x.AccrualMethod)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.AccrualRatePerDay)
               .HasColumnType("decimal(18,6)")
               .IsRequired();

        builder.Property(x => x.MaxCarryOverDays)
               .HasColumnType("decimal(18,4)")
               .IsRequired(false);

        builder.Property(x => x.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(x => x.EffectiveFrom)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.EffectiveTo)
               .HasColumnType("datetime2")
               .IsRequired(false);

        builder.HasOne(x => x.LeaveType)
               .WithMany()
               .HasForeignKey(x => x.LeaveTypeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.ConfigureSoftDeleteAndAudit();
    }
}
