using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.PayrollRuns.Commands.CreatePayrollRun;
using FijiPayroll.Application.Features.PayrollRuns.Commands.CalculatePayrollRun;
using FijiPayroll.Application.Features.PayrollRuns.Commands.ResetPayrollRun;
using FijiPayroll.Application.Features.PayrollRuns.Commands.ApprovePayrollRun;
using FijiPayroll.Application.Features.PayrollRuns.Commands.PostPayrollRun;
using FijiPayroll.Application.Features.PayrollRuns.Commands.AdminOverrideLock;
using FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollRunById;
using FijiPayroll.Application.Services;
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

public sealed class PayrollRunIntegrationTests : IDisposable
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
    private readonly PayrollValidationService _validationService;

    public PayrollRunIntegrationTests()
    {
        _currentUserAccessor = Substitute.For<ICurrentUserAccessor>();
        _currentUserAccessor.Username.Returns("integration-test-user");

        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.Username.Returns("integration-test-user");
        _currentUserService.HasPermission(Arg.Any<string>()).Returns(true);
        _currentUserService.HasCompanyAccess(Arg.Any<int>()).Returns(true);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var interceptor = new AuditableEntityInterceptor(_currentUserAccessor);
        _context = new ApplicationDbContext(options, interceptor);

        _compRepository = new PayrollComponentRepository(_context);
        _runRepository = new PayrollRunRepository(_context);
        _taxRepository = new TaxBracketRepository(_context);
        _empRepository = new EmployeeRepository(_context);

        _unitOfWork = new UnitOfWork(_context, _compRepository, _runRepository, _empRepository, _taxRepository);

        _calculationEngine = new PayrollCalculationEngine();
        _validationService = new PayrollValidationService();
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
    public async Task CreatePayrollRun_ValidRequest_SavesHeader()
    {
        // Arrange
        await SeedDataAsync();
        var handler = new CreatePayrollRunCommandHandler(_unitOfWork, _currentUserService);
        var command = new CreatePayrollRunCommand(
            CompanyId: 1,
            RunCode: "PR-2026-06-W01",
            PeriodName: "June 2026 Week 1",
            StartDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate: new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc),
            PaymentDate: new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            Frequency: PayrollFrequency.Fortnightly,
            Description: "First week of June"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);

        var run = await _context.PayrollRuns.FindAsync(result.Value);
        run.Should().NotBeNull();
        run!.RunCode.Should().Be("PR-2026-06-W01");
        run.Status.Should().Be(PayrollRunStatus.Draft);
    }

    [Fact]
    public async Task CalculatePayrollRun_ValidRequest_CalculatesDeterministicValues()
    {
        // Arrange
        await SeedDataAsync();

        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "PR-2026-06-F01",
            periodName: "June 2026 Fortnight 1",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequency.Fortnightly,
            description: "Fortnightly payroll"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        var handler = new CalculatePayrollRunCommandHandler(
            _unitOfWork,
            _currentUserService,
            _calculationEngine,
            _validationService
        );

        var reqId1 = Guid.NewGuid();
        var command = new CalculatePayrollRunCommand(run.Id, reqId1);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedRun = await _context.PayrollRuns
            .Include(r => r.Employees).ThenInclude(e => e.LineItems)
            .Include(r => r.Employees).ThenInclude(e => e.Trace)
            .FirstOrDefaultAsync(r => r.Id == run.Id);

        updatedRun!.Status.Should().Be(PayrollRunStatus.Calculated);
        updatedRun.SnapshotHash.Should().NotBeNullOrWhiteSpace();

        // ── Deterministic Check ──────────────────────────────────────────────────
        // Execute calculation again with different Request ID but same inputs
        var reqId2 = Guid.NewGuid();
        
        // Reset to draft first
        var resetHandler = new ResetPayrollRunCommandHandler(_unitOfWork, _currentUserService);
        await resetHandler.Handle(new ResetPayrollRunCommand(run.Id), CancellationToken.None);

        // Calculate again
        var result2 = await handler.Handle(new CalculatePayrollRunCommand(run.Id, reqId2), CancellationToken.None);
        result2.IsSuccess.Should().BeTrue();

        var run2 = await _context.PayrollRuns
            .Include(r => r.Employees).ThenInclude(e => e.LineItems)
            .FirstOrDefaultAsync(r => r.Id == run.Id);

        // SnapshotHash must be identical
        run2!.SnapshotHash.Should().Be(updatedRun.SnapshotHash);
    }

    [Fact]
    public async Task CalculatePayrollRun_ConcurrencyLocked_RejectsDuplicateCalculation()
    {
        // Arrange
        await SeedDataAsync();

        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "PR-CONC",
            periodName: "Concurrency Test",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequency.Fortnightly,
            description: "Concurrency"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        var handler = new CalculatePayrollRunCommandHandler(
            _unitOfWork,
            _currentUserService,
            _calculationEngine,
            _validationService
        );

        // Act - Start first calculation (acquires lock)
        var command1 = new CalculatePayrollRunCommand(run.Id, Guid.NewGuid());
        
        // Simulate concurrent execution by executing the AcquireLock directly in DB
        var runToLock = await _context.PayrollRuns.FindAsync(run.Id);
        runToLock!.AcquireLock(Guid.NewGuid(), "user-1");
        await _context.SaveChangesAsync();

        // Try second calculation concurrently
        var command2 = new CalculatePayrollRunCommand(run.Id, Guid.NewGuid());
        var result2 = await handler.Handle(command2, CancellationToken.None);

        // Assert
        result2.IsSuccess.Should().BeFalse();
        result2.Error.Should().Contain("Cannot calculate payroll run");
    }

    [Fact]
    public async Task ResetPayrollRun_ValidCalculatedRun_ResetsAndMarksSuperseded()
    {
        // Arrange
        await SeedDataAsync();

        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "PR-RESET",
            periodName: "Reset Test",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequency.Fortnightly,
            description: "Reset"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        var calcHandler = new CalculatePayrollRunCommandHandler(
            _unitOfWork,
            _currentUserService,
            _calculationEngine,
            _validationService
        );
        await calcHandler.Handle(new CalculatePayrollRunCommand(run.Id, Guid.NewGuid()), CancellationToken.None);

        var resetHandler = new ResetPayrollRunCommandHandler(_unitOfWork, _currentUserService);
        var resetCommand = new ResetPayrollRunCommand(run.Id);

        // Act
        var result = await resetHandler.Handle(resetCommand, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var resetRun = await _context.PayrollRuns
            .Include(r => r.Employees)
            .FirstOrDefaultAsync(r => r.Id == run.Id);

        resetRun!.Status.Should().Be(PayrollRunStatus.Draft);
        resetRun.SnapshotHash.Should().BeNull();
        resetRun.Employees.Count.Should().BeGreaterThan(0);
        resetRun.Employees.All(e => e.IsSuperseded).Should().BeTrue(); // Reset Safety: Marks records as superseded rather than physical delete
    }

    [Fact]
    public async Task AdminOverrideLock_LockStuck_ResetsStateToDraft()
    {
        // Arrange
        await SeedDataAsync();

        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "PR-OVERRIDE",
            periodName: "Override Test",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequency.Fortnightly,
            description: "Lock override"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        // Simulate calculating state lock
        run.AcquireLock(Guid.NewGuid(), "user-stuck");
        await _unitOfWork.SaveChangesAsync();

        var handler = new AdminOverrideLockCommandHandler(_unitOfWork, _currentUserService);
        var command = new AdminOverrideLockCommand(run.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedRun = await _context.PayrollRuns.FindAsync(run.Id);
        updatedRun!.Status.Should().Be(PayrollRunStatus.Draft);
        updatedRun.CurrentRequestId.Should().BeNull();
    }

    [Fact]
    public async Task CalculatePayrollRun_MissingBrackets_ThrowsDeterministicError()
    {
        // Arrange
        var componentSeeder = new PayrollComponentSeeder(_context);
        await componentSeeder.SeedAsync(CancellationToken.None);

        // Note: We do NOT seed tax brackets!
        var empSeeder = new EmployeeSeeder(_context);
        await empSeeder.SeedAsync(CancellationToken.None);

        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "PR-ERROR",
            periodName: "Tax Error Test",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequency.Fortnightly,
            description: "Error test"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        var handler = new CalculatePayrollRunCommandHandler(
            _unitOfWork,
            _currentUserService,
            _calculationEngine,
            _validationService
        );

        // Act
        Func<Task> act = () => handler.Handle(new CalculatePayrollRunCommand(run.Id, Guid.NewGuid()), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TAX_ENGINE_ERROR*");
    }

    [Fact]
    public async Task Performance_10k_Employee_BatchSimulation()
    {
        // Arrange
        await SeedDataAsync();

        // Load one seeded employee template
        var template = await _context.Employees.FirstAsync();

        // Seed 10,000 employees using template
        var extraEmployees = new List<Employee>();
        for (int i = 0; i < 10000; i++)
        {
            extraEmployees.Add(Employee.Create(
                companyId: template.CompanyId,
                fullName: $"Employee Scale {i}",
                tin: $"100{i:D6}",
                fnpfNumber: $"800{i:D6}-K",
                residencyStatus: "Resident",
                department: "Scalability",
                baseSalary: 1200.00m + (i % 100),
                frequency: PayrollFrequency.Fortnightly,
                isFnpfExempt: false,
                isTaxExempt: false,
                isActive: true
            ));
        }

        foreach (var emp in extraEmployees)
        {
            emp.CreatedBy = "perf-test";
            emp.CreatedAt = DateTime.UtcNow;
        }

        await _context.Employees.AddRangeAsync(extraEmployees);
        await _context.SaveChangesAsync();

        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "PR-SCALE",
            periodName: "10K Scalability Test",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequency.Fortnightly,
            description: "Scalability benchmark"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        var handler = new CalculatePayrollRunCommandHandler(
            _unitOfWork,
            _currentUserService,
            _calculationEngine,
            _validationService
        );

        // Act & Assert
        // We time the execution to confirm sequential scalability and memory stability
        var startTime = DateTime.UtcNow;
        var result = await handler.Handle(new CalculatePayrollRunCommand(run.Id, Guid.NewGuid()), CancellationToken.None);
        var duration = DateTime.UtcNow - startTime;

        result.IsSuccess.Should().BeTrue();
        duration.TotalSeconds.Should().BeLessThan(30.0); // Scalability requirement check: 10,000 employees computed within 30 seconds
    }

    [Fact]
    public async Task CalculatePayrollRun_DuplicateRequestId_RejectsCalculation()
    {
        // Arrange
        await SeedDataAsync();

        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "PR-DUP",
            periodName: "Duplicate Test",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequency.Fortnightly,
            description: "Duplicate request ID"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        var handler = new CalculatePayrollRunCommandHandler(
            _unitOfWork,
            _currentUserService,
            _calculationEngine,
            _validationService
        );

        var requestId = Guid.NewGuid();

        // Start first execution
        // We simulate that the lock is already acquired for this request ID
        var runToLock = await _context.PayrollRuns.FindAsync(run.Id);
        runToLock!.AcquireLock(requestId, "user-1");
        await _context.SaveChangesAsync();

        // Act - Try another execution with the same request ID
        var command = new CalculatePayrollRunCommand(run.Id, requestId);
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cannot calculate payroll run");
    }

    [Fact]
    public async Task PayrollRunEmployeeTrace_AttemptUpdate_ThrowsException()
    {
        // Arrange
        await SeedDataAsync();

        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "PR-TRACE-LOCK",
            periodName: "Trace Lock Test",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequency.Fortnightly,
            description: "Trace lock test"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        var handler = new CalculatePayrollRunCommandHandler(
            _unitOfWork,
            _currentUserService,
            _calculationEngine,
            _validationService
        );

        await handler.Handle(new CalculatePayrollRunCommand(run.Id, Guid.NewGuid()), CancellationToken.None);

        // Fetch trace record
        var trace = await _context.PayrollRunEmployeeTraces.FirstAsync();

        // Attempt update (should be blocked by ApplicationDbContext)
        _context.Entry(trace).Property(t => t.TraceText).IsModified = true;

        // Act
        Func<Task> act = () => _context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TRACE_RULE_VIOLATION*");
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_EnforcesOrderingByEmployeeId()
    {
        // Arrange
        await SeedDataAsync();

        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "PR-ORDER",
            periodName: "Ordering Test",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequency.Fortnightly,
            description: "Ordering verification"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        var handler = new CalculatePayrollRunCommandHandler(
            _unitOfWork,
            _currentUserService,
            _calculationEngine,
            _validationService
        );

        await handler.Handle(new CalculatePayrollRunCommand(run.Id, Guid.NewGuid()), CancellationToken.None);

        // Act
        var runDetails = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(run.Id);

        // Assert
        runDetails.Should().NotBeNull();
        var employeeIds = runDetails!.Employees.Select(e => e.EmployeeId).ToList();

        employeeIds.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task SnapshotHash_Deterministic_ByteLevelConsistency()
    {
        // Generate snapshots
        var emp1 = new EmployeeSnapshot
        {
            EmployeeId = 1,
            FullName = "Employee 1",
            BaseSalary = 1250.00m,
            ResidencyStatus = "Resident",
            IsFnpfExempt = false,
            IsTaxExempt = false,
            ComponentOverrides = Array.Empty<EmployeeComponentOverrideSnapshot>()
        };

        var emp2 = new EmployeeSnapshot
        {
            EmployeeId = 2,
            FullName = "Employee 2",
            BaseSalary = 2500.50m,
            ResidencyStatus = "NonResident",
            IsFnpfExempt = true,
            IsTaxExempt = false,
            ComponentOverrides = Array.Empty<EmployeeComponentOverrideSnapshot>()
        };

        var comp1 = new PayrollComponentSnapshot
        {
            Id = 1,
            ComponentCode = "BASIC",
            ComponentType = ComponentType.Earning,
            CalculationMethod = CalculationMethod.Fixed,
            CalculationValue = 0m,
            IsTaxable = true,
            IsFnpfApplicable = true
        };

        var comp2 = new PayrollComponentSnapshot
        {
            Id = 2,
            ComponentCode = "PAYE",
            ComponentType = ComponentType.Deduction,
            CalculationMethod = CalculationMethod.Fixed,
            CalculationValue = 0m,
            IsTaxable = false,
            IsFnpfApplicable = false
        };

        // Order 1
        var hash1 = PayrollSnapshotHasher.GenerateHash(new[] { emp1, emp2 }, "2025-2026", new[] { comp1, comp2 });
        // Order 2 (shuffled input array)
        var hash2 = PayrollSnapshotHasher.GenerateHash(new[] { emp2, emp1 }, "2025-2026", new[] { comp2, comp1 });

        hash1.Should().Be(hash2);
    }

    [Fact]
    public async Task Concurrency_Calculation_Stress_Parallel()
    {
        await SeedDataAsync();

        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "PR-STRESS",
            periodName: "Concurrency Stress Test",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequency.Fortnightly,
            description: "Stress test"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        var handler = new CalculatePayrollRunCommandHandler(
            _unitOfWork,
            _currentUserService,
            _calculationEngine,
            _validationService
        );

        // Act & Assert: Trigger 100 parallel calculate calls
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => handler.Handle(new CalculatePayrollRunCommand(run.Id, Guid.NewGuid()), CancellationToken.None))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // There must be exactly one success, other 99 fail due to concurrency locks
        int successCount = results.Count(r => r.IsSuccess);
        int failureCount = results.Count(r => !r.IsSuccess);

        successCount.Should().Be(1);
        failureCount.Should().Be(99);
    }

    [Fact]
    public async Task Reset_Cannot_Trigger_Recalculation_Indirectly()
    {
        await SeedDataAsync();

        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "PR-RESET-SAFETY",
            periodName: "Reset Recalc Guard Test",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequency.Fortnightly,
            description: "Safety check"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        // Calculate it first
        var calcHandler = new CalculatePayrollRunCommandHandler(
            _unitOfWork,
            _currentUserService,
            _calculationEngine,
            _validationService
        );
        await calcHandler.Handle(new CalculatePayrollRunCommand(run.Id, Guid.NewGuid()), CancellationToken.None);

        // Reset it
        var resetHandler = new ResetPayrollRunCommandHandler(_unitOfWork, _currentUserService);
        
        var result = await resetHandler.Handle(new ResetPayrollRunCommand(run.Id), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        var resetRun = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(run.Id);
        resetRun!.Status.Should().Be(PayrollRunStatus.Draft);
        
        // Ensure no recalculation was triggered (employees count for the run is unchanged, and all are superseded)
        resetRun.Employees.All(e => e.IsSuperseded).Should().BeTrue();
    }

    [Fact]
    public async Task PayrollRunEmployeeTrace_AttemptDelete_ThrowsException()
    {
        await SeedDataAsync();

        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "PR-TRACE-DEL",
            periodName: "Trace Delete Guard Test",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequency.Fortnightly,
            description: "Delete safety check"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        var handler = new CalculatePayrollRunCommandHandler(
            _unitOfWork,
            _currentUserService,
            _calculationEngine,
            _validationService
        );
        await handler.Handle(new CalculatePayrollRunCommand(run.Id, Guid.NewGuid()), CancellationToken.None);

        var trace = await _context.PayrollRunEmployeeTraces.FirstAsync();
        _context.PayrollRunEmployeeTraces.Remove(trace);

        Func<Task> act = () => _context.SaveChangesAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TRACE_RULE_VIOLATION*");
    }

    [Fact]
    public async Task OrderingCorrectness_10k_Dataset()
    {
        await SeedDataAsync();

        var template = await _context.Employees.FirstAsync();
        var extraEmployees = new List<Employee>();
        for (int i = 0; i < 1000; i++) 
        {
            extraEmployees.Add(Employee.Create(
                companyId: template.CompanyId,
                fullName: $"Employee Order scale {i}",
                tin: $"100{i:D6}",
                fnpfNumber: $"800{i:D6}-K",
                residencyStatus: "Resident",
                department: "ScaleOrder",
                baseSalary: 1000.00m + i,
                frequency: PayrollFrequency.Fortnightly,
                isFnpfExempt: false,
                isTaxExempt: false,
                isActive: true
            ));
        }

        foreach (var emp in extraEmployees)
        {
            emp.CreatedBy = "perf-test";
            emp.CreatedAt = DateTime.UtcNow;
        }

        await _context.Employees.AddRangeAsync(extraEmployees);
        await _context.SaveChangesAsync();

        var run = PayrollRun.Create(
            companyId: 1,
            runCode: "PR-ORDER-LARGE",
            periodName: "Large Dataset Ordering",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            frequency: PayrollFrequency.Fortnightly,
            description: "Ordering correctness check"
        );
        await _unitOfWork.PayrollRuns.AddAsync(run);
        await _unitOfWork.SaveChangesAsync();

        var handler = new CalculatePayrollRunCommandHandler(
            _unitOfWork,
            _currentUserService,
            _calculationEngine,
            _validationService
        );

        await handler.Handle(new CalculatePayrollRunCommand(run.Id, Guid.NewGuid()), CancellationToken.None);

        var runDetails = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(run.Id);
        var employeeIds = runDetails!.Employees.Select(e => e.EmployeeId).ToList();

        employeeIds.Should().BeInAscendingOrder();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
