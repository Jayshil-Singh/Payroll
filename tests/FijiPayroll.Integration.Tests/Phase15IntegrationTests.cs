using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.PayrollPeriods.Commands.CreatePayrollPeriod;
using FijiPayroll.Application.Features.PayrollRuns.Commands.CreatePayrollRun;
using FijiPayroll.Application.Features.PayrollRuns.Commands.ProcessBatchPayroll;
using FijiPayroll.Application.Features.PayrollRuns.Commands.RollbackPayrollRun;
using FijiPayroll.Application.Features.PayrollRuns.Commands.FreezeLedger;
using FijiPayroll.Application.Features.PayrollRuns.Commands.ApprovePayrollRunWithSignature;
using FijiPayroll.Application.Services;
using FijiPayroll.Application.Services.EvidencePack;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Interceptors;
using FijiPayroll.Persistence.Repositories;
using FijiPayroll.Persistence.Seeders;
using FijiPayroll.Shared.Constants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FijiPayroll.Integration.Tests;

public sealed class Phase15IntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPayrollRunRepository _runRepository;
    private readonly ITaxBracketRepository _taxRepository;
    private readonly IEmployeeRepository _empRepository;
    private readonly IPayrollComponentRepository _compRepository;

    private readonly PayrollCalculationEngine _calculationEngine;
    private readonly PayrollValidationPipeline _validationPipeline;
    private readonly BatchProcessingCoordinator _coordinator;
    private readonly PayrollContextBuilder _contextBuilder;
    private readonly PayrollValidationService _validationService;
    private readonly PayrollPipelineService _pipeline;
    private readonly IDigitalSignatureService _digitalSignatureService;

    public Phase15IntegrationTests()
    {
        _currentUserAccessor = Substitute.For<ICurrentUserAccessor>();
        _currentUserAccessor.Username.Returns("phase15-test-user");

        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.Username.Returns("phase15-test-user");
        _currentUserService.HasPermission(Arg.Any<string>()).Returns(true);
        _currentUserService.HasCompanyAccess(Arg.Any<int>()).Returns(true);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentCompanyId().Returns(1);

        var interceptor = new AuditableEntityInterceptor(_currentUserAccessor);
        _context = new ApplicationDbContext(options, interceptor, tenantProvider);

        _compRepository = new PayrollComponentRepository(_context);
        _runRepository = new PayrollRunRepository(_context);
        _taxRepository = new TaxBracketRepository(_context);
        _empRepository = new EmployeeRepository(_context);
        var mockLookupRepo = Substitute.For<IMasterLookupRepository>();

        _unitOfWork = new UnitOfWork(
            _context,
            _compRepository,
            _runRepository,
            _empRepository,
            _taxRepository,
            mockLookupRepo,
            null!, null!, null!, null!, null!
        );

        _calculationEngine = new PayrollCalculationEngine();
        _validationPipeline = new PayrollValidationPipeline();
        _coordinator = new BatchProcessingCoordinator();
        _validationService = new PayrollValidationService();
        _contextBuilder = new PayrollContextBuilder(_unitOfWork);
        _pipeline = new PayrollPipelineService(
            _unitOfWork,
            _currentUserService,
            _contextBuilder,
            _calculationEngine,
            _validationService,
            _validationPipeline,
            _coordinator);
        _digitalSignatureService = Substitute.For<IDigitalSignatureService>();
        _digitalSignatureService.VerifySignature(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _digitalSignatureService.SignData(Arg.Any<string>()).Returns("mock-signature");
    }

    private async Task SeedDataAsync()
    {
        // Seed components
        var componentSeeder = new PayrollComponentSeeder(_context);
        await componentSeeder.SeedAsync(CancellationToken.None);

        // Seed tax brackets
        var taxSeeder = new TaxBracketSeeder(_context);
        await taxSeeder.SeedAsync(CancellationToken.None);

        // Seed employees
        var empSeeder = new EmployeeSeeder(_context);
        await empSeeder.SeedAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Verify_Simultaneous_Weekly_And_Monthly_Frequencies()
    {
        // Arrange
        await SeedDataAsync();

        // Let's create a Weekly payroll period & run, and a Monthly payroll period & run
        var periodHandler = new CreatePayrollPeriodCommandHandler(_unitOfWork, _currentUserService);
        var runHandler = new CreatePayrollRunCommandHandler(_unitOfWork, _currentUserService);

        var weeklyPeriodRes = await periodHandler.Handle(new CreatePayrollPeriodCommand(
            CompanyId: 1,
            PeriodCode: "WP-2026-FN12",
            Frequency: PayrollFrequencyType.Weekly,
            FiscalYear: 2026,
            FiscalMonth: 12,
            StartDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate: new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc),
            PaymentDate: new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc)
        ), CancellationToken.None);

        var monthlyPeriodRes = await periodHandler.Handle(new CreatePayrollPeriodCommand(
            CompanyId: 1,
            PeriodCode: "MP-2026-FN12",
            Frequency: PayrollFrequencyType.Monthly,
            FiscalYear: 2026,
            FiscalMonth: 6,
            StartDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            PaymentDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc)
        ), CancellationToken.None);

        weeklyPeriodRes.IsSuccess.Should().BeTrue();
        monthlyPeriodRes.IsSuccess.Should().BeTrue();

        var weeklyRunRes = await runHandler.Handle(new CreatePayrollRunCommand(
            CompanyId: 1,
            RunCode: "WR-FN12",
            PeriodName: "Week 24 Run",
            StartDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate: new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc),
            PaymentDate: new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc),
            Frequency: PayrollFrequencyType.Weekly,
            Description: "Weekly staff"
        ), CancellationToken.None);

        var monthlyRunRes = await runHandler.Handle(new CreatePayrollRunCommand(
            CompanyId: 1,
            RunCode: "MR-FN12",
            PeriodName: "Monthly staff staff",
            StartDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            PaymentDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Frequency: PayrollFrequencyType.Monthly,
            Description: "Monthly staff"
        ), CancellationToken.None);

        // Act & Assert
        weeklyRunRes.IsSuccess.Should().BeTrue();
        monthlyRunRes.IsSuccess.Should().BeTrue();

        var weeklyRun = await _context.PayrollRuns.FindAsync(weeklyRunRes.Value);
        var monthlyRun = await _context.PayrollRuns.FindAsync(monthlyRunRes.Value);

        weeklyRun.Should().NotBeNull();
        monthlyRun.Should().NotBeNull();
        weeklyRun!.Frequency.Should().Be(PayrollFrequencyType.Weekly);
        monthlyRun!.Frequency.Should().Be(PayrollFrequencyType.Monthly);
        
        // Assert they can co-exist simultaneously in Draft status
        weeklyRun.Status.Should().Be(PayrollRunStatus.Draft);
        monthlyRun.Status.Should().Be(PayrollRunStatus.Draft);
    }

    [Fact]
    public async Task Verify_DoubleEntry_Ledger_Balances_And_Writes_Correct_Lines()
    {
        // Arrange
        await SeedDataAsync();

        // Create and calculate a monthly run
        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "RUN-LE-01",
            periodName: "Ledger Explorer Period",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequencyType.Monthly,
            description: "Test run"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        var processHandler = new ProcessBatchPayrollCommandHandler(_pipeline);
        var calcResult = await processHandler.Handle(new ProcessBatchPayrollCommand(run.Id, Guid.NewGuid()), CancellationToken.None);
        calcResult.IsSuccess.Should().BeTrue();

        // Approve it first so we can freeze the ledger
        var approveHandler = new ApprovePayrollRunWithSignatureCommandHandler(_unitOfWork, _currentUserService, _digitalSignatureService);
        var reloadRun = await _unitOfWork.PayrollRuns.GetByIdAsync(run.Id);
        var approveResult = await approveHandler.Handle(new ApprovePayrollRunWithSignatureCommand(
            run.Id, "thumbprint", "mock-signature", "machine", "ip", "correlation"), CancellationToken.None);
        approveResult.IsSuccess.Should().BeTrue();

        var freezeHandler = new FreezeLedgerCommandHandler(_unitOfWork, _currentUserService);

        // Act
        var ledgerResult = await freezeHandler.Handle(new FreezeLedgerCommand(run.Id), CancellationToken.None);

        // Assert
        ledgerResult.IsSuccess.Should().BeTrue(ledgerResult.Error);
        var ledgerId = ledgerResult.Value;

        var ledger = await _context.Set<PayrollLedger>()
            .Include(l => l.Transactions)
            .Include(l => l.Employees)
            .FirstOrDefaultAsync(l => l.Id == ledgerId);

        ledger.Should().NotBeNull();
        ledger!.Transactions.Should().NotBeEmpty();

        // Debits must equal Credits
        decimal totalDebits = ledger.Transactions.Sum(t => t.Debit);
        decimal totalCredits = ledger.Transactions.Sum(t => t.Credit);
        totalDebits.Should().Be(totalCredits);

        // Verify Gross Expense, Net Payable, and tax liabilities exist
        ledger.Transactions.Any(t => t.AccountCode == "5000-SAL" && t.Debit > 0).Should().BeTrue();
        ledger.Transactions.Any(t => t.AccountCode == "2000-PAY" && t.Credit > 0).Should().BeTrue();
    }

    [Fact]
    public async Task Verify_Rollback_Recovery_Reverses_Ledger_And_Snapshots()
    {
        // Arrange
        await SeedDataAsync();

        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "RUN-RB-01",
            periodName: "Rollback Period",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequencyType.Monthly,
            description: "Test run"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        // Calculate run
        var processHandler = new ProcessBatchPayrollCommandHandler(_pipeline);
        await processHandler.Handle(new ProcessBatchPayrollCommand(run.Id, Guid.NewGuid()), CancellationToken.None);

        // Approve and Freeze ledger
        var approveHandler = new ApprovePayrollRunWithSignatureCommandHandler(_unitOfWork, _currentUserService, _digitalSignatureService);
        await approveHandler.Handle(new ApprovePayrollRunWithSignatureCommand(
            run.Id, "thumbprint", "mock-signature", "machine", "ip", "correlation"), CancellationToken.None);

        var freezeHandler = new FreezeLedgerCommandHandler(_unitOfWork, _currentUserService);
        await freezeHandler.Handle(new FreezeLedgerCommand(run.Id), CancellationToken.None);

        // Check snapshot & ledger exists
        var snapshotsBefore = await _unitOfWork.PayrollSnapshots.GetByRunIdAsync(run.Id);
        snapshotsBefore.Should().NotBeEmpty();

        var ledgerBefore = await _unitOfWork.Compliance.GetLedgerHeaderByRunIdAsync(run.Id);
        ledgerBefore.Should().NotBeNull();
        ledgerBefore!.IsReversed.Should().BeFalse();

        // Execute rollback
        var rollbackEngine = new RollbackEngine(_unitOfWork);
        var rollbackHandler = new RollbackPayrollRunCommandHandler(rollbackEngine, _currentUserService);

        // Act
        var rollbackResult = await rollbackHandler.Handle(new RollbackPayrollRunCommand(run.Id, "Reversal requested"), CancellationToken.None);

        // Assert
        rollbackResult.IsSuccess.Should().BeTrue();

        var runAfter = await _context.PayrollRuns.FindAsync(run.Id);
        runAfter!.Status.Should().Be(PayrollRunStatus.Draft);

        // Snapshots deleted
        var snapshotsAfter = await _unitOfWork.PayrollSnapshots.GetByRunIdAsync(run.Id);
        snapshotsAfter.Should().BeEmpty();

        // Ledger is marked reversed
        var ledgerAfter = await _unitOfWork.Compliance.GetLedgerHeaderByRunIdAsync(run.Id);
        ledgerAfter!.IsReversed.Should().BeTrue();

        // Reversal link created
        var reversals = await _context.Set<PayrollLedgerReversal>().ToListAsync();
        reversals.Should().Contain(r => r.OriginalLedgerId == ledgerBefore.Id);
    }

    [Fact]
    public async Task Verify_Replay_Verification_Stateless_Recreation()
    {
        // Arrange
        await SeedDataAsync();

        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "RUN-RP-01",
            periodName: "Replay Period",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequencyType.Monthly,
            description: "Test run"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        var processHandler = new ProcessBatchPayrollCommandHandler(_pipeline);
        await processHandler.Handle(new ProcessBatchPayrollCommand(run.Id, Guid.NewGuid()), CancellationToken.None);

        // Get latest snapshot
        var snapshot = await _unitOfWork.PayrollSnapshots.GetLatestByRunIdAsync(run.Id);
        snapshot.Should().NotBeNull();

        // Act
        var replayEngine = new PayrollReplayEngine(_calculationEngine);
        bool replaySuccess = replayEngine.Replay(
            snapshot!,
            out string calculatedHash,
            out decimal totalGross,
            out decimal totalPAYE,
            out decimal totalNet);

        // Assert
        replaySuccess.Should().BeTrue();
        calculatedHash.Should().Be(snapshot!.Hash);
        totalGross.Should().BeGreaterThan(0);
        totalNet.Should().BeGreaterThan(0);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
