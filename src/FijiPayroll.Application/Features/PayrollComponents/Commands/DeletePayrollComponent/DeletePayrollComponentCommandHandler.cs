using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Application.Features.PayrollComponents.Commands.DeletePayrollComponent;

/// <summary>
/// Handles <see cref="DeletePayrollComponentCommand"/>.
/// Enforces that system components and in-use components cannot be deleted.
/// </summary>
public sealed class DeletePayrollComponentCommandHandler
    : IRequestHandler<DeletePayrollComponentCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DeletePayrollComponentCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public DeletePayrollComponentCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ILogger<DeletePayrollComponentCommandHandler> logger)
    {
        _unitOfWork  = unitOfWork;
        _currentUser = currentUser;
        _logger      = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        DeletePayrollComponentCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Permission ────────────────────────────────────────────────────────
        if (!_currentUser.HasPermission(PermissionConstants.PayrollComponentsDelete))
        {
            throw new ForbiddenAccessException(PermissionConstants.PayrollComponentsDelete);
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

        // ── 4. System component check ────────────────────────────────────────────
        // Domain entity enforces this, but we surface a friendly message here.
        if (component.IsSystemComponent)
        {
            return Result.Failure(
                $"'{component.ComponentCode}' is a system component and cannot be deleted.");
        }

        // ── 5. In-use check ──────────────────────────────────────────────────────
        var isUsed = await _unitOfWork.PayrollComponents.IsUsedInPayrollRunsAsync(
            request.Id, cancellationToken);

        if (isUsed)
        {
            return Result.Failure(
                $"'{component.ComponentCode}' cannot be deleted because it has been used in one " +
                "or more payroll runs. Deactivate it instead.");
        }

        // ── 6. Soft delete ───────────────────────────────────────────────────────
        try
        {
            component.SoftDelete(_currentUser.Username);
        }
        catch (Exception ex) when (ex is Domain.Exceptions.DomainException)
        {
            return Result.Failure(ex.Message);
        }

        _unitOfWork.PayrollComponents.Update(component);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payroll component '{Code}' (ID: {Id}) soft-deleted by {User}",
            component.ComponentCode, request.Id, _currentUser.Username);

        return Result.Success();
    }
}
