using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Application.Features.PayrollComponents.Commands.UpdatePayrollComponent;

/// <summary>
/// Handles <see cref="UpdatePayrollComponentCommand"/>.
///
/// Steps:
/// <list type="number">
///   <item>Verify permission.</item>
///   <item>Load entity — throws <see cref="NotFoundException"/> if missing.</item>
///   <item>Verify company access (prevents cross-company tampering).</item>
///   <item>Delegate update to the domain entity (enforces system component restrictions).</item>
///   <item>Persist changes.</item>
/// </list>
/// </summary>
public sealed class UpdatePayrollComponentCommandHandler
    : IRequestHandler<UpdatePayrollComponentCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<UpdatePayrollComponentCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public UpdatePayrollComponentCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeService dateTime,
        ILogger<UpdatePayrollComponentCommandHandler> logger)
    {
        _unitOfWork  = unitOfWork;
        _currentUser = currentUser;
        _dateTime    = dateTime;
        _logger      = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        UpdatePayrollComponentCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Permission ────────────────────────────────────────────────────────
        if (!_currentUser.HasPermission(PermissionConstants.PayrollComponentsEdit))
        {
            throw new ForbiddenAccessException(PermissionConstants.PayrollComponentsEdit);
        }

        // ── 2. Load entity ───────────────────────────────────────────────────────
        var component = await _unitOfWork.PayrollComponents.GetByIdAsync(
            request.Id, cancellationToken);

        if (component is null)
        {
            throw new NotFoundException(nameof(Domain.Entities.Company.PayrollComponent), request.Id);
        }

        // ── 3. Company access ────────────────────────────────────────────────────
        if (!_currentUser.HasCompanyAccess(component.CompanyId))
        {
            return Result.Failure("You do not have access to the company owning this component.");
        }

        // ── 4. Domain update ─────────────────────────────────────────────────────
        try
        {
            component.Update(
                componentName:     request.ComponentName,
                componentType:     request.ComponentType,
                calculationMethod: request.CalculationMethod,
                calculationValue:  request.CalculationValue,
                formula:           request.Formula,
                isTaxable:         request.IsTaxable,
                isFnpfApplicable:  request.IsFnpfApplicable,
                displayOrder:      request.DisplayOrder,
                description:       request.Description);
        }
        catch (Exception ex) when (ex is Domain.Exceptions.DomainException)
        {
            return Result.Failure(ex.Message);
        }

        component.ModifiedBy = _currentUser.Username;
        component.ModifiedAt = _dateTime.UtcNow;

        // ── 5. Persist ───────────────────────────────────────────────────────────
        _unitOfWork.PayrollComponents.Update(component);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payroll component '{Code}' (ID: {Id}) updated by {User}",
            component.ComponentCode, component.Id, _currentUser.Username);

        return Result.Success();
    }
}
