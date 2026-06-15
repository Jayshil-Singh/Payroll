using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;

namespace FijiPayroll.Application.Features.PayrollComponents.Queries.GetPayrollComponentList;

/// <summary>
/// Handles <see cref="GetPayrollComponentListQuery"/>.
/// Applies search, type filter, and pagination server-side via SQL Server.
/// </summary>
public sealed class GetPayrollComponentListQueryHandler
: IRequestHandler<GetPayrollComponentListQuery, Result<PagedResult<PayrollComponentSummaryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initialises the handler.</summary>
    public GetPayrollComponentListQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <inheritdoc/>
    public async Task<Result<PagedResult<PayrollComponentSummaryDto>>> Handle(
        GetPayrollComponentListQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollComponentsView))
        {
            throw new ForbiddenAccessException(PermissionConstants.PayrollComponentsView);
        }

        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            throw new ForbiddenAccessException(PermissionConstants.PayrollComponentsView);
        }

        // Perform server-side filtering, ordering, and pagination
        var (items, totalCount) = await _unitOfWork.PayrollComponents.GetPagedAsync(
            request.CompanyId,
            request.SearchTerm,
            request.ComponentTypeFilter,
            request.ActiveOnly,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        // Map database entities to DTOs
        var dtos = items.Select(c => new PayrollComponentSummaryDto
        {
            Id = c.Id,
            ComponentCode = c.ComponentCode,
            ComponentName = c.ComponentName,
            ComponentType = c.ComponentType,
            CalculationMethod = c.CalculationMethod,
            IsTaxable = c.IsTaxable,
            IsFnpfApplicable = c.IsFnpfApplicable,
            DisplayOrder = c.DisplayOrder,
            IsSystemComponent = c.IsSystemComponent,
            IsActive = c.IsActive,
        }).ToList();

        var pagedResult = new PagedResult<PayrollComponentSummaryDto>(
            dtos.AsReadOnly(),
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Result<PagedResult<PayrollComponentSummaryDto>>.Success(pagedResult);
    }
}
