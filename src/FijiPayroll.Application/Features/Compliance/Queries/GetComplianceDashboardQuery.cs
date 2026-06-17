using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using MediatR;

namespace FijiPayroll.Application.Features.Compliance.Queries;

/// <summary>Represents compliance dashboard details.</summary>
public sealed record ComplianceDashboardModel(
    string ActivePeriodName,
    string ActivePeriodStatus,
    int OutstandingValidationErrorsCount,
    List<SubmissionSummary> RecentSubmissions
);

/// <summary>Represents a summary representation of submission documents.</summary>
public sealed record SubmissionSummary(
    int Id,
    string DocumentType,
    string Status,
    DateTime CreatedDate,
    string PinnedRulesVersion
);

/// <summary>
/// CQRS Query to retrieve summary dashboard statistics for the compliance management view.
/// </summary>
public sealed record GetComplianceDashboardQuery(int CompanyId) : IRequest<Result<ComplianceDashboardModel>>;

/// <summary>
/// Handles GetComplianceDashboardQuery.
/// </summary>
public sealed class GetComplianceDashboardQueryHandler : IRequestHandler<GetComplianceDashboardQuery, Result<ComplianceDashboardModel>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetComplianceDashboardQueryHandler"/> class.
    /// </summary>
    public GetComplianceDashboardQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc/>
    public async Task<Result<ComplianceDashboardModel>> Handle(GetComplianceDashboardQuery request, CancellationToken cancellationToken)
    {
        var activePeriod = await _unitOfWork.Compliance.GetActivePeriodAsync(request.CompanyId, cancellationToken);
        
        string activePeriodName = activePeriod != null 
            ? $"{new DateTime(activePeriod.Year, activePeriod.Month, 1):MMMM yyyy}" 
            : "No active period";
        
        string activePeriodStatus = activePeriod != null ? activePeriod.Status.ToString() : "N/A";

        // Query submissions via repository to maintain Clean Architecture limits
        var frcsSubmissions = await _unitOfWork.Compliance.GetRecentFRCSSubmissionsAsync(request.CompanyId, 5, cancellationToken);
        var recentFrcs = frcsSubmissions
            .Select(x => new SubmissionSummary(x.Id, "FRCS MER", x.Status.ToString(), x.CreatedAt, x.PinnedRuleVersion))
            .ToList();

        var fnpfSubmissions = await _unitOfWork.Compliance.GetRecentFNPFSubmissionsAsync(request.CompanyId, 5, cancellationToken);
        var recentFnpf = fnpfSubmissions
            .Select(x => new SubmissionSummary(x.Id, "FNPF Remittance", x.Status.ToString(), x.CreatedAt, x.PinnedRuleVersion))
            .ToList();

        var bankFiles = await _unitOfWork.Compliance.GetRecentBankFilesAsync(request.CompanyId, 5, cancellationToken);
        var recentBanks = bankFiles
            .Select(x => new SubmissionSummary(x.Id, $"{x.BankCode} Clearing", "Generated", x.CreatedAt, "N/A"))
            .ToList();

        var recentSubmissions = recentFrcs.Concat(recentFnpf).Concat(recentBanks)
            .OrderByDescending(x => x.CreatedDate)
            .Take(5)
            .ToList();

        int employeeErrorsCount = await _unitOfWork.Compliance.GetEmployeeMissingDetailsCountAsync(request.CompanyId, cancellationToken);

        var dashboard = new ComplianceDashboardModel(
            ActivePeriodName: activePeriodName,
            ActivePeriodStatus: activePeriodStatus,
            OutstandingValidationErrorsCount: employeeErrorsCount,
            RecentSubmissions: recentSubmissions
        );

        return Result<ComplianceDashboardModel>.Success(dashboard);
    }
}
