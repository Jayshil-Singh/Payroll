using FijiPayroll.Domain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Payroll;

internal sealed class CompliancePeriodConfiguration : IEntityTypeConfiguration<CompliancePeriod>
{
    public void Configure(EntityTypeBuilder<CompliancePeriod> builder)
    {
        builder.ToTable("CompliancePeriods", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.Month).IsRequired();
        builder.Property(x => x.Year).IsRequired();
        builder.Property(x => x.StartDate).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.EndDate).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.Status).HasColumnType("nvarchar(20)").IsRequired().HasConversion<string>();
        
        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ModifiedBy).HasColumnType("nvarchar(100)").IsRequired(false);
        builder.Property(x => x.ModifiedAt).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
    }
}

internal sealed class ComplianceBatchConfiguration : IEntityTypeConfiguration<ComplianceBatch>
{
    public void Configure(EntityTypeBuilder<ComplianceBatch> builder)
    {
        builder.ToTable("ComplianceBatches", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.CompliancePeriodId).IsRequired();
        builder.Property(x => x.BatchName).HasColumnType("nvarchar(200)").IsRequired();
        builder.Property(x => x.Status).HasColumnType("nvarchar(20)").IsRequired().HasConversion<string>();
        builder.Property(x => x.DigitalSignature).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.CertificateThumbprint).HasColumnType("nvarchar(100)").IsRequired(false);
        builder.Property(x => x.FileHash).HasColumnType("nvarchar(100)").IsRequired(false);

        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ModifiedBy).HasColumnType("nvarchar(100)").IsRequired(false);
        builder.Property(x => x.ModifiedAt).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
    }
}

internal sealed class PayrollLedgerConfiguration : IEntityTypeConfiguration<PayrollLedger>
{
    public void Configure(EntityTypeBuilder<PayrollLedger> builder)
    {
        builder.ToTable("PayrollLedgers", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.PayrollRunId).IsRequired();
        builder.Property(x => x.TotalGross).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.TotalPAYE).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.TotalFNPFEmployee).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.TotalFNPFEmployer).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.TotalNetPay).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Hash).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.IsReversed).IsRequired();
        builder.Property(x => x.ReversalDate).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.ReversalReason).HasColumnType("nvarchar(500)").IsRequired(false);

        builder.HasMany(x => x.Employees)
            .WithOne()
            .HasForeignKey(x => x.PayrollLedgerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Transactions)
            .WithOne()
            .HasForeignKey(x => x.PayrollLedgerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.PayrollRunId);
    }
}

internal sealed class ComplianceEventConfiguration : IEntityTypeConfiguration<ComplianceEvent>
{
    public void Configure(EntityTypeBuilder<ComplianceEvent> builder)
    {
        builder.ToTable("ComplianceEvents", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CorrelationId).IsRequired();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.EventType).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.User).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.Machine).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.ApplicationVersion).HasColumnType("nvarchar(50)").IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();

        builder.HasIndex(x => x.CorrelationId);
    }
}

internal sealed class ApprovalMatrixConfiguration : IEntityTypeConfiguration<ApprovalMatrix>
{
    public void Configure(EntityTypeBuilder<ApprovalMatrix> builder)
    {
        builder.ToTable("ApprovalMatrices", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.Role).HasColumnType("nvarchar(50)").IsRequired().HasConversion<string>();
        builder.Property(x => x.ActionType).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.MinThreshold).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.MaxThreshold).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ModifiedBy).HasColumnType("nvarchar(100)").IsRequired(false);
        builder.Property(x => x.ModifiedAt).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
    }
}

internal sealed class ComplianceSnapshotConfiguration : IEntityTypeConfiguration<ComplianceSnapshot>
{
    public void Configure(EntityTypeBuilder<ComplianceSnapshot> builder)
    {
        builder.ToTable("ComplianceSnapshots", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.ComplianceBatchId).IsRequired(false);
        builder.Property(x => x.PayrollRunId).IsRequired();
        builder.Property(x => x.SnapshotVersion).HasColumnType("nvarchar(50)").IsRequired();
        builder.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.SHA256Hash).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();

        builder.HasIndex(x => x.ComplianceBatchId);
    }
}

internal sealed class ComplianceAmendmentConfiguration : IEntityTypeConfiguration<ComplianceAmendment>
{
    public void Configure(EntityTypeBuilder<ComplianceAmendment> builder)
    {
        builder.ToTable("ComplianceAmendments", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.OriginalSubmissionId).IsRequired();
        builder.Property(x => x.PreviousSubmissionId).IsRequired();
        builder.Property(x => x.CurrentSubmissionId).IsRequired();
        builder.Property(x => x.Reason).HasColumnType("nvarchar(500)").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedUtc).HasColumnType("datetime2").IsRequired();

        builder.HasIndex(x => x.OriginalSubmissionId);
        builder.HasIndex(x => x.PreviousSubmissionId);
        builder.HasIndex(x => x.CurrentSubmissionId);
    }
}

