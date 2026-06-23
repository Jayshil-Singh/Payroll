using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Entities.Leave;
using FijiPayroll.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Persistence.Converters;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

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
    public int CurrentCompanyId =>
        _tenantProvider?.GetCurrentCompanyId()
        ?? throw new TenantContextException(
            "Tenant provider is required. Data access without an established company context is not permitted.");

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

    /// <summary>Gets or sets the user accounts DbSet.</summary>
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    /// <summary>Gets or sets the user password histories DbSet.</summary>
    public DbSet<UserPasswordHistory> UserPasswordHistories => Set<UserPasswordHistory>();

    /// <summary>Gets or sets the user roles DbSet.</summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <summary>Gets or sets the user permissions DbSet.</summary>
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

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
    public DbSet<PayrollLedgerEmployee> PayrollLedgerEmployees => Set<PayrollLedgerEmployee>();
    public DbSet<PayrollLedgerComponent> PayrollLedgerComponents => Set<PayrollLedgerComponent>();
    public DbSet<PayrollLedgerTransaction> PayrollLedgerTransactions => Set<PayrollLedgerTransaction>();
    public DbSet<PayrollLedgerReversal> PayrollLedgerReversals => Set<PayrollLedgerReversal>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<PayrollGroup> PayrollGroups => Set<PayrollGroup>();
    public DbSet<PayrollAdjustment> PayrollAdjustments => Set<PayrollAdjustment>();
    public DbSet<RetroactiveAdjustment> RetroactiveAdjustments => Set<RetroactiveAdjustment>();
    public DbSet<PayrollSnapshot> PayrollSnapshots => Set<PayrollSnapshot>();
    public DbSet<PayrollExceptionQueue> PayrollExceptionQueues => Set<PayrollExceptionQueue>();
    public DbSet<PayrollRunHistory> PayrollRunHistories => Set<PayrollRunHistory>();
    public DbSet<BackgroundJob> BackgroundJobs => Set<BackgroundJob>();
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

    // ── Leave Module DbSets ─────────────────────────────────────────────────
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveTransaction> LeaveTransactions => Set<LeaveTransaction>();
    public DbSet<LeaveAccrualPolicy> LeaveAccrualPolicies => Set<LeaveAccrualPolicy>();

    // ── Loan Module DbSets ──────────────────────────────────────────────────
    public DbSet<Loan> StaffLoans => Set<Loan>();
    public DbSet<LoanRepayment> StaffLoanRepayments => Set<LoanRepayment>();


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

        modelBuilder.Entity<UserAccount>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<ImportSession>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<ImportSessionRow>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<WorkflowStepLog>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<ComplianceSnapshot>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<ComplianceAmendment>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

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

        modelBuilder.Entity<PayrollLedgerEmployee>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<PayrollLedgerTransaction>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<PayrollLedgerReversal>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<PayrollPeriod>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<PayrollGroup>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<PayrollAdjustment>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<RetroactiveAdjustment>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<PayrollSnapshot>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<PayrollExceptionQueue>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<PayrollRunHistory>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<BackgroundJob>()
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

        // Leave Module RLS Query Filters
        modelBuilder.Entity<LeaveType>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<LeaveBalance>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<LeaveRequest>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<LeaveTransaction>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<LeaveAccrualPolicy>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        // Loan Module RLS Query Filters
        modelBuilder.Entity<Loan>()
            .HasQueryFilter(x => x.CompanyId == CurrentCompanyId && !x.IsDeleted);

        modelBuilder.Entity<PayrollRunEmployee>()
            .HasQueryFilter(e => e.PayrollRun != null && e.PayrollRun.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<PayrollRunLineItem>()
            .HasQueryFilter(li => li.PayrollRunEmployee != null
                && li.PayrollRunEmployee.PayrollRun != null
                && li.PayrollRunEmployee.PayrollRun.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<PayrollRunStateHistory>()
            .HasQueryFilter(h => h.PayrollRun != null && h.PayrollRun.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<PayrollRunEmployeeTrace>()
            .HasQueryFilter(t => t.PayrollRunEmployee != null
                && t.PayrollRunEmployee.PayrollRun != null
                && t.PayrollRunEmployee.PayrollRun.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<PayrollLedgerComponent>()
            .HasQueryFilter(c => c.PayrollLedgerEmployee != null && c.PayrollLedgerEmployee.CompanyId == CurrentCompanyId);
    }

    /// <summary>
    /// Migrates all PLAINTEXT-prefixed encrypted column values to AES-256 using per-tenant security keys.
    /// This method bypasses EF query filters to scan ALL companies and their associated PII records.
    /// Safe to call multiple times — records already encrypted with AES256 are skipped.
    /// </summary>
    public async Task MigratePlaintextToAesAsync()
    {
        // Step 1: Load all companies (bypass tenant filter)
        var companies = await Companies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync();

        if (companies.Count == 0) return;

        foreach (var company in companies)
        {
            string tenantKey = $"KEY_COMP_{company.Id}";
            int migrated = 0;

            // ── Migrate Employee PII (Tin, FnpfNumber) ─────────────────────
            var employees = await Database.SqlQueryRaw<EncryptedEmployeeRow>(
                "SELECT [Id], [Tin], [FnpfNumber] FROM [company].[Employees] WHERE [CompanyId] = {0} AND [IsDeleted] = 0", company.Id)
                .ToListAsync();

            foreach (var emp in employees)
            {
                bool tinNeedsMigration = IsPlaintextEncrypted(emp.Tin);
                bool fnpfNeedsMigration = IsPlaintextEncrypted(emp.FnpfNumber);

                if (!tinNeedsMigration && !fnpfNeedsMigration) continue;

                string newTin = tinNeedsMigration ? ReEncryptValue(emp.Tin, tenantKey) : emp.Tin;
                string newFnpf = fnpfNeedsMigration ? ReEncryptValue(emp.FnpfNumber, tenantKey) : emp.FnpfNumber;

                await Database.ExecuteSqlRawAsync(
                    "UPDATE [company].[Employees] SET [Tin] = {0}, [FnpfNumber] = {1} WHERE [Id] = {2}",
                    newTin, newFnpf, emp.Id);
                migrated++;
            }

            // ── Migrate EmployeePaymentMethods (BankAccountNumber, BankSortCode) ──
            var paymentMethods = await Database.SqlQueryRaw<EncryptedPaymentMethodRow>(
                "SELECT [Id], [BankAccountNumber], [BankSortCode] FROM [company].[EmployeePaymentMethods] " +
                "WHERE [EmployeeId] IN (SELECT [Id] FROM [company].[Employees] WHERE [CompanyId] = {0} AND [IsDeleted] = 0)", company.Id)
                .ToListAsync();

            foreach (var pm in paymentMethods)
            {
                bool bankAcctNeedsMigration = IsPlaintextEncrypted(pm.BankAccountNumber);
                bool sortCodeNeedsMigration = IsPlaintextEncrypted(pm.BankSortCode);

                if (!bankAcctNeedsMigration && !sortCodeNeedsMigration) continue;

                string? newBankAcct = bankAcctNeedsMigration ? ReEncryptValue(pm.BankAccountNumber!, tenantKey) : pm.BankAccountNumber;
                string? newSortCode = sortCodeNeedsMigration ? ReEncryptValue(pm.BankSortCode!, tenantKey) : pm.BankSortCode;

                await Database.ExecuteSqlRawAsync(
                    "UPDATE [company].[EmployeePaymentMethods] SET [BankAccountNumber] = {0}, [BankSortCode] = {1} WHERE [Id] = {2}",
                    (object?)newBankAcct ?? DBNull.Value, (object?)newSortCode ?? DBNull.Value, pm.Id);
                migrated++;
            }

            // ── Migrate CompanyBankAccounts (EncryptedAccountNumber) ─────────
            var bankAccounts = await Database.SqlQueryRaw<EncryptedBankAccountRow>(
                "SELECT [Id], [EncryptedAccountNumber] FROM [company].[CompanyBankAccounts] WHERE [CompanyId] = {0} AND [IsDeleted] = 0", company.Id)
                .ToListAsync();

            foreach (var ba in bankAccounts)
            {
                if (!IsPlaintextEncrypted(ba.EncryptedAccountNumber)) continue;

                string newAcctNum = ReEncryptValue(ba.EncryptedAccountNumber, tenantKey);

                await Database.ExecuteSqlRawAsync(
                    "UPDATE [company].[CompanyBankAccounts] SET [EncryptedAccountNumber] = {0} WHERE [Id] = {1}",
                    newAcctNum, ba.Id);
                migrated++;
            }
        }

        // Reset the key context to avoid leaking the last company's key
        TenantEncryptionValueConverter.CurrentKey = null;
    }

    /// <summary>
    /// Checks if a stored value uses the PLAINTEXT fallback encoding from TenantEncryptionValueConverter.
    /// </summary>
    private static bool IsPlaintextEncrypted(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        return value.StartsWith("PLAINTEXT:", StringComparison.Ordinal);
    }

    /// <summary>
    /// Decrypts a PLAINTEXT-encoded value and re-encrypts it with AES-256 using the specified tenant key.
    /// </summary>
    private static string ReEncryptValue(string cipherText, string tenantKey)
    {
        // Decrypt the plaintext-encoded value (no key needed for PLAINTEXT format)
        string plainText = TenantEncryptionValueConverter.Decrypt(cipherText);

        // Set the tenant key and re-encrypt with AES-256
        TenantEncryptionValueConverter.CurrentKey = tenantKey;
        return TenantEncryptionValueConverter.Encrypt(plainText);
    }

    // ── Projection DTOs for raw SQL queries ─────────────────────────────────

    private sealed class EncryptedEmployeeRow
    {
        public int Id { get; set; }
        public string Tin { get; set; } = string.Empty;
        public string FnpfNumber { get; set; } = string.Empty;
    }

    private sealed class EncryptedPaymentMethodRow
    {
        public int Id { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankSortCode { get; set; }
    }

    private sealed class EncryptedBankAccountRow
    {
        public int Id { get; set; }
        public string EncryptedAccountNumber { get; set; } = string.Empty;
    }
}
