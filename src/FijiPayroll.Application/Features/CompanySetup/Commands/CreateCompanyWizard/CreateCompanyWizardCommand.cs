using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Events;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.CompanySetup.Commands.CreateCompanyWizard;

/// <summary>
/// CQRS Command to finalize onboarding for a company wizard setup run, using transactions.
/// </summary>
public sealed record CreateCompanyWizardCommand(int CompanyId, Guid ExecutionId) : IRequest<Result<Guid>>;

/// <summary>
/// Handler for <see cref="CreateCompanyWizardCommand"/>.
/// </summary>
public sealed class CreateCompanyWizardCommandHandler : IRequestHandler<CreateCompanyWizardCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISetupWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateCompanyWizardCommandHandler> _logger;

    /// <summary>
    /// Initialises a new instance of the <see cref="CreateCompanyWizardCommandHandler"/> class.
    /// </summary>
    public CreateCompanyWizardCommandHandler(
        IUnitOfWork unitOfWork,
        ISetupWorkflowService workflowService,
        ICurrentUserService currentUser,
        ILogger<CreateCompanyWizardCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateCompanyWizardCommand request, CancellationToken cancellationToken)
    {
        // 1. Permission check
        if (!_currentUser.HasPermission(PermissionConstants.CompanyEdit))
        {
            throw new ForbiddenAccessException(PermissionConstants.CompanyEdit);
        }

        // 2. Company access check
        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<Guid>.Failure($"You do not have access to company ID {request.CompanyId}.");
        }

        // 3. Get Company
        var company = await _unitOfWork.Setup.GetCompanyByIdAsync(request.CompanyId, cancellationToken);
        if (company == null)
        {
            return Result<Guid>.Failure("Company profile not found.");
        }

        // 4. Idempotency Check — Check if already complete
        if (company.IsSetupComplete)
        {
            _logger.LogInformation("Company {CompanyId} wizard setup is already complete (Idempotency return).", request.CompanyId);
            return Result<Guid>.Success(request.ExecutionId);
        }

        // 5. Idempotency Check — Check if execution record already exists
        var existingRecord = await _unitOfWork.Setup.GetSetupExecutionRecordAsync(request.CompanyId, request.ExecutionId, cancellationToken);
        if (existingRecord != null)
        {
            if (existingRecord.Status == ExecutionStatus.Completed)
            {
                return Result<Guid>.Success(request.ExecutionId);
            }
            if (existingRecord.Status == ExecutionStatus.Failed)
            {
                return Result<Guid>.Failure(existingRecord.ErrorMessage ?? "Previous onboarding wizard run failed.");
            }
            return Result<Guid>.Failure("An execution wizard run is already in progress for this execution ID.");
        }

        var username = _currentUser.Username ?? "System";

        // 6. Begin transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        SetupExecutionRecord? record = null;
        try
        {
            // Create and add the execution record inside the transaction
            record = SetupExecutionRecord.Create(
                request.CompanyId,
                request.ExecutionId,
                Environment.MachineName,
                "1.0.0"
            );
            await _unitOfWork.Setup.AddSetupExecutionRecordAsync(record, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Dry-run validation checks checklist
            var validation = await _workflowService.ValidateSetupAsync(request.CompanyId, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors);
                record.MarkFailed($"Validation errors: {errors}", null);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                
                return Result<Guid>.Failure(validation.Errors);
            }

            // Complete the Validation step via SetupWorkflowService
            var workflowResult = await _workflowService.CompleteStepAsync(request.CompanyId, WizardStep.Validation, username, cancellationToken);
            if (!workflowResult.IsSuccess)
            {
                record.MarkFailed($"Workflow transition failed: {workflowResult.Error}", null);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return Result<Guid>.Failure(workflowResult.Error);
            }

            // Raise Domain Event
            company.AddDomainEvent(new CompanySetupCompletedEvent(request.CompanyId, request.ExecutionId, username));

            // Mark execution record completed
            record.MarkCompleted();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Commit transaction
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Company {CompanyId} wizard setup successfully completed by {User}. ExecutionId: {ExecutionId}", 
                request.CompanyId, username, request.ExecutionId);

            return Result<Guid>.Success(request.ExecutionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to finalize company setup wizard transaction. Rolling back.");
            try
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback company setup wizard transaction.");
            }

            // Create and save a failed execution record to ensure auditability of the failure
            try
            {
                var failedRecord = SetupExecutionRecord.Create(
                    request.CompanyId,
                    request.ExecutionId,
                    Environment.MachineName,
                    "1.0.0"
                );
                failedRecord.MarkFailed(ex.Message, ex.StackTrace);
                await _unitOfWork.Setup.AddSetupExecutionRecordAsync(failedRecord, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to save failed execution record state.");
            }

            return Result<Guid>.Failure($"Failed to commit company onboarding wizard: {ex.Message}");
        }
    }
}
