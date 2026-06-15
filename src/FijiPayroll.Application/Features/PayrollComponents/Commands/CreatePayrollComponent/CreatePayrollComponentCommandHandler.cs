using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Application.Features.PayrollComponents.Commands.CreatePayrollComponent;

/// <summary>
/// Handles <see cref="CreatePayrollComponentCommand"/>.
///
/// Steps:
/// <list type="number">
///   <item>Verify the user holds <see cref="PermissionConstants.PayrollComponentsCreate"/>.</item>
///   <item>Verify the user has access to the target company.</item>
///   <item>Check the component code is unique within the company.</item>
///   <item>Create the domain entity via <see cref="PayrollComponent.Create"/>.</item>
///   <item>Persist via Unit of Work.</item>
///   <item>Return the new component's <see cref="FijiPayroll.Domain.Entities.Common.BaseEntity.Id"/>.</item>
/// </list>
/// </summary>
public sealed class CreatePayrollComponentCommandHandler
    : IRequestHandler<CreatePayrollComponentCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<CreatePayrollComponentCommandHandler> _logger;

    /// <summary>Initialises the handler with its dependencies.</summary>
    public CreatePayrollComponentCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeService dateTime,
        ILogger<CreatePayrollComponentCommandHandler> logger)
    {
        _unitOfWork  = unitOfWork;
        _currentUser = currentUser;
        _dateTime    = dateTime;
        _logger      = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<int>> Handle(
        CreatePayrollComponentCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Permission check ──────────────────────────────────────────────────
        if (!_currentUser.HasPermission(PermissionConstants.PayrollComponentsCreate))
        {
            throw new ForbiddenAccessException(PermissionConstants.PayrollComponentsCreate);
        }

        // ── 2. Company access check ──────────────────────────────────────────────
        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<int>.Failure(
                $"You do not have access to company ID {request.CompanyId}.");
        }

        // ── 3. Unique code check ─────────────────────────────────────────────────
        var codeExists = await _unitOfWork.PayrollComponents.CodeExistsAsync(
            request.CompanyId,
            request.ComponentCode,
            excludeId: null,
            cancellationToken);

        if (codeExists)
        {
            return Result<int>.Failure(
                $"A payroll component with code '{request.ComponentCode.ToUpperInvariant()}' " +
                $"already exists for this company.");
        }

        // ── 4. Create domain entity ──────────────────────────────────────────────
        PayrollComponent component;
        try
        {
            component = PayrollComponent.Create(
                companyId:         request.CompanyId,
                componentCode:     request.ComponentCode,
                componentName:     request.ComponentName,
                componentType:     request.ComponentType,
                calculationMethod: request.CalculationMethod,
                calculationValue:  request.CalculationValue,
                formula:           request.Formula,
                isTaxable:         request.IsTaxable,
                isFnpfApplicable:  request.IsFnpfApplicable,
                displayOrder:      request.DisplayOrder,
                description:       request.Description,
                isSystemComponent: false);
        }
        catch (Exception ex) when (ex is Domain.Exceptions.DomainException)
        {
            _logger.LogWarning(ex, "Domain rule violated creating payroll component");
            return Result<int>.Failure(ex.Message);
        }

        // Set audit fields — handled by EF Core interceptor but set here for explicitness
        component.CreatedBy = _currentUser.Username;
        component.CreatedAt = _dateTime.UtcNow;

        // ── 5. Persist ───────────────────────────────────────────────────────────
        await _unitOfWork.PayrollComponents.AddAsync(component, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payroll component '{Code}' (ID: {Id}) created for company {CompanyId} by {User}",
            component.ComponentCode, component.Id, request.CompanyId, _currentUser.Username);

        // ── 6. Return new ID ─────────────────────────────────────────────────────
        return Result<int>.Success(component.Id);
    }
}
