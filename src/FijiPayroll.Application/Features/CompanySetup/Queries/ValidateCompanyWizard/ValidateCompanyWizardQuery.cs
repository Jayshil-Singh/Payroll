using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Services;
using FijiPayroll.Shared.Constants;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.CompanySetup.Queries.ValidateCompanyWizard;

/// <summary>
/// CQRS Query to perform a dry-run validation check across all Company Setup Wizard steps.
/// </summary>
public sealed record ValidateCompanyWizardQuery(int CompanyId) : IRequest<Result<ValidationResultDto>>;

/// <summary>
/// Handler for <see cref="ValidateCompanyWizardQuery"/>.
/// </summary>
public sealed class ValidateCompanyWizardQueryHandler : IRequestHandler<ValidateCompanyWizardQuery, Result<ValidationResultDto>>
{
    private readonly ISetupWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUser;

    /// <summary>
    /// Initialises a new instance of the <see cref="ValidateCompanyWizardQueryHandler"/> class.
    /// </summary>
    public ValidateCompanyWizardQueryHandler(
        ISetupWorkflowService workflowService,
        ICurrentUserService currentUser)
    {
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <inheritdoc />
    public async Task<Result<ValidationResultDto>> Handle(ValidateCompanyWizardQuery request, CancellationToken cancellationToken)
    {
        // 1. Permission check
        if (!_currentUser.HasPermission(PermissionConstants.CompanyView))
        {
            throw new ForbiddenAccessException(PermissionConstants.CompanyView);
        }

        // 2. Company access check
        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<ValidationResultDto>.Failure($"You do not have access to company ID {request.CompanyId}.");
        }

        // 3. Dry-run validation
        var resultDto = await _workflowService.ValidateSetupAsync(request.CompanyId, cancellationToken);
        return Result<ValidationResultDto>.Success(resultDto);
    }
}
