using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Application.Features.PayrollComponents.Commands.DuplicatePayrollComponent;

/// <summary>
/// Handles <see cref="DuplicatePayrollComponentCommand"/>.
/// Clones all non-system-flag settings from the source component into a new entity.
/// </summary>
public sealed class DuplicatePayrollComponentCommandHandler
    : IRequestHandler<DuplicatePayrollComponentCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<DuplicatePayrollComponentCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public DuplicatePayrollComponentCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeService dateTime,
        ILogger<DuplicatePayrollComponentCommandHandler> logger)
    {
        _unitOfWork  = unitOfWork;
        _currentUser = currentUser;
        _dateTime    = dateTime;
        _logger      = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<int>> Handle(
        DuplicatePayrollComponentCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollComponentsCreate))
        {
            throw new ForbiddenAccessException(PermissionConstants.PayrollComponentsCreate);
        }

        var source = await _unitOfWork.PayrollComponents.GetByIdAsync(
            request.SourceId, cancellationToken);

        if (source is null)
        {
            throw new NotFoundException(nameof(Domain.Entities.Company.PayrollComponent), request.SourceId);
        }

        if (!_currentUser.HasCompanyAccess(source.CompanyId))
        {
            return Result<int>.Failure("You do not have access to the company owning this component.");
        }

        // Validate new code uniqueness
        var codeExists = await _unitOfWork.PayrollComponents.CodeExistsAsync(
            source.CompanyId, request.NewCode, excludeId: null, cancellationToken);

        if (codeExists)
        {
            return Result<int>.Failure(
                $"A component with code '{request.NewCode.ToUpperInvariant()}' already exists.");
        }

        // Clone via domain entity method
        var duplicate = source.Duplicate(request.NewCode, request.NewName);
        duplicate.CreatedBy = _currentUser.Username;
        duplicate.CreatedAt = _dateTime.UtcNow;

        await _unitOfWork.PayrollComponents.AddAsync(duplicate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payroll component '{Code}' cloned from ID {SourceId} by {User}. New ID: {NewId}",
            duplicate.ComponentCode, request.SourceId, _currentUser.Username, duplicate.Id);

        return Result<int>.Success(duplicate.Id);
    }
}
