using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Persistence.Converters;

namespace FijiPayroll.Persistence.Context;

/// <summary>
/// Entity Framework Core database context for the Fiji Enterprise Payroll System.
/// Implements transactional control and automatically applies auditable interceptors.
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly AuditableEntityInterceptor? _auditableEntityInterceptor;
    private readonly AuditLogInterceptor? _auditLogInterceptor;
    private readonly ITenantProvider? _tenantProvider;
    private IDbContextTransaction? _currentTransaction;

    /// <summary>Gets the current resolved company tenant ID.</summary>
    public int CurrentCompanyId => _tenantProvider?.GetCurrentCompanyId() ?? 1;

    /// <summary>
    /// Initialises a new instance of the <see cref="ApplicationDbContext"/> class.
    /// Used for EF migrations and design-time tooling.
    /// </summary>
    /// <param name="options">Context configuration options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="ApplicationDbContext"/> class with interceptors and tenant provider.
    /// </summary>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditableEntityInterceptor auditableEntityInterceptor,
        ITenantProvider tenantProvider)
        : this(options, auditableEntityInterceptor, null!, tenantProvider)
    {
        SetTenantKey(tenantProvider);
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="ApplicationDbContext"/> class with interceptors, audit interceptor, and tenant provider.
    /// </summary>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditableEntityInterceptor auditableEntityInterceptor,
        AuditLogInterceptor auditLogInterceptor,
        ITenantProvider tenantProvider)
        : base(options)
    {
        _auditableEntityInterceptor = auditableEntityInterceptor;
        _auditLogInterceptor = auditLogInterceptor;
        _tenantProvider = tenantProvider;
        SetTenantKey(tenantProvider);
    }

    private static void SetTenantKey(ITenantProvider tenantProvider)
    {
        if (tenantProvider is not null)
        {
            try
            {
                TenantEncryptionValueConverter.CurrentKey = tenantProvider.GetCurrentTenantSecurityKey();
            }
            catch
            {
                // Fallback for design-time/migration scenarios
            }
        }
    }

    /// <summary>
    /// Gets or sets the payroll components DbSet.
    /// </summary>
    public DbSet<PayrollComponent> PayrollComponents => Set<PayrollComponent>();

    /// <summary>Gets or sets the companies DbSet.</summary>
    public DbSet<Company> Companies => Set<Company>();

    /// <summary>Gets or sets the master lookups DbSet.</summary>
    public DbSet<MasterLookup> MasterLookups => Set<MasterLookup>();

    /// <summary>Gets or sets the employees DbSet.</summary>
    public DbSet<FijiPayroll.Domain.Entities.Company.Employee> Employees => Set<FijiPayroll.Domain.Entities.Company.Employee>();

    /// <summary>Gets or sets the tax brackets DbSet.</summary>
    public DbSet<FijiPayroll.Domain.Entities.Company.TaxBracket> TaxBrackets => Set<FijiPayroll.Domain.Entities.Company.TaxBracket>();

    /// <summary>Gets or sets the payroll runs DbSet.</summary>
    public DbSet<FijiPayroll.Domain.Entities.Payroll.PayrollRun> PayrollRuns => Set<FijiPayroll.Domain.Entities.Payroll.PayrollRun>();

    /// <summary>Gets or sets the payroll run employees DbSet.</summary>
    public DbSet<FijiPayroll.Domain.Entities.Payroll.PayrollRunEmployee> PayrollRunEmployees => Set<FijiPayroll.Domain.Entities.Payroll.PayrollRunEmployee>();

    /// <summary>Gets or sets the payroll run employee traces DbSet.</summary>
    public DbSet<FijiPayroll.Domain.Entities.Payroll.PayrollRunEmployeeTrace> PayrollRunEmployeeTraces => Set<FijiPayroll.Domain.Entities.Payroll.PayrollRunEmployeeTrace>();

    /// <summary>Gets or sets the payroll run line items DbSet.</summary>
    public DbSet<FijiPayroll.Domain.Entities.Payroll.PayrollRunLineItem> PayrollRunLineItems => Set<FijiPayroll.Domain.Entities.Payroll.PayrollRunLineItem>();

    /// <summary>Gets or sets the payroll run state histories DbSet.</summary>
    public DbSet<FijiPayroll.Domain.Entities.Payroll.PayrollRunStateHistory> PayrollRunStateHistories => Set<FijiPayroll.Domain.Entities.Payroll.PayrollRunStateHistory>();

    /// <summary>Gets or sets the audit logs DbSet.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>Gets or sets the entity events (transactional outbox) DbSet.</summary>
    public DbSet<EntityEvent> EntityEvents => Set<EntityEvent>();

    /// <summary>Gets or sets the import jobs DbSet.</summary>
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();

    /// <summary>Gets or sets the search indexes DbSet.</summary>
    public DbSet<SearchIndex> SearchIndexes => Set<SearchIndex>();

    /// <summary>Gets or sets the approval workflows DbSet.</summary>
    public DbSet<ApprovalWorkflow> ApprovalWorkflows => Set<ApprovalWorkflow>();

    /// <summary>Gets or sets the workflow step logs DbSet.</summary>
    public DbSet<WorkflowStepLog> WorkflowStepLogs => Set<WorkflowStepLog>();

    // ── Rule Engine and Staging DbSets ──────────────────────────────────────

    public DbSet<RuleModule> RuleModules => Set<RuleModule>();
    public DbSet<RuleSet> RuleSets => Set<RuleSet>();
    public DbSet<PayrollComponentRule> PayrollComponentRules => Set<PayrollComponentRule>();
    public DbSet<PayrollComponentDependency> PayrollComponentDependencies => Set<PayrollComponentDependency>();
    public DbSet<PayrollComponentVersion> PayrollComponentVersions => Set<PayrollComponentVersion>();
    public DbSet<RuleExecutionMetric> RuleExecutionMetrics => Set<RuleExecutionMetric>();
    public DbSet<ImportSession> ImportSessions => Set<ImportSession>();
    public DbSet<ImportSessionRow> ImportSessionRows => Set<ImportSessionRow>();
    public DbSet<ExportHistory> ExportHistories => Set<ExportHistory>();

    // ── Onboarding and Setup Wizard DbSets ──────────────────────────────────
    public DbSet<CompanySetupState> CompanySetupStates => Set<CompanySetupState>();
    public DbSet<CompanySetupTask> CompanySetupTasks => Set<CompanySetupTask>();
    public DbSet<SetupExecutionRecord> SetupExecutionRecords => Set<SetupExecutionRecord>();
    public DbSet<SetupCheckpoint> SetupCheckpoints => Set<SetupCheckpoint>();
    public DbSet<CompanySetupAudit> CompanySetupAudits => Set<CompanySetupAudit>();
    public DbSet<CompanySeedVersion> CompanySeedVersions => Set<CompanySeedVersion>();
    public DbSet<FiscalCalendar> FiscalCalendars => Set<FiscalCalendar>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<PayrollFrequencyDefinition> PayrollFrequencyDefinitions => Set<PayrollFrequencyDefinition>();
    public DbSet<PayPeriodSchedule> PayPeriodSchedules => Set<PayPeriodSchedule>();
    public DbSet<FnpfConfiguration> FnpfConfigurations => Set<FnpfConfiguration>();
    public DbSet<BankMaster> BankMasters => Set<BankMaster>();
    public DbSet<BankBranch> BankBranches => Set<BankBranch>();
    public DbSet<CompanyBankAccount> CompanyBankAccounts => Set<CompanyBankAccount>();
    public DbSet<ApprovalConfig> ApprovalConfigs => Set<ApprovalConfig>();

    // ── Compliance and Platform Hardening DbSets ────────────────────────────
    public DbSet<CompliancePeriod> CompliancePeriods => Set<CompliancePeriod>();
    public DbSet<ComplianceBatch> ComplianceBatches => Set<ComplianceBatch>();
    public DbSet<PayrollLedger> PayrollLedgers => Set<PayrollLedger>();
    public DbSet<ComplianceEvent> ComplianceEvents => Set<ComplianceEvent>();
    public DbSet<ApprovalMatrix> ApprovalMatrices => Set<ApprovalMatrix>();
    public DbSet<ComplianceSnapshot> ComplianceSnapshots => Set<ComplianceSnapshot>();
    public DbSet<ComplianceAmendment> ComplianceAmendments => Set<ComplianceAmendment>();
    public DbSet<StatutoryRule> StatutoryRules => Set<StatutoryRule>();
    public DbSet<FileLayoutDefinition> FileLayoutDefinitions => Set<FileLayoutDefinition>();
    public DbSet<ComplianceJob> ComplianceJobs => Set<ComplianceJob>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<FRCSSubmission> FRCSSubmissions => Set<FRCSSubmission>();
    public DbSet<FNPFSubmission> FNPFSubmissions => Set<FNPFSubmission>();
    public DbSet<BankFile> BankFiles => Set<BankFile>();

    /// <summary>
    /// Begins a database transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
        {
            return;
        }

        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Commits the current database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            if (_currentTransaction is not null)
            {
                await _currentTransaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction is not null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    /// <summary>
    /// Rolls back the current database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentTransaction is not null)
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
        }
        finally
        {
            if (_currentTransaction is not null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PreventTraceUpdates();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PreventTraceUpdates();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PreventTraceUpdates()
    {
        var entries = ChangeTracker.Entries<FijiPayroll.Domain.Entities.Payroll.PayrollRunEmployeeTrace>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                throw new InvalidOperationException("TRACE_RULE_VIOLATION: Updates, modifications, or deletions of PayrollRunEmployeeTrace records are strictly prohibited.");
            }
        }
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_auditableEntityInterceptor is not null)
        {
            optionsBuilder.AddInterceptors(_auditableEntityInterceptor);
        }

        if (_auditLogInterceptor is not null)
        {
            optionsBuilder.AddInterceptors(_auditLogInterceptor);
        }

        base.OnConfiguring(optionsBuilder);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations in the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Enforce global multi-tenancy query filters
        modelBuilder.Entity<FijiPayroll.Domain.Entities.Company.Employee>()
            .HasQueryFilter(e => e.CompanyId == CurrentCompanyId && !e.IsDeleted);

        modelBuilder.Entity<FijiPayroll.Domain.Entities.Company.PayrollComponent>()
            .HasQueryFilter(pc => pc.CompanyId == CurrentCompanyId && !pc.IsDeleted);

        modelBuilder.Entity<FijiPayroll.Domain.Entities.Company.MasterLookup>()
            .HasQueryFilter(ml => ml.CompanyId == CurrentCompanyId && !ml.IsDeleted);

        modelBuilder.Entity<FijiPayroll.Domain.Entities.Payroll.PayrollRun>()
            .HasQueryFilter(pr => pr.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<AuditLog>()
            .HasQueryFilter(al => al.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<EntityEvent>()
            .HasQueryFilter(ee => ee.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<ImportJob>()
            .HasQueryFilter(ij => ij.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<SearchIndex>()
            .HasQueryFilter(si => si.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<ApprovalWorkflow>()
            .HasQueryFilter(aw => aw.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<RuleSet>()
            .HasQueryFilter(rs => rs.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<CompanySetupState>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<CompanySetupTask>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<SetupExecutionRecord>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<SetupCheckpoint>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<CompanySetupAudit>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<CompanySeedVersion>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<FiscalCalendar>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<FiscalPeriod>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<PayrollFrequencyDefinition>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<PayPeriodSchedule>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<FnpfConfiguration>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<BankMaster>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<BankBranch>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<CompanyBankAccount>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<ApprovalConfig>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        // Compliance multi-tenant filters
        modelBuilder.Entity<CompliancePeriod>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<ComplianceBatch>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<PayrollLedger>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<ComplianceEvent>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<ApprovalMatrix>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<ComplianceJob>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<Notification>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<FRCSSubmission>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<FNPFSubmission>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<BankFile>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);
    }
}
