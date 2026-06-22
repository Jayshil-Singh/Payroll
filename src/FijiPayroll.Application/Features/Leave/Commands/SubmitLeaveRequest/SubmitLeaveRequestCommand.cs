using FijiPayroll.Application.Common.Behaviours;
using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Leave;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Leave.Commands.SubmitLeaveRequest;

/// <summary>
/// Command to submit a new employee leave request.
/// </summary>
public sealed record SubmitLeaveRequestCommand(
    int CompanyId,
    int EmployeeId,
    int LeaveTypeId,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalDays,
    string? Notes = null
) : IRequest<Result<int>>, ITransactional, IRequirePermission
{
    public string Permission => PermissionConstants.LeaveCreate;
}

/// <summary>
/// Handler for <see cref="SubmitLeaveRequestCommand"/>.
/// </summary>
public sealed class SubmitLeaveRequestCommandHandler : IRequestHandler<SubmitLeaveRequestCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<SubmitLeaveRequestCommandHandler> _logger;

    /// <summary>Initializes dependencies.</summary>
    public SubmitLeaveRequestCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeService dateTime,
        ILogger<SubmitLeaveRequestCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<int>> Handle(SubmitLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        // 1. Permission check
        if (!_currentUser.HasPermission(PermissionConstants.LeaveCreate))
        {
            throw new ForbiddenAccessException(PermissionConstants.LeaveCreate);
        }

        // 2. Company access check
        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<int>.Failure("Forbidden: You do not have access to this company.");
        }

        // 3. Load employee to make sure they exist and belong to this company
        var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return Result<int>.Failure($"Employee with ID {request.EmployeeId} was not found.");
        }

        if (employee.CompanyId != request.CompanyId)
        {
            return Result<int>.Failure("Employee company mismatch.");
        }

        // 4. Load leave type
        var leaveType = await _unitOfWork.Leave.GetTypeByIdAsync(request.LeaveTypeId, cancellationToken);
        if (leaveType == null)
        {
            return Result<int>.Failure($"Leave type with ID {request.LeaveTypeId} was not found.");
        }

        if (leaveType.CompanyId != request.CompanyId)
        {
            return Result<int>.Failure("Leave type company mismatch.");
        }

        if (!leaveType.IsActive)
        {
            return Result<int>.Failure("The selected leave type is not active.");
        }

        // 5. Submit leave request (creates entity and raises event)
        LeaveRequest leaveRequest;
        try
        {
            leaveRequest = LeaveRequest.Submit(
                companyId: request.CompanyId,
                employeeId: request.EmployeeId,
                leaveTypeId: request.LeaveTypeId,
                startDate: request.StartDate,
                endDate: request.EndDate,
                totalDays: request.TotalDays,
                medicalCertificateRequired: leaveType.RequiresMedicalCertificate,
                notes: request.Notes);

            leaveRequest.CreatedBy = _currentUser.Username;
            leaveRequest.CreatedAt = _dateTime.UtcNow;
        }
        catch (Exception ex) when (ex is FijiPayroll.Domain.Exceptions.DomainException || ex is ArgumentException)
        {
            _logger.LogWarning(ex, "Domain rule violation submitting leave request.");
            return Result<int>.Failure(ex.Message);
        }

        // 6. Persist request
        await _unitOfWork.Leave.AddRequestAsync(leaveRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Leave request (ID: {RequestId}) submitted for employee {EmployeeId} ({Days} days) by {User}",
            leaveRequest.Id, request.EmployeeId, request.TotalDays, _currentUser.Username);

        return Result<int>.Success(leaveRequest.Id);
    }
}
