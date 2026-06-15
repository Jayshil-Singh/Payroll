using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;

namespace FijiPayroll.Application.Features.PayrollComponents.Queries.GetPayrollComponentById;

/// <summary>
/// Handles <see cref="GetPayrollComponentByIdQuery"/>.
/// Maps the domain entity to a <see cref="PayrollComponentDetailDto"/> without
/// leaking domain objects to the presentation layer.
/// </summary>
public sealed class GetPayrollComponentByIdQueryHandler
    : IRequestHandler<GetPayrollComponentByIdQuery, Result<PayrollComponentDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initialises the handler.</summary>
    public GetPayrollComponentByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _unitOfWork  = unitOfWork;
        _currentUser = currentUser;
    }

    /// <inheritdoc/>
    public async Task<Result<PayrollComponentDetailDto>> Handle(
        GetPayrollComponentByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollComponentsView))
        {
            throw new ForbiddenAccessException(PermissionConstants.PayrollComponentsView);
        }

        var component = await _unitOfWork.PayrollComponents.GetByIdAsync(
            request.Id, cancellationToken);

        if (component is null)
        {
            throw new NotFoundException(nameof(Domain.Entities.Company.PayrollComponent), request.Id);
        }

        if (!_currentUser.HasCompanyAccess(component.CompanyId))
        {
            throw new ForbiddenAccessException(PermissionConstants.PayrollComponentsView);
        }

        var dto = new PayrollComponentDetailDto
        {
            Id                  = component.Id,
            CompanyId           = component.CompanyId,
            ComponentCode       = component.ComponentCode,
            ComponentName       = component.ComponentName,
            ComponentType       = component.ComponentType,
            CalculationMethod   = component.CalculationMethod,
            CalculationValue    = component.CalculationValue,
            Formula             = component.Formula,
            IsTaxable           = component.IsTaxable,
            IsFnpfApplicable    = component.IsFnpfApplicable,
            DisplayOrder        = component.DisplayOrder,
            Description         = component.Description,
            IsSystemComponent   = component.IsSystemComponent,
            IsActive            = component.IsActive,
            CreatedBy           = component.CreatedBy,
            CreatedAt           = component.CreatedAt,
            ModifiedBy          = component.ModifiedBy,
            ModifiedAt          = component.ModifiedAt,
        };

        return Result<PayrollComponentDetailDto>.Success(dto);
    }
}
