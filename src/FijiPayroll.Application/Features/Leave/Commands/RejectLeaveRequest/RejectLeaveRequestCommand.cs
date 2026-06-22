using FijiPayroll.Application.Common.Behaviours;
using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Leave.Commands.RejectLeaveRequest;

/// <summary>
/// Command to reject a pending leave request with a mandatory reason.
/// </summary>
public sealed record RejectLeaveRequestCommand(int RequestId, string Reason) : IRequest<Result>, ITransactional;

/// <summary>
/// Handler for <see cref="RejectLeaveRequestCommand"/>.
/// </summary>
public sealed class RejectLeaveRequestCommandHandler : IRequestHandler<RejectLeaveRequestCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<RejectLeaveRequestCommandHandler> _logger;

    /// <summary>Initializes dependencies.</summary>
    public RejectLeaveRequestCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeService dateTime,
        ILogger<RejectLeaveRequestCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(RejectLeaveRequestCommand request, CancellationToken cancellationToken)
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

        // 4. Update request status to Rejected
        try
        {
            leaveRequest.Reject(_currentUser.Username, request.Reason);
            leaveRequest.ModifiedBy = _currentUser.Username;
            leaveRequest.ModifiedAt = _dateTime.UtcNow;
        }
        catch (Exception ex) when (ex is FijiPayroll.Domain.Exceptions.DomainException || ex is ArgumentException)
        {
            _logger.LogWarning(ex, "Domain rule violation rejecting leave request ID {Id}.", request.RequestId);
            return Result.Failure(ex.Message);
        }

        // 5. Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Leave request ID {RequestId} was rejected by {User}. Reason: {Reason}",
            leaveRequest.Id, _currentUser.Username, request.Reason);

        return Result.Success();
    }
}
