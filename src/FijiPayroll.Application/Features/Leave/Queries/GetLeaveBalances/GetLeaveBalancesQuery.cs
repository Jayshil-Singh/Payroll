using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Leave;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Leave.Queries.GetLeaveBalances;

/// <summary>
/// Query to retrieve leave balances for a specific employee in a given fiscal year.
/// </summary>
public sealed record GetLeaveBalancesQuery(int EmployeeId, int FiscalYear) : IRequest<Result<IReadOnlyList<LeaveBalanceDto>>>;

/// <summary>
/// DTO representing an employee's leave balance details.
/// </summary>
public sealed class LeaveBalanceDto
{
    /// <summary>Balance record identifier.</summary>
    public int Id { get; set; }

    /// <summary>Associated employee identifier.</summary>
    public int EmployeeId { get; set; }

    /// <summary>Leave type identifier.</summary>
    public int LeaveTypeId { get; set; }

    /// <summary>Leave type display name.</summary>
    public string LeaveTypeName { get; set; } = string.Empty;

    /// <summary>Fiscal year of this balance tracker.</summary>
    public int FiscalYear { get; set; }

    /// <summary>Annual entitlement days.</summary>
    public decimal Entitlement { get; set; }

    /// <summary>Accrued days to date.</summary>
    public decimal Accrued { get; set; }

    /// <summary>Days brought forward from previous year.</summary>
    public decimal CarriedForward { get; set; }

    /// <summary>Days taken and processed in payroll.</summary>
    public decimal Taken { get; set; }

    /// <summary>Days reserved in pending approved requests.</summary>
    public decimal Pending { get; set; }

    /// <summary>Calculated available days balance.</summary>
    public decimal Available { get; set; }
}

/// <summary>
/// Handler for <see cref="GetLeaveBalancesQuery"/>.
/// </summary>
public sealed class GetLeaveBalancesQueryHandler : IRequestHandler<GetLeaveBalancesQuery, Result<IReadOnlyList<LeaveBalanceDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes dependencies.</summary>
    public GetLeaveBalancesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LeaveBalanceDto>>> Handle(GetLeaveBalancesQuery request, CancellationToken cancellationToken)
    {
        // 1. Permission check
        if (!_currentUser.HasPermission(PermissionConstants.LeaveView))
        {
            return Result<IReadOnlyList<LeaveBalanceDto>>.Failure("Forbidden: You do not have permission to view leave balances.");
        }

        // 2. Load employee to check company access
        var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return Result<IReadOnlyList<LeaveBalanceDto>>.Failure($"Employee with ID {request.EmployeeId} was not found.");
        }

        // 3. Company access check
        if (!_currentUser.HasCompanyAccess(employee.CompanyId))
        {
            return Result<IReadOnlyList<LeaveBalanceDto>>.Failure("Forbidden: You do not have access to this company's records.");
        }

        // 4. Load balances
        var balances = await _unitOfWork.Leave.GetBalancesByEmployeeAsync(request.EmployeeId, request.FiscalYear, cancellationToken);

        // 5. Map to DTOs
        var dtos = balances.Select(b => new LeaveBalanceDto
        {
            Id = b.Id,
            EmployeeId = b.EmployeeId,
            LeaveTypeId = b.LeaveTypeId,
            LeaveTypeName = b.LeaveType?.TypeName ?? "Unknown Leave Type",
            FiscalYear = b.FiscalYear,
            Entitlement = b.Entitlement,
            Accrued = b.Accrued,
            CarriedForward = b.CarriedForward,
            Taken = b.Taken,
            Pending = b.Pending,
            Available = b.Available
        }).ToList();

        return Result<IReadOnlyList<LeaveBalanceDto>>.Success(dtos.AsReadOnly());
    }
}
