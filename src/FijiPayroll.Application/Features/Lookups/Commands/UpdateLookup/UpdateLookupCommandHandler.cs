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

namespace FijiPayroll.Application.Features.Lookups.Commands.UpdateLookup;

/// <summary>
/// Handles <see cref="UpdateLookupCommand"/>.
/// </summary>
public sealed class UpdateLookupCommandHandler : IRequestHandler<UpdateLookupCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IReferenceDataCache _cache;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<UpdateLookupCommandHandler> _logger;

    /// <summary>Initializes dependencies.</summary>
    public UpdateLookupCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IReferenceDataCache cache,
        IDateTimeService dateTime,
        ILogger<UpdateLookupCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _cache = cache;
        _dateTime = dateTime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateLookupCommand request, CancellationToken cancellationToken)
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
            return Result.Failure("Company ID mismatch on lookup update request.");
        }

        // 4. Update the entity properties
        try
        {
            lookup.Update(
                name: request.Name,
                effectiveFrom: request.EffectiveFrom,
                effectiveTo: request.EffectiveTo,
                parentId: request.ParentId,
                displayOrder: request.DisplayOrder,
                isActive: request.IsActive);
        }
        catch (Exception ex) when (ex is FijiPayroll.Domain.Exceptions.DomainException || ex is ArgumentException)
        {
            _logger.LogWarning(ex, "Domain rule violation updating lookup.");
            return Result.Failure(ex.Message);
        }

        // Set audit fields
        lookup.ModifiedBy = _currentUser.Username;
        lookup.ModifiedAt = _dateTime.UtcNow;

        // 5. Save and persist
        _unitOfWork.MasterLookups.Update(lookup);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Invalidate reference data cache
        _cache.InvalidateCategory(lookup.Category);

        _logger.LogInformation("Lookup item '{Code}' (ID: {Id}) in category '{Category}' updated for company {CompanyId} by {User}",
            lookup.Code, lookup.Id, lookup.Category, request.CompanyId, _currentUser.Username);

        return Result.Success();
    }
}
