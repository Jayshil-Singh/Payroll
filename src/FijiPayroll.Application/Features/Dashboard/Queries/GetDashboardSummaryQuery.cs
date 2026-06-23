using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Dashboard.Queries;

public sealed class DashboardSummaryDto
{
    public int ActiveEmployeesCount { get; set; }
    public int TerminatedThisMonthCount { get; set; }
    public int OpenPeriodsCount { get; set; }
    public string CurrentPeriodCode { get; set; } = "N/A";
    public string CurrentRunStatus { get; set; } = "No Active Run";
    public int PostedRunsCount { get; set; }
    public decimal LatestGrossPay { get; set; }
    public decimal LatestPAYETax { get; set; }
    public decimal LatestFNPFEmployee { get; set; }
    public decimal LatestFNPFEmployer { get; set; }
    public decimal LatestNetPay { get; set; }

    public List<string> SystemAlerts { get; set; } = new();
    public List<HistoricRunDto> RecentRuns { get; set; } = new();
}

public sealed class HistoricRunDto
{
    public string PeriodCode { get; set; } = string.Empty;
    public decimal GrossPay { get; set; }
    public decimal PAYETax { get; set; }
    public decimal NetPay { get; set; }
    public DateTime PaymentDate { get; set; }
}

public sealed record GetDashboardSummaryQuery(int CompanyId) : IRequest<Result<DashboardSummaryDto>>;

public sealed class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDashboardSummaryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var dto = new DashboardSummaryDto();

            dto.ActiveEmployeesCount = await _unitOfWork.Dashboard.GetActiveEmployeesCountAsync(request.CompanyId, cancellationToken);
            dto.TerminatedThisMonthCount = await _unitOfWork.Dashboard.GetTerminatedThisMonthCountAsync(request.CompanyId, cancellationToken);
            dto.OpenPeriodsCount = await _unitOfWork.Dashboard.GetOpenPeriodsCountAsync(request.CompanyId, cancellationToken);
            dto.CurrentPeriodCode = await _unitOfWork.Dashboard.GetCurrentPeriodNameAsync(request.CompanyId, cancellationToken);
            dto.CurrentRunStatus = await _unitOfWork.Dashboard.GetCurrentRunStatusAsync(request.CompanyId, cancellationToken);
            dto.PostedRunsCount = await _unitOfWork.Dashboard.GetPostedRunsCountAsync(request.CompanyId, cancellationToken);

            var (gross, paye, fnpfEmp, fnpfEr, net) = await _unitOfWork.Dashboard.GetLatestPostedTotalsAsync(request.CompanyId, cancellationToken);
            dto.LatestGrossPay = gross;
            dto.LatestPAYETax = paye;
            dto.LatestFNPFEmployee = fnpfEmp;
            dto.LatestFNPFEmployer = fnpfEr;
            dto.LatestNetPay = net;

            var alerts = await _unitOfWork.Dashboard.GetSystemAlertsAsync(request.CompanyId, cancellationToken);
            dto.SystemAlerts = alerts.ToList();

            var recent = await _unitOfWork.Dashboard.GetRecentRunsAsync(request.CompanyId, 3, cancellationToken);
            dto.RecentRuns = recent.Select(x => new HistoricRunDto
            {
                PeriodCode = x.PeriodName,
                GrossPay = x.GrossPay,
                PAYETax = x.PAYETax,
                NetPay = x.NetPay,
                PaymentDate = x.PaymentDate
            }).ToList();

            return Result<DashboardSummaryDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<DashboardSummaryDto>.Failure($"Failed to retrieve dashboard summary: {ex.Message}");
        }
    }
}
