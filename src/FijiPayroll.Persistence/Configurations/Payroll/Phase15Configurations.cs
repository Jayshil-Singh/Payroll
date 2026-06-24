using FijiPayroll.Domain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Payroll;

internal sealed class PayrollPeriodConfiguration : IEntityTypeConfiguration<PayrollPeriod>
{
    public void Configure(EntityTypeBuilder<PayrollPeriod> builder)
    {
        builder.ToTable("PayrollPeriods", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.PeriodCode).HasColumnType("nvarchar(50)").IsRequired();
        builder.Property(x => x.PayrollFrequency).HasColumnType("nvarchar(50)").IsRequired().HasConversion<string>();
        builder.Property(x => x.FiscalYear).IsRequired();
        builder.Property(x => x.FiscalMonth).IsRequired();
        builder.Property(x => x.StartDate).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.EndDate).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.PaymentDate).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.Status).HasColumnType("nvarchar(50)").IsRequired().HasConversion<string>();

        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ModifiedBy).HasColumnType("nvarchar(100)").IsRequired(false);
        builder.Property(x => x.ModifiedAt).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.CompanyId, x.PayrollFrequency, x.Status });
    }
}

internal sealed class PayrollGroupConfiguration : IEntityTypeConfiguration<PayrollGroup>
{
    public void Configure(EntityTypeBuilder<PayrollGroup> builder)
    {
        builder.ToTable("PayrollGroups", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.Name).HasColumnType("nvarchar(200)").IsRequired();
        builder.Property(x => x.Code).HasColumnType("nvarchar(50)").IsRequired();
        builder.Property(x => x.FilterCriteria).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.DefaultBankAccountId).IsRequired(false);
        builder.Property(x => x.DefaultCalendarId).IsRequired(false);
        builder.Property(x => x.DefaultCostCentre).HasColumnType("nvarchar(100)").IsRequired(false);
        builder.Property(x => x.DefaultLeaveRulesPackageId).IsRequired(false);
        builder.Property(x => x.ApprovalWorkflowId).IsRequired(false);

        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ModifiedBy).HasColumnType("nvarchar(100)").IsRequired(false);
        builder.Property(x => x.ModifiedAt).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
    }
}

internal sealed class PayrollAdjustmentConfiguration : IEntityTypeConfiguration<PayrollAdjustment>
{
    public void Configure(EntityTypeBuilder<PayrollAdjustment> builder)
    {
        builder.ToTable("PayrollAdjustments", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.EmployeeId).IsRequired();
        builder.Property(x => x.Type).HasColumnType("nvarchar(50)").IsRequired().HasConversion<string>();
        builder.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Description).HasColumnType("nvarchar(500)").IsRequired();
        builder.Property(x => x.IsApplied).IsRequired();
        builder.Property(x => x.AppliedDate).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.AppliedInPayrollRunId).IsRequired(false);
        builder.Property(x => x.IsCancelled).IsRequired();
        builder.Property(x => x.CancelledBy).HasColumnType("nvarchar(100)").IsRequired(false);
        builder.Property(x => x.CancelledDate).HasColumnType("datetime2").IsRequired(false);

        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ModifiedBy).HasColumnType("nvarchar(100)").IsRequired(false);
        builder.Property(x => x.ModifiedAt).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.CompanyId, x.EmployeeId, x.IsApplied });
    }
}

internal sealed class PayrollSnapshotConfiguration : IEntityTypeConfiguration<PayrollSnapshot>
{
    public void Configure(EntityTypeBuilder<PayrollSnapshot> builder)
    {
        builder.ToTable("PayrollSnapshots", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.PayrollRunId).IsRequired();
        builder.Property(x => x.Version).IsRequired();
        builder.Property(x => x.Hash).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.JsonPayload).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedDate).HasColumnType("datetime2").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.PayrollRunId, x.Version });
    }
}

internal sealed class PayrollExceptionQueueConfiguration : IEntityTypeConfiguration<PayrollExceptionQueue>
{
    public void Configure(EntityTypeBuilder<PayrollExceptionQueue> builder)
    {
        builder.ToTable("PayrollExceptionQueues", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.PayrollRunId).IsRequired();
        builder.Property(x => x.EmployeeId).IsRequired();
        builder.Property(x => x.EmployeeName).HasColumnType("nvarchar(200)").IsRequired();
        builder.Property(x => x.Reason).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.Severity).HasColumnType("nvarchar(50)").IsRequired().HasConversion<string>();
        builder.Property(x => x.Recommendation).HasColumnType("nvarchar(500)").IsRequired();
        builder.Property(x => x.StackTrace).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.AuditId).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.OperatorResolution).HasColumnType("nvarchar(500)").IsRequired(false);
        builder.Property(x => x.ResolvedDate).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.ResolvedBy).HasColumnType("nvarchar(100)").IsRequired(false);
        builder.Property(x => x.IsResolved).IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.PayrollRunId, x.IsResolved });
    }
}

