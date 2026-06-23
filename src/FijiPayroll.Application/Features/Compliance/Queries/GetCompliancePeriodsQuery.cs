using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using MediatR;

namespace FijiPayroll.Application.Features.Compliance.Queries;

/// <summary>
/// CQRS Query to retrieve a list of all compliance periods for a company.
/// </summary>
public sealed record GetCompliancePeriodsQuery(int CompanyId) : IRequest<Result<IReadOnlyList<CompliancePeriod>>>;

/// <summary>
/// Handles GetCompliancePeriodsQuery.
/// </summary>
public sealed class GetCompliancePeriodsQueryHandler : IRequestHandler<GetCompliancePeriodsQuery, Result<IReadOnlyList<CompliancePeriod>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCompliancePeriodsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<CompliancePeriod>>> Handle(GetCompliancePeriodsQuery request, CancellationToken cancellationToken)
    {
        var periods = await _unitOfWork.Compliance.GetPeriodsByCompanyAsync(request.CompanyId, cancellationToken);
        return Result<IReadOnlyList<CompliancePeriod>>.Success(periods);
    }
}
