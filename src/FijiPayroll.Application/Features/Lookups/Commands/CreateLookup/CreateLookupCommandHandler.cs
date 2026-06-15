using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Lookups.Commands.CreateLookup;

/// <summary>
/// Handles <see cref="CreateLookupCommand"/>.
/// </summary>
public sealed class CreateLookupCommandHandler : IRequestHandler<CreateLookupCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IReferenceDataCache _cache;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<CreateLookupCommandHandler> _logger;

    /// <summary>Initializes dependencies.</summary>
    public CreateLookupCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IReferenceDataCache cache,
        IDateTimeService dateTime,
        ILogger<CreateLookupCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _cache = cache;
        _dateTime = dateTime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<int>> Handle(CreateLookupCommand request, CancellationToken cancellationToken)
    {
        // 1. Permission check
        if (!_currentUser.HasPermission(PermissionConstants.SettingsConfig))
        {
            throw new ForbiddenAccessException(PermissionConstants.SettingsConfig);
        }

        // 2. Company access check
        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<int>.Failure($"You do not have access to company ID {request.CompanyId}.");
        }

        // 3. Unique code check
        bool codeExists = await _unitOfWork.MasterLookups.CodeExistsAsync(
            request.CompanyId,
            request.Category,
            request.Code,
            excludeId: null,
            cancellationToken);

        if (codeExists)
        {
            return Result<int>.Failure($"A lookup item with code '{request.Code.ToUpperInvariant()}' already exists in category '{request.Category.ToUpperInvariant()}' for this company.");
        }

        // 4. Create entity
        MasterLookup lookup;
        try
        {
            lookup = MasterLookup.Create(
                companyId: request.CompanyId,
                category: request.Category,
                code: request.Code,
                name: request.Name,
                effectiveFrom: request.EffectiveFrom,
                effectiveTo: request.EffectiveTo,
                parentId: request.ParentId,
                displayOrder: request.DisplayOrder,
                isActive: request.IsActive);
        }
        catch (Exception ex) when (ex is FijiPayroll.Domain.Exceptions.DomainException || ex is ArgumentException)
        {
            _logger.LogWarning(ex, "Domain rule violation creating lookup.");
            return Result<int>.Failure(ex.Message);
        }

        // Set audit fields
        lookup.CreatedBy = _currentUser.Username;
        lookup.CreatedAt = _dateTime.UtcNow;

        // 5. Save and persist
        await _unitOfWork.MasterLookups.AddAsync(lookup, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Invalidate reference data cache
        _cache.InvalidateCategory(request.Category);

        _logger.LogInformation("Lookup item '{Code}' (ID: {Id}) in category '{Category}' created for company {CompanyId} by {User}",
            lookup.Code, lookup.Id, lookup.Category, request.CompanyId, _currentUser.Username);

        return Result<int>.Success(lookup.Id);
    }
}
