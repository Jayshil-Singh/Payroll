using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Leave;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Leave.Queries.GetLeaveRequests;

/// <summary>
/// Query to retrieve leave requests for a company, optionally filtered by a specific employee.
/// </summary>
public sealed record GetLeaveRequestsQuery(int CompanyId, int? EmployeeId = null) : IRequest<Result<IReadOnlyList<LeaveRequestDto>>>;

/// <summary>
/// DTO representing a leave request record.
/// </summary>
public sealed class LeaveRequestDto
{
    /// <summary>Unique request identifier.</summary>
    public int Id { get; set; }

    /// <summary>Associated employee identifier.</summary>
    public int EmployeeId { get; set; }

    /// <summary>Employee's full name.</summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>Leave type identifier.</summary>
    public int LeaveTypeId { get; set; }

    /// <summary>Display name of the leave type.</summary>
    public string LeaveTypeName { get; set; } = string.Empty;

    /// <summary>Start date of leave period.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>End date of leave period.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Total requested working days.</summary>
    public decimal TotalDays { get; set; }

    /// <summary>Current approval status (Pending/Approved/Rejected/Cancelled).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Employee notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Manager who approved or rejected the request.</summary>
    public string? ApprovedRejectedBy { get; set; }

    /// <summary>Timestamp of approval or rejection decision.</summary>
    public DateTime? ApprovedRejectedAt { get; set; }

    /// <summary>Reason for rejection.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>Reason for cancellation.</summary>
    public string? CancellationReason { get; set; }

    /// <summary>True if a medical certificate is required.</summary>
    public bool MedicalCertificateRequired { get; set; }

    /// <summary>True if a medical certificate was provided.</summary>
    public bool MedicalCertificateProvided { get; set; }
}

/// <summary>
/// Handler for <see cref="GetLeaveRequestsQuery"/>.
/// </summary>
public sealed class GetLeaveRequestsQueryHandler : IRequestHandler<GetLeaveRequestsQuery, Result<IReadOnlyList<LeaveRequestDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes dependencies.</summary>
    public GetLeaveRequestsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LeaveRequestDto>>> Handle(GetLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        // 1. Permission check
        if (!_currentUser.HasPermission(PermissionConstants.LeaveView))
        {
            return Result<IReadOnlyList<LeaveRequestDto>>.Failure("Forbidden: You do not have permission to view leave requests.");
        }

        // 2. Company access check
        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<IReadOnlyList<LeaveRequestDto>>.Failure("Forbidden: You do not have access to this company.");
        }

        // 3. Load requests
        IReadOnlyList<LeaveRequest> requests;
        if (request.EmployeeId.HasValue)
        {
            // Verify employee is in the requested company
            var emp = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId.Value, cancellationToken);
            if (emp == null || emp.CompanyId != request.CompanyId)
            {
                return Result<IReadOnlyList<LeaveRequestDto>>.Failure("Employee not found or company mismatch.");
            }

            requests = await _unitOfWork.Leave.GetRequestsByEmployeeAsync(request.EmployeeId.Value, cancellationToken);
        }
        else
        {
            requests = await _unitOfWork.Leave.GetRequestsByCompanyAsync(request.CompanyId, cancellationToken);
        }

        // 4. Fetch employee names
        var employeeIds = requests.Select(r => r.EmployeeId).Distinct().ToList();
        var employees = await _unitOfWork.Employees.GetByIdsAsync(employeeIds, cancellationToken);
        var employeeNameMap = employees.ToDictionary(e => e.Id, e => e.FullName);

        // 5. Map to DTOs
        var dtos = requests.Select(r => new LeaveRequestDto
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            EmployeeName = employeeNameMap.TryGetValue(r.EmployeeId, out var name) ? name : "Unknown Employee",
            LeaveTypeId = r.LeaveTypeId,
            LeaveTypeName = r.LeaveType?.TypeName ?? "Unknown Leave Type",
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            TotalDays = r.TotalDays,
            Status = r.Status.ToString(),
            Notes = r.Notes,
            ApprovedRejectedBy = r.ApprovedRejectedBy,
            ApprovedRejectedAt = r.ApprovedRejectedAt,
            RejectionReason = r.RejectionReason,
            CancellationReason = r.CancellationReason,
            MedicalCertificateRequired = r.MedicalCertificateRequired,
            MedicalCertificateProvided = r.MedicalCertificateProvided
        }).ToList();

        return Result<IReadOnlyList<LeaveRequestDto>>.Success(dtos.AsReadOnly());
    }
}
