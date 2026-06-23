using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Notifications.Commands;

// ── Mark Single Notification As Read ──────────────────────────────────────────

public sealed record MarkNotificationReadCommand(int NotificationId) : IRequest<Result<Unit>>;

public sealed class MarkNotificationReadCommandHandler
    : IRequestHandler<MarkNotificationReadCommand, Result<Unit>>
{
    private readonly IComplianceRepository _complianceRepo;
    private readonly IUnitOfWork          _unitOfWork;

    public MarkNotificationReadCommandHandler(IComplianceRepository complianceRepo, IUnitOfWork unitOfWork)
    {
        _complianceRepo = complianceRepo ?? throw new ArgumentNullException(nameof(complianceRepo));
        _unitOfWork     = unitOfWork     ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Unit>> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await _complianceRepo.GetNotificationByIdAsync(request.NotificationId, cancellationToken);
            if (notification == null)
                return Result<Unit>.Failure($"Notification {request.NotificationId} not found.");

            notification.MarkAsRead();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to mark notification as read: {ex.Message}");
        }
    }
}

// ── Mark All Notifications As Read ───────────────────────────────────────────

public sealed record MarkAllNotificationsReadCommand(int CompanyId) : IRequest<Result<Unit>>;

public sealed class MarkAllNotificationsReadCommandHandler
    : IRequestHandler<MarkAllNotificationsReadCommand, Result<Unit>>
{
    private readonly IComplianceRepository _complianceRepo;
    private readonly IUnitOfWork          _unitOfWork;

    public MarkAllNotificationsReadCommandHandler(IComplianceRepository complianceRepo, IUnitOfWork unitOfWork)
    {
        _complianceRepo = complianceRepo ?? throw new ArgumentNullException(nameof(complianceRepo));
        _unitOfWork     = unitOfWork     ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Unit>> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var unread = await _complianceRepo.GetUnreadDesktopNotificationsAsync(request.CompanyId, cancellationToken);
            if (unread.Count == 0)
                return Result<Unit>.Success(Unit.Value);

            foreach (var n in unread)
            {
                n.MarkAsRead();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to mark all notifications as read: {ex.Message}");
        }
    }
}
