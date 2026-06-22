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

namespace FijiPayroll.Application.Features.Leave.Commands.ApproveLeaveRequest;

/// <summary>
/// Command to approve a pending leave request and reserve the balance.
/// </summary>
public sealed record ApproveLeaveRequestCommand(int RequestId) : IRequest<Result>, ITransactional;

/// <summary>
/// Handler for <see cref="ApproveLeaveRequestCommand"/>.
/// </summary>
public sealed class ApproveLeaveRequestCommandHandler : IRequestHandler<ApproveLeaveRequestCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<ApproveLeaveRequestCommandHandler> _logger;

    /// <summary>Initializes dependencies.</summary>
    public ApproveLeaveRequestCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeService dateTime,
        ILogger<ApproveLeaveRequestCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ApproveLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        // 1. Permission check
        if (!_currentUser.HasPermission(PermissionConstants.LeaveApprove))
        {
            throw new ForbiddenAccessException(PermissionConstants.LeaveApprove);
        }

        // 2. Load request
        var leaveRequest = await _unitOfWork.Leave.GetRequestByIdAsync(request.RequestId, cancellationToken);
        if (leaveRequest == null)
        {
            return Result.Failure($"Leave request with ID {request.RequestId} was not found.");
        }

        // 3. Company access check
        if (!_currentUser.HasCompanyAccess(leaveRequest.CompanyId))
        {
            return Result.Failure("Forbidden: You do not have access to this company's records.");
        }

        // 4. Load or initialize leave balance
        int fiscalYear = leaveRequest.StartDate.Year;
        var leaveBalance = await _unitOfWork.Leave.GetBalanceAsync(
            leaveRequest.EmployeeId,
            leaveRequest.LeaveTypeId,
            fiscalYear,
            cancellationToken);

        if (leaveBalance == null)
        {
            var leaveType = await _unitOfWork.Leave.GetTypeByIdAsync(leaveRequest.LeaveTypeId, cancellationToken);
            if (leaveType == null)
            {
                return Result.Failure("Leave type associated with request was not found.");
            }

            leaveBalance = LeaveBalance.Initialise(
                leaveRequest.CompanyId,
                leaveRequest.EmployeeId,
                leaveRequest.LeaveTypeId,
                fiscalYear,
                leaveType.EntitlementDays);

            leaveBalance.CreatedBy = _currentUser.Username;
            leaveBalance.CreatedAt = _dateTime.UtcNow;
            await _unitOfWork.Leave.AddBalanceAsync(leaveBalance, cancellationToken);
        }

        // 5. Update state and reserve balance
        try
        {
            // First reserve days on the balance to verify availability
            leaveBalance.Reserve(leaveRequest.TotalDays);
            leaveBalance.ModifiedBy = _currentUser.Username;
            leaveBalance.ModifiedAt = _dateTime.UtcNow;

            // Approve request
            leaveRequest.Approve(_currentUser.Username);
            leaveRequest.ModifiedBy = _currentUser.Username;
            leaveRequest.ModifiedAt = _dateTime.UtcNow;
        }
        catch (Exception ex) when (ex is FijiPayroll.Domain.Exceptions.DomainException || ex is ArgumentException)
        {
            _logger.LogWarning(ex, "Domain rule violation approving leave request ID {Id}.", request.RequestId);
            return Result.Failure(ex.Message);
        }

        // 6. Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Leave request ID {RequestId} approved by {User}. Reserved {Days} days from balance.",
            leaveRequest.Id, _currentUser.Username, leaveRequest.TotalDays);

        return Result.Success();
    }
}