internal sealed class StatutoryRuleConfiguration : IEntityTypeConfiguration<StatutoryRule>
{
    public void Configure(EntityTypeBuilder<StatutoryRule> builder)
    {
        builder.ToTable("StatutoryRules", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Authority).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.RuleCode).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.RuleValue).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.Description).HasColumnType("nvarchar(500)").IsRequired();
        builder.Property(x => x.EffectiveFrom).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.EffectiveTo).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.IsActive).IsRequired();

        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ModifiedBy).HasColumnType("nvarchar(100)").IsRequired(false);
        builder.Property(x => x.ModifiedAt).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.Authority, x.RuleCode });
    }
}

internal sealed class FileLayoutDefinitionConfiguration : IEntityTypeConfiguration<FileLayoutDefinition>
{
    public void Configure(EntityTypeBuilder<FileLayoutDefinition> builder)
    {
        builder.ToTable("FileLayoutDefinitions", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.OwnerCode).HasColumnType("nvarchar(50)").IsRequired();
        builder.Property(x => x.LayoutType).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.HeaderTemplate).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.DetailTemplate).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.FooterTemplate).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.ColumnDelimiter).HasColumnType("char(1)").IsRequired();
        builder.Property(x => x.FileExtension).HasColumnType("nvarchar(20)").IsRequired();

        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ModifiedBy).HasColumnType("nvarchar(100)").IsRequired(false);
        builder.Property(x => x.ModifiedAt).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
    }
}

internal sealed class ComplianceJobConfiguration : IEntityTypeConfiguration<ComplianceJob>
{
    public void Configure(EntityTypeBuilder<ComplianceJob> builder)
    {
        builder.ToTable("ComplianceJobs", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.JobType).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.Status).HasColumnType("nvarchar(20)").IsRequired().HasConversion<string>();
        builder.Property(x => x.ErrorMessage).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.AttemptCount).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.LastAttemptAt).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.CompletedAt).HasColumnType("datetime2").IsRequired(false);
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.Channel).HasColumnType("nvarchar(20)").IsRequired().HasConversion<string>();
        builder.Property(x => x.Recipient).HasColumnType("nvarchar(200)").IsRequired();
        builder.Property(x => x.Subject).HasColumnType("nvarchar(200)").IsRequired();
        builder.Property(x => x.Message).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.Status).HasColumnType("nvarchar(20)").IsRequired();
        builder.Property(x => x.RetryCount).IsRequired();
        builder.Property(x => x.ErrorMessage).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.SentAt).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.IsRead).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.ReadAt).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.Category).HasColumnType("nvarchar(50)").IsRequired().HasDefaultValue("Info");
    }
}

internal sealed class FRCSSubmissionConfiguration : IEntityTypeConfiguration<FRCSSubmission>
{
    public void Configure(EntityTypeBuilder<FRCSSubmission> builder)
    {
        builder.ToTable("FRCSSubmissions", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.CompliancePeriodId).IsRequired();
        builder.Property(x => x.Status).HasColumnType("nvarchar(20)").IsRequired().HasConversion<string>();
        builder.Property(x => x.FrcsFileContent).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.FilePath).HasColumnType("nvarchar(500)").IsRequired();
        builder.Property(x => x.Hash).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CalculationEngineVersion).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.FormulaEngineVersion).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.ComplianceEngineVersion).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.PinnedRuleVersion).HasColumnType("nvarchar(100)").IsRequired();

        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ModifiedBy).HasColumnType("nvarchar(100)").IsRequired(false);
        builder.Property(x => x.ModifiedAt).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
    }
}

internal sealed class FNPFSubmissionConfiguration : IEntityTypeConfiguration<FNPFSubmission>
{
    public void Configure(EntityTypeBuilder<FNPFSubmission> builder)
    {
        builder.ToTable("FNPFSubmissions", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.CompliancePeriodId).IsRequired();
        builder.Property(x => x.Status).HasColumnType("nvarchar(20)").IsRequired().HasConversion<string>();
        builder.Property(x => x.FnpfFileContent).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.FilePath).HasColumnType("nvarchar(500)").IsRequired();
        builder.Property(x => x.Hash).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CalculationEngineVersion).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.FormulaEngineVersion).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.ComplianceEngineVersion).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.PinnedRuleVersion).HasColumnType("nvarchar(100)").IsRequired();

        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ModifiedBy).HasColumnType("nvarchar(100)").IsRequired(false);
        builder.Property(x => x.ModifiedAt).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
    }
}

internal sealed class BankFileConfiguration : IEntityTypeConfiguration<BankFile>
{
    public void Configure(EntityTypeBuilder<BankFile> builder)
    {
        builder.ToTable("BankFiles", "payroll");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.BankCode).HasColumnType("nvarchar(50)").IsRequired();
        builder.Property(x => x.PayrollRunId).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.TotalEmployeesCount).IsRequired();
        builder.Property(x => x.FileContent).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.FilePath).HasColumnType("nvarchar(500)").IsRequired();
        builder.Property(x => x.Hash).HasColumnType("nvarchar(100)").IsRequired();

        builder.Property(x => x.CreatedBy).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ModifiedBy).HasColumnType("nvarchar(100)").IsRequired(false);
        builder.Property(x => x.ModifiedAt).HasColumnType("datetime2").IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
    }
}
