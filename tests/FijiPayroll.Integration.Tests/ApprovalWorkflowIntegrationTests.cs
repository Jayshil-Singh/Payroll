using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Events;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Interceptors;
using FijiPayroll.Persistence.Repositories;
using FijiPayroll.Persistence.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FijiPayroll.Integration.Tests;

public sealed class ApprovalWorkflowIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IEventBus _eventBus;
    private readonly IApprovalEngine _approvalEngine;

    public ApprovalWorkflowIntegrationTests()
    {
        var services = new ServiceCollection();

        // Database setup using InMemory
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));

        // Mocks & Providers
        _tenantProvider = Substitute.For<ITenantProvider>();
        _tenantProvider.GetCurrentCompanyId().Returns(1);
        services.AddSingleton(_tenantProvider);

        var currentUserAccessor = Substitute.For<ICurrentUserAccessor>();
        currentUserAccessor.Username.Returns("workflow-test-user");
        services.AddSingleton(currentUserAccessor);

        var auditableInterceptor = new AuditableEntityInterceptor(currentUserAccessor);
        services.AddSingleton(auditableInterceptor);

        _eventBus = Substitute.For<IEventBus>();
        services.AddSingleton(_eventBus);

        // Register repositories
        services.AddScoped<IPayrollComponentRepository>(sp => Substitute.For<IPayrollComponentRepository>());
        services.AddScoped<IPayrollRunRepository>(sp => Substitute.For<IPayrollRunRepository>());
        services.AddScoped<IEmployeeRepository>(sp => Substitute.For<IEmployeeRepository>());
        services.AddScoped<ITaxBracketRepository>(sp => Substitute.For<ITaxBracketRepository>());
        services.AddScoped<IMasterLookupRepository>(sp => Substitute.For<IMasterLookupRepository>());
        services.AddScoped<IImportJobRepository>(sp => Substitute.For<IImportJobRepository>());
        services.AddScoped<ISearchIndexRepository>(sp => Substitute.For<ISearchIndexRepository>());
        services.AddScoped<IApprovalWorkflowRepository, ApprovalWorkflowRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Real ApprovalEngine
        services.AddScoped<IApprovalEngine, ApprovalEngine>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _approvalEngine = _serviceProvider.GetRequiredService<IApprovalEngine>();
    }

    [Fact]
    public async Task SubmitAsync_ShouldCreateWorkflow_WithCorrectInitialStateAndAuditLog()
    {
        // Arrange
        string entityType = "Employee";
        string entityId = "EMP-999";
        string requester = "initiator-user";
        string comment = "Initial creation request.";

        // Act
        var result = await _approvalEngine.SubmitAsync(entityType, entityId, requester, comment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CurrentState.Should().Be(WorkflowState.Submitted.ToString());

        var workflow = await _context.ApprovalWorkflows
            .Include(w => w.Steps)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.WorkflowId == result.WorkflowId);

        workflow.Should().NotBeNull();
        workflow!.EntityType.Should().Be(entityType);
        workflow.EntityId.Should().Be(entityId);
        workflow.CurrentState.Should().Be(WorkflowState.Submitted);
        workflow.RequestedBy.Should().Be(requester);
        workflow.CompanyId.Should().Be(1);

        // Verify audit step recorded
        workflow.Steps.Should().HaveCount(1);
        var initialStep = workflow.Steps.First();
        initialStep.FromState.Should().Be(WorkflowState.Draft.ToString());
        initialStep.ToState.Should().Be(WorkflowState.Submitted.ToString());
        initialStep.TransitionedBy.Should().Be(requester);

        // Verify Event Bus published WorkflowStateChangedEvent
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<WorkflowStateChangedEvent>(e => 
                e.WorkflowId == result.WorkflowId &&
                e.EntityType == entityType &&
                e.EntityId == entityId &&
                e.OldState == WorkflowState.Draft &&
                e.NewState == WorkflowState.Submitted &&
                e.TransitionedBy == requester &&
                e.Comments == comment),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitAsync_ShouldFail_IfActiveWorkflowAlreadyExists()
    {
        // Arrange
        string entityType = "PayrollRun";
        string entityId = "RUN-100";
        string requester = "initiator-user";

        // Submit once
        var firstResult = await _approvalEngine.SubmitAsync(entityType, entityId, requester, "First submit");
        firstResult.IsSuccess.Should().BeTrue();

        // Act
        var secondResult = await _approvalEngine.SubmitAsync(entityType, entityId, requester, "Second submit duplicate");

        // Assert
        secondResult.IsSuccess.Should().BeFalse();
        secondResult.Errors.Should().Contain("A workflow request is already active for this entity.");
        secondResult.WorkflowId.Should().Be(firstResult.WorkflowId);
    }

    [Fact]
    public async Task ApproveAsync_ShouldTransitionStateToApproved_AndRecordAuditLog()
    {
        // Arrange
        string entityType = "Employee";
        string entityId = "EMP-001";
        string requester = "initiator-user";
        string approver = "approver-user";
        string approveComment = "Approved for payroll processing.";

        var submitResult = await _approvalEngine.SubmitAsync(entityType, entityId, requester, "Submit");
        submitResult.IsSuccess.Should().BeTrue();

        // Clear mock calls to focus only on approval publication
        _eventBus.ClearReceivedCalls();

        // Act
        var approveResult = await _approvalEngine.ApproveAsync(submitResult.WorkflowId, approver, approveComment);

        // Assert
        approveResult.IsSuccess.Should().BeTrue();
        approveResult.CurrentState.Should().Be(WorkflowState.Approved.ToString());

        var workflow = await _context.ApprovalWorkflows
            .Include(w => w.Steps)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.WorkflowId == submitResult.WorkflowId);

        workflow.Should().NotBeNull();
        workflow!.CurrentState.Should().Be(WorkflowState.Approved);

        // Verify two steps: Submit and Approve
        workflow.Steps.Should().HaveCount(2);
        var secondStep = workflow.Steps.Last();
        secondStep.FromState.Should().Be(WorkflowState.Submitted.ToString());
        secondStep.ToState.Should().Be(WorkflowState.Approved.ToString());
        secondStep.TransitionedBy.Should().Be(approver);
        secondStep.Comments.Should().Be(approveComment);

        // Verify Event Bus published approval event
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<WorkflowStateChangedEvent>(e => 
                e.WorkflowId == submitResult.WorkflowId &&
                e.OldState == WorkflowState.Submitted &&
                e.NewState == WorkflowState.Approved &&
                e.TransitionedBy == approver &&
                e.Comments == approveComment),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectAsync_ShouldTransitionStateToRejected_AndRecordAuditLog()
    {
        // Arrange
        string entityType = "Employee";
        string entityId = "EMP-002";
        string requester = "initiator-user";
        string rejecter = "rejecter-user";
        string rejectComment = "Rejected due to invalid TIN.";

        var submitResult = await _approvalEngine.SubmitAsync(entityType, entityId, requester, "Submit");
        submitResult.IsSuccess.Should().BeTrue();

        _eventBus.ClearReceivedCalls();

        // Act
        var rejectResult = await _approvalEngine.RejectAsync(submitResult.WorkflowId, rejecter, rejectComment);

        // Assert
        rejectResult.IsSuccess.Should().BeTrue();
        rejectResult.CurrentState.Should().Be(WorkflowState.Rejected.ToString());

        var workflow = await _context.ApprovalWorkflows
            .Include(w => w.Steps)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.WorkflowId == submitResult.WorkflowId);

        workflow.Should().NotBeNull();
        workflow!.CurrentState.Should().Be(WorkflowState.Rejected);

        // Verify steps
        workflow.Steps.Should().HaveCount(2);
        var secondStep = workflow.Steps.Last();
        secondStep.FromState.Should().Be(WorkflowState.Submitted.ToString());
        secondStep.ToState.Should().Be(WorkflowState.Rejected.ToString());
        secondStep.TransitionedBy.Should().Be(rejecter);
        secondStep.Comments.Should().Be(rejectComment);

        // Verify Event Bus published rejection event
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<WorkflowStateChangedEvent>(e => 
                e.WorkflowId == submitResult.WorkflowId &&
                e.OldState == WorkflowState.Submitted &&
                e.NewState == WorkflowState.Rejected &&
                e.TransitionedBy == rejecter &&
                e.Comments == rejectComment),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_ShouldFail_IfWorkflowIsAlreadyApproved()
    {
        // Arrange
        var submitResult = await _approvalEngine.SubmitAsync("Employee", "EMP-003", "initiator-user", "Submit");
        
        // Approve first time
        var firstApprove = await _approvalEngine.ApproveAsync(submitResult.WorkflowId, "approver-user", "OK");
        firstApprove.IsSuccess.Should().BeTrue();

        // Act
        var secondApprove = await _approvalEngine.ApproveAsync(submitResult.WorkflowId, "approver-user", "Duplicate OK");

        // Assert
        secondApprove.IsSuccess.Should().BeFalse();
        secondApprove.Errors.Should().Contain("Workflow request is not in a state that can be approved.");
    }

    [Fact]
    public async Task ApprovalEngine_ShouldEnforceTenantIsolation()
    {
        // Arrange
        // Submit workflow for Company 1
        _tenantProvider.GetCurrentCompanyId().Returns(1);
        var submitResult = await _approvalEngine.SubmitAsync("Employee", "EMP-004", "initiator-user", "Submit");
        submitResult.IsSuccess.Should().BeTrue();

        // Act & Assert
        // Attempt to approve as Company 2
        _tenantProvider.GetCurrentCompanyId().Returns(2);
        var approveResult = await _approvalEngine.ApproveAsync(submitResult.WorkflowId, "approver-user", "Approve cross-tenant");
        
        approveResult.IsSuccess.Should().BeFalse();
        approveResult.Errors.Should().Contain("Workflow request was not found.");
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }
}
