using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollRunList;

public sealed record GetPayrollRunListQuery(
    int CompanyId,
    PayrollFrequency? FrequencyFilter,
    PayrollRunStatus? StatusFilter,
    int PageNumber,
    int PageSize
) : IRequest<Result<PagedResult<PayrollRunSummaryDto>>>;

public sealed class GetPayrollRunListQueryHandler 
    : IRequestHandler<GetPayrollRunListQuery, Result<PagedResult<PayrollRunSummaryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetPayrollRunListQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<PayrollRunSummaryDto>>> Handle(
        GetPayrollRunListQuery request, 
        CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsView))
        {
            return Result<PagedResult<PayrollRunSummaryDto>>.Failure("Forbidden: You do not have permission to view payroll runs.");
        }

        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<PagedResult<PayrollRunSummaryDto>>.Failure("Forbidden: You do not have access to this company.");
        }

        var (runs, totalCount) = await _unitOfWork.PayrollRuns.GetPagedAsync(
            request.CompanyId,
            request.FrequencyFilter,
            request.StatusFilter,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = runs.Select(r => new PayrollRunSummaryDto
        {
            Id = r.Id,
            RunCode = r.RunCode,
            PeriodName = r.PeriodName,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            PaymentDate = r.PaymentDate,
            Frequency = r.Frequency,
            Status = r.Status,
            Description = r.Description
        }).ToList();

        var pagedResult = new PagedResult<PayrollRunSummaryDto>(
            dtos.AsReadOnly(),
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Result<PagedResult<PayrollRunSummaryDto>>.Success(pagedResult);
    }
}
