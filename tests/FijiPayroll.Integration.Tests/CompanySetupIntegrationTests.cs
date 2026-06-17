using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.CompanySetup.Commands.CreateCompanyWizard;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Interceptors;
using FijiPayroll.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FijiPayroll.Integration.Tests;

public sealed class CompanySetupIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISetupWorkflowService _workflowService;

    public CompanySetupIntegrationTests()
    {
        _currentUserAccessor = Substitute.For<ICurrentUserAccessor>();
        _currentUserAccessor.Username.Returns("integration-test-user");

        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.Username.Returns("integration-test-user");
        _currentUserService.HasPermission(Arg.Any<string>()).Returns(true);
        _currentUserService.HasCompanyAccess(Arg.Any<int>()).Returns(true);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentCompanyId().Returns(1);

        var interceptor = new AuditableEntityInterceptor(_currentUserAccessor);
        _context = new ApplicationDbContext(options, interceptor, tenantProvider);

        var setupRepo = new SetupRepository(_context);
        _unitOfWork = new UnitOfWork(
            _context,
            Substitute.For<IPayrollComponentRepository>(),
            Substitute.For<IPayrollRunRepository>(),
            Substitute.For<IEmployeeRepository>(),
            Substitute.For<ITaxBracketRepository>(),
            Substitute.For<IMasterLookupRepository>(),
            Substitute.For<IImportJobRepository>(),
            Substitute.For<ISearchIndexRepository>(),
            Substitute.For<IApprovalWorkflowRepository>(),
            setupRepo);

        var correlationContext = Substitute.For<ICorrelationContext>();
        correlationContext.CorrelationId.Returns(Guid.NewGuid());

        _workflowService = new SetupWorkflowService(_unitOfWork, correlationContext);
    }

    [Fact]
    public async Task CompleteWizardFlow_ValidSteps_SucceedsAndLocksCompany()
    {
        // 1. Seed base company
        var company = Company.Create("Fiji Corp Ltd", "Key-Isolator");
        company.TradingName = "Fiji Corp";
        company.TIN = "123456789";
        company.FnpfEmployerNumber = "FNPF-001";
        await _context.Companies.AddAsync(company);

        // Seed other requirements so that validation passes
        // A. Fiscal Calendar
        var cal = FiscalCalendar.Create(1, 2026, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), CalendarType.Custom, "System");
        cal.AddPeriod(FiscalPeriod.Create(1, 1, 1, "January 2026", new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)));
        await _context.FiscalCalendars.AddAsync(cal);

        // B. Payroll Frequency
        var freq = PayrollFrequencyDefinition.Create(1, "Monthly Frequency", PayrollFrequencyType.Monthly, FrequencyCode.Monthly, "25th", 12, "Standard Monthly Payroll");
        var sched = PayPeriodSchedule.Create(1, 1, 1, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), new DateTime(2026, 1, 28), new DateTime(2026, 1, 30));
        freq.AssociateSchedule(sched);
        await _context.PayrollFrequencyDefinitions.AddAsync(freq);

        // C. Company Bank Account
        var bank = CompanyBankAccount.Create(1, "BSP Operating Account", 1, 1, BankAccountType.Operating, "AES:1:key:ciphertext", "hash", "4567");
        await _context.CompanyBankAccounts.AddAsync(bank);

        // D. Approval Config
        var approval = ApprovalConfig.Create(1, "finance-admin-user", null, 1, ApprovalRole.FinanceManager);
        await _context.ApprovalConfigs.AddAsync(approval);

        await _context.SaveChangesAsync();

        // 2. Initial Setup State Check
        var state = await _workflowService.GetOrCreateSetupStateAsync(1);
        state.CurrentStep.Should().Be(WizardStep.Welcome);
        state.IsCompleted.Should().BeFalse();

        // 3. Move through wizard steps
        var stepsToComplete = new[]
        {
            WizardStep.Welcome,
            WizardStep.CompanyDetails,
            WizardStep.FiscalCalendar,
            WizardStep.PayrollFrequency,
            WizardStep.BankConfiguration,
            WizardStep.Approvers
        };

        foreach (var step in stepsToComplete)
        {
            var result = await _workflowService.CompleteStepAsync(1, step, "integration-test-user");
            result.IsSuccess.Should().BeTrue();
        }

        // State should be at Validation step
        var updatedState = await _workflowService.GetOrCreateSetupStateAsync(1);
        updatedState.CurrentStep.Should().Be(WizardStep.Validation);

        // 4. Run dry-run validation check
        var validation = await _workflowService.ValidateSetupAsync(1);
        validation.IsValid.Should().BeTrue();
        validation.Errors.Should().BeEmpty();

        // 5. Execute finalization command
        var logger = Substitute.For<ILogger<CreateCompanyWizardCommandHandler>>();
        var handler = new CreateCompanyWizardCommandHandler(_unitOfWork, _workflowService, _currentUserService, logger);

        var executionId = Guid.NewGuid();
        var command = new CreateCompanyWizardCommand(1, executionId);
        var finalizeResult = await handler.Handle(command, CancellationToken.None);

        // Assert setup success
        finalizeResult.IsSuccess.Should().BeTrue();
        finalizeResult.Value.Should().Be(executionId);

        // Confirm DB state is updated
        var finalizedCompany = await _unitOfWork.Setup.GetCompanyByIdAsync(1, CancellationToken.None);
        finalizedCompany.Should().NotBeNull();
        finalizedCompany!.IsSetupComplete.Should().BeTrue();
        finalizedCompany.SetupCompletedUtc.Should().NotBeNull();

        // Check execution record exists and is Completed
        var record = await _unitOfWork.Setup.GetSetupExecutionRecordAsync(1, executionId, CancellationToken.None);
        record.Should().NotBeNull();
        record!.Status.Should().Be(ExecutionStatus.Completed);
        record.CompletedUtc.Should().NotBeNull();

        // Check domain event was raised
        finalizedCompany.DomainEvents.Should().ContainSingle(e => e is FijiPayroll.Domain.Events.CompanySetupCompletedEvent);
    }

    [Fact]
    public async Task FinalizeSetup_FailedValidation_RollsBackTransactionAndRecordsFailure()
    {
        // 1. Seed base company WITH INVALID DETAILS (Missing TIN & Fnpf number)
        var company = Company.Create("Fiji Corp Ltd", "Key-Isolator");
        await _context.Companies.AddAsync(company);
        await _context.SaveChangesAsync();

        // 2. Initial Setup State Check
        var state = await _workflowService.GetOrCreateSetupStateAsync(1);
        state.CurrentStep.Should().Be(WizardStep.Welcome);

        // Complete up to Validation
        await _workflowService.CompleteStepAsync(1, WizardStep.Welcome, "user");
        await _workflowService.CompleteStepAsync(1, WizardStep.CompanyDetails, "user");
        await _workflowService.CompleteStepAsync(1, WizardStep.FiscalCalendar, "user");
        await _workflowService.CompleteStepAsync(1, WizardStep.PayrollFrequency, "user");
        await _workflowService.CompleteStepAsync(1, WizardStep.BankConfiguration, "user");
        await _workflowService.CompleteStepAsync(1, WizardStep.Approvers, "user");

        // 3. Execute finalization command which will run validation
        var logger = Substitute.For<ILogger<CreateCompanyWizardCommandHandler>>();
        var handler = new CreateCompanyWizardCommandHandler(_unitOfWork, _workflowService, _currentUserService, logger);

        var executionId = Guid.NewGuid();
        var command = new CreateCompanyWizardCommand(1, executionId);
        var finalizeResult = await handler.Handle(command, CancellationToken.None);

        // Assert failure
        finalizeResult.IsSuccess.Should().BeFalse();
        finalizeResult.Errors.Should().NotBeEmpty();

        // Confirm company setup remains false
        var dbCompany = await _unitOfWork.Setup.GetCompanyByIdAsync(1, CancellationToken.None);
        dbCompany.Should().NotBeNull();
        dbCompany!.IsSetupComplete.Should().BeFalse();

        // Confirm execution record exists and is marked as Failed
        var record = await _unitOfWork.Setup.GetSetupExecutionRecordAsync(1, executionId, CancellationToken.None);
        record.Should().NotBeNull();
        record!.Status.Should().Be(ExecutionStatus.Failed);
        record.ErrorMessage.Should().Contain("Validation errors");
    }

    public void Dispose()
    {
        _context.Dispose();
        _unitOfWork.Dispose();
    }
}
