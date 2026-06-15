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

namespace FijiPayroll.Application.Features.Lookups.Commands.ArchiveLookup;

/// <summary>
/// Handles <see cref="ArchiveLookupCommand"/>.
/// </summary>
public sealed class ArchiveLookupCommandHandler : IRequestHandler<ArchiveLookupCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IReferenceDataCache _cache;
    private readonly ILogger<ArchiveLookupCommandHandler> _logger;

    /// <summary>Initializes dependencies.</summary>
    public ArchiveLookupCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IReferenceDataCache cache,
        ILogger<ArchiveLookupCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ArchiveLookupCommand request, CancellationToken cancellationToken)
    {
        // 1. Permission check
        if (!_currentUser.HasPermission(PermissionConstants.SettingsConfig))
        {
            throw new ForbiddenAccessException(PermissionConstants.SettingsConfig);
        }

        // 2. Company access check
        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result.Failure($"You do not have access to company ID {request.CompanyId}.");
        }

        // 3. Retrieve the entity
        var lookup = await _unitOfWork.MasterLookups.GetByIdAsync(request.Id, cancellationToken);
        if (lookup == null)
        {
            return Result.Failure($"Lookup item with ID {request.Id} was not found.");
        }

        if (lookup.CompanyId != request.CompanyId)
        {
            return Result.Failure("Company ID mismatch on lookup archive request.");
        }

        // 4. Archive entity
        lookup.Archive(_currentUser.Username ?? "system", request.Reason);

        // 5. Save and persist
        _unitOfWork.MasterLookups.Update(lookup);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Invalidate reference data cache
        _cache.InvalidateCategory(lookup.Category);

        _logger.LogInformation("Lookup item '{Code}' (ID: {Id}) in category '{Category}' archived for company {CompanyId} by {User}",
            lookup.Code, lookup.Id, lookup.Category, request.CompanyId, _currentUser.Username);

        return Result.Success();
    }
}
