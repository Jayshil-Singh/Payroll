using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Notifications.Queries;

// ── DTO ───────────────────────────────────────────────────────────────────────

public sealed class DesktopNotificationDto
{
    public int      Id        { get; set; }
    public string   Subject   { get; set; } = string.Empty;
    public string   Message   { get; set; } = string.Empty;
    public string   Category  { get; set; } = "Info";
    public bool     IsRead    { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt   { get; set; }
}

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Retrieves recent desktop-channel notifications for the specified company.
/// </summary>
public sealed record GetDesktopNotificationsQuery(int CompanyId, int MaxCount = 20) 
    : IRequest<Result<IReadOnlyList<DesktopNotificationDto>>>;

public sealed class GetDesktopNotificationsQueryHandler
    : IRequestHandler<GetDesktopNotificationsQuery, Result<IReadOnlyList<DesktopNotificationDto>>>
{
    private readonly IComplianceRepository _complianceRepo;

    public GetDesktopNotificationsQueryHandler(IComplianceRepository complianceRepo)
    {
        _complianceRepo = complianceRepo ?? throw new ArgumentNullException(nameof(complianceRepo));
    }

    public async Task<Result<IReadOnlyList<DesktopNotificationDto>>> Handle(
        GetDesktopNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var notifications = await _complianceRepo.GetDesktopNotificationsAsync(
                request.CompanyId, 
                request.MaxCount, 
                cancellationToken);

            var dtos = notifications.Select(n => new DesktopNotificationDto
            {
                Id        = n.Id,
                Subject   = n.Subject,
                Message   = n.Message,
                Category  = n.Category,
                IsRead    = n.IsRead,
                CreatedAt = n.CreatedAt,
                ReadAt    = n.ReadAt
            }).ToList();

            return Result<IReadOnlyList<DesktopNotificationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<DesktopNotificationDto>>.Failure($"Failed to retrieve notifications: {ex.Message}");
        }
    }
}
