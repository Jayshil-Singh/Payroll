using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Application.Features.PayrollComponents.Commands.TogglePayrollComponentActive;

/// <summary>
/// Handles <see cref="TogglePayrollComponentActiveCommand"/>.
/// Delegates activate/deactivate to the domain entity, which enforces system component protection.
/// </summary>
public sealed class TogglePayrollComponentActiveCommandHandler
    : IRequestHandler<TogglePayrollComponentActiveCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<TogglePayrollComponentActiveCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public TogglePayrollComponentActiveCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeService dateTime,
        ILogger<TogglePayrollComponentActiveCommandHandler> logger)
    {
        _unitOfWork  = unitOfWork;
        _currentUser = currentUser;
        _dateTime    = dateTime;
        _logger      = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        TogglePayrollComponentActiveCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollComponentsEdit))
        {
            throw new ForbiddenAccessException(PermissionConstants.PayrollComponentsEdit);
        }

        var component = await _unitOfWork.PayrollComponents.GetByIdAsync(
            request.Id, cancellationToken);

        if (component is null)
        {
            throw new NotFoundException(nameof(Domain.Entities.Company.PayrollComponent), request.Id);
        }

        if (!_currentUser.HasCompanyAccess(component.CompanyId))
        {
            return Result.Failure("You do not have access to the company owning this component.");
        }

        try
        {
            if (request.SetActive)
            {
                component.Activate();
            }
            else
            {
                component.Deactivate();
            }
        }
        catch (Exception ex) when (ex is Domain.Exceptions.DomainException)
        {
            return Result.Failure(ex.Message);
        }

        component.ModifiedBy = _currentUser.Username;
        component.ModifiedAt = _dateTime.UtcNow;

        _unitOfWork.PayrollComponents.Update(component);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payroll component '{Code}' (ID: {Id}) {Action} by {User}",
            component.ComponentCode, request.Id,
            request.SetActive ? "activated" : "deactivated",
            _currentUser.Username);

        return Result.Success();
    }
}