internal sealed class PayrollRunHistoryConfiguration : IEntityTypeConfiguration<PayrollRunHistory>
{
    public void Configure(EntityTypeBuilder<PayrollRunHistory> builder)
    {
        builder.ToTable("PayrollRunHistories", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.PayrollRunId).IsRequired();
        builder.Property(x => x.Action).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.Timestamp).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.User).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.Machine).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CorrelationId).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.Description).HasColumnType("nvarchar(1000)").IsRequired();
        builder.Property(x => x.OldValues).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.NewValues).HasColumnType("nvarchar(max)").IsRequired(false);

        builder.HasIndex(x => new { x.CompanyId, x.PayrollRunId });
    }
}

internal sealed class BackgroundJobConfiguration : IEntityTypeConfiguration<BackgroundJob>
{
    public void Configure(EntityTypeBuilder<BackgroundJob> builder)
    {
        builder.ToTable("BackgroundJobs", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.JobType).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.Status).HasColumnType("nvarchar(50)").IsRequired();
        builder.Property(x => x.Parameters).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.Progress).IsRequired();
        builder.Property(x => x.ErrorMessage).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.CreatedUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ScheduledUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.StartedUtc).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.CompletedUtc).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.RetryCount).IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.Status });
    }
}

internal sealed class PayrollLedgerEmployeeConfiguration : IEntityTypeConfiguration<PayrollLedgerEmployee>
{
    public void Configure(EntityTypeBuilder<PayrollLedgerEmployee> builder)
    {
        builder.ToTable("PayrollLedgerEmployees", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.PayrollLedgerId).IsRequired();
        builder.Property(x => x.EmployeeId).IsRequired();
        builder.Property(x => x.EmployeeName).HasColumnType("nvarchar(200)").IsRequired();
        builder.Property(x => x.EmployeeTin).HasColumnType("nvarchar(50)").IsRequired();
        builder.Property(x => x.EmployeeFnpfNumber).HasColumnType("nvarchar(50)").IsRequired();
        builder.Property(x => x.Gross).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.PAYE).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.FNPFEmployee).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.FNPFEmployer).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.NetPay).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Hash).HasColumnType("nvarchar(100)").IsRequired();

        builder.HasMany(x => x.Components)
            .WithOne(x => x.PayrollLedgerEmployee)
            .HasForeignKey(x => x.PayrollLedgerEmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CompanyId, x.PayrollLedgerId });
        builder.HasIndex(x => x.EmployeeId);
    }
}

internal sealed class PayrollLedgerComponentConfiguration : IEntityTypeConfiguration<PayrollLedgerComponent>
{
    public void Configure(EntityTypeBuilder<PayrollLedgerComponent> builder)
    {
        builder.ToTable("PayrollLedgerComponents", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.PayrollLedgerEmployeeId).IsRequired();
        builder.Property(x => x.ComponentCode).HasColumnType("nvarchar(50)").IsRequired();
        builder.Property(x => x.ComponentName).HasColumnType("nvarchar(200)").IsRequired();
        builder.Property(x => x.Type).HasColumnType("nvarchar(50)").IsRequired().HasConversion<string>();
        builder.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();


    }
}

internal sealed class PayrollLedgerTransactionConfiguration : IEntityTypeConfiguration<PayrollLedgerTransaction>
{
    public void Configure(EntityTypeBuilder<PayrollLedgerTransaction> builder)
    {
        builder.ToTable("PayrollLedgerTransactions", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.PayrollLedgerId).IsRequired();
        builder.Property(x => x.PayrollLedgerComponentId).IsRequired(false);
        builder.Property(x => x.EmployeeId).IsRequired(false);
        builder.Property(x => x.AccountCode).HasColumnType("nvarchar(50)").IsRequired();
        builder.Property(x => x.Debit).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Credit).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Description).HasColumnType("nvarchar(500)").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.PayrollLedgerId });
    }
}

internal sealed class PayrollLedgerReversalConfiguration : IEntityTypeConfiguration<PayrollLedgerReversal>
{
    public void Configure(EntityTypeBuilder<PayrollLedgerReversal> builder)
    {
        builder.ToTable("PayrollLedgerReversals", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.OriginalLedgerId).IsRequired();
        builder.Property(x => x.ReversalLedgerId).IsRequired();
        builder.Property(x => x.ReversalDate).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ReversalReason).HasColumnType("nvarchar(500)").IsRequired();
        builder.Property(x => x.User).HasColumnType("nvarchar(100)").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.OriginalLedgerId });
        builder.HasIndex(x => x.ReversalLedgerId);
    }
}
