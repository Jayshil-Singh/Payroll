using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FijiPayroll.Application.Features.Employees.Queries.GetEmployeesList;

/// <summary>
/// Query to retrieve a server-side filtered and paginated list of employees.
/// </summary>
public sealed record GetEmployeesListQuery(
    int CompanyId,
    string? SearchTerm,
    string? DepartmentFilter,
    int PageNumber,
    int PageSize
) : IRequest<Result<PagedResult<EmployeeSummaryDto>>>;

/// <summary>
/// Summary details for an employee registry record.
/// </summary>
public sealed class EmployeeSummaryDto
{
    /// <summary>The employee's unique identifier.</summary>
    public int Id { get; set; }
    
    /// <summary>Displays formatted Employee Code matching standard payroll convention.</summary>
    public string EmployeeCode => $"EMP-{Id:D3}";
    
    /// <summary>The employee's full name.</summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>The employee's assigned department.</summary>
    public string Department { get; set; } = string.Empty;
    
    /// <summary>Residency status ("Resident" or "NonResident").</summary>
    public string ResidencyStatus { get; set; } = string.Empty;
    
    /// <summary>Periodic base salary or rate.</summary>
    public decimal BaseSalary { get; set; }
    
    /// <summary>Deactivation status indicator.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Handler for <see cref="GetEmployeesListQuery"/>.
/// </summary>
public sealed class GetEmployeesListQueryHandler
    : IRequestHandler<GetEmployeesListQuery, Result<PagedResult<EmployeeSummaryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes dependencies.</summary>
    public GetEmployeesListQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<EmployeeSummaryDto>>> Handle(
        GetEmployeesListQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.EmployeesView))
        {
            return Result<PagedResult<EmployeeSummaryDto>>.Failure("Forbidden: You do not have permission to view employees.");
        }

        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<PagedResult<EmployeeSummaryDto>>.Failure("Forbidden: You do not have access to this company.");
        }

        var (items, totalCount) = await _unitOfWork.Employees.GetPagedAsync(
            request.CompanyId,
            request.SearchTerm,
            request.DepartmentFilter,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(e => new EmployeeSummaryDto
        {
            Id = e.Id,
            FullName = e.FullName,
            Department = e.Department,
            ResidencyStatus = e.ResidencyStatus,
            BaseSalary = e.BaseSalary,
            IsActive = e.IsActive
        }).ToList();

        var pagedResult = new PagedResult<EmployeeSummaryDto>(
            dtos.AsReadOnly(),
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Result<PagedResult<EmployeeSummaryDto>>.Success(pagedResult);
    }
}
