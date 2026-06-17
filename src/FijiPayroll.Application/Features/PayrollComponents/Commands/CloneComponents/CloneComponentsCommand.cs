using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollComponents.Commands.CloneComponents;

/// <summary>
/// Command to clone payroll components from a source company to a target company.
/// Supports Merge, Replace, or Skip modes.
/// </summary>
public sealed record CloneComponentsCommand(
    int SourceCompanyId,
    int TargetCompanyId,
    string Mode, // "Merge", "Replace", "Skip"
    IReadOnlyList<int> ComponentIds
) : IRequest<Result<CloneReconciliationSummary>>;

public sealed class CloneReconciliationSummary
{
    public int ClonedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ReplacedCount { get; set; }
    public List<string> LogMessages { get; set; } = new();
}

public sealed class CloneComponentsCommandHandler
    : IRequestHandler<CloneComponentsCommand, Result<CloneReconciliationSummary>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<CloneComponentsCommandHandler> _logger;

    public CloneComponentsCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeService dateTime,
        ILogger<CloneComponentsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task<Result<CloneReconciliationSummary>> Handle(
        CloneComponentsCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollComponentsCreate))
        {
            throw new ForbiddenAccessException(PermissionConstants.PayrollComponentsCreate);
        }

        if (!_currentUser.HasCompanyAccess(request.SourceCompanyId) ||
            !_currentUser.HasCompanyAccess(request.TargetCompanyId))
        {
            return Result<CloneReconciliationSummary>.Failure("Unauthorized: You do not have access to both source and target companies.");
        }

        var summary = new CloneReconciliationSummary();

        // 1. Fetch source components
        var allSourceComponents = await _unitOfWork.PayrollComponents.GetPagedAsync(
            request.SourceCompanyId, searchTerm: null, typeFilter: null, activeOnly: false, pageNumber: 1, pageSize: 1000, cancellationToken: cancellationToken);

        var componentsToClone = allSourceComponents.Items;
        if (request.ComponentIds != null && request.ComponentIds.Count > 0)
        {
            componentsToClone = componentsToClone.Where(c => request.ComponentIds.Contains(c.Id)).ToList();
        }

        // 2. Fetch target components for conflict check
        var targetComponents = await _unitOfWork.PayrollComponents.GetPagedAsync(
            request.TargetCompanyId, searchTerm: null, typeFilter: null, activeOnly: false, pageNumber: 1, pageSize: 1000, cancellationToken: cancellationToken);

        var targetMap = targetComponents.Items.ToDictionary(c => c.ComponentCode, StringComparer.OrdinalIgnoreCase);

        foreach (var source in componentsToClone)
        {
            if (targetMap.TryGetValue(source.ComponentCode, out var conflictTarget))
            {
                if (request.Mode.Equals("Skip", StringComparison.OrdinalIgnoreCase))
                {
                    summary.SkippedCount++;
                    summary.LogMessages.Add($"Skipped: Component '{source.ComponentCode}' already exists in target company.");
                    continue;
                }

                if (request.Mode.Equals("Replace", StringComparison.OrdinalIgnoreCase))
                {
                    // Update/replace properties
                    conflictTarget.Update(
                        source.ComponentName,
                        source.ComponentType,
                        source.CalculationMethod,
                        source.CalculationValue,
                        source.Formula,
                        source.IsTaxable,
                        source.IsFnpfApplicable,
                        source.DisplayOrder,
                        source.Description
                    );
                    conflictTarget.ModifiedBy = _currentUser.Username;
                    conflictTarget.ModifiedAt = _dateTime.UtcNow;
                    summary.ReplacedCount++;
                    summary.LogMessages.Add($"Replaced: Component '{source.ComponentCode}' properties overridden in target company.");
                }
                else // "Merge"
                {
                    summary.LogMessages.Add($"Merge conflict: Component '{source.ComponentCode}' already exists. Skipping duplicate.");
                    summary.SkippedCount++;
                }
            }
            else
            {
                // Create a clone
                var clone = PayrollComponent.Create(
                    companyId: request.TargetCompanyId,
                    componentCode: source.ComponentCode,
                    componentName: source.ComponentName,
                    componentType: source.ComponentType,
                    calculationMethod: source.CalculationMethod,
                    calculationValue: source.CalculationValue,
                    formula: source.Formula,
                    isTaxable: source.IsTaxable,
                    isFnpfApplicable: source.IsFnpfApplicable,
                    displayOrder: source.DisplayOrder,
                    description: source.Description,
                    isSystemComponent: source.IsSystemComponent
                );
                clone.CreatedBy = _currentUser.Username;
                clone.CreatedAt = _dateTime.UtcNow;

                await _unitOfWork.PayrollComponents.AddAsync(clone, cancellationToken);
                summary.ClonedCount++;
                summary.LogMessages.Add($"Cloned: Component '{source.ComponentCode}' created in target company.");
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CloneReconciliationSummary>.Success(summary);
    }
}
