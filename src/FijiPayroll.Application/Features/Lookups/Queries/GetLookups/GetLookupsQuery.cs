using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Lookups.Queries.GetLookups;

/// <summary>
/// CQRS Query to retrieve master lookups for a specific category filtered by tenant company context.
/// </summary>
public sealed record GetLookupsQuery(string Category) : IRequest<Result<IReadOnlyList<MasterLookup>>>;

/// <summary>
/// Handles GetLookupsQuery.
/// </summary>
public sealed class GetLookupsQueryHandler : IRequestHandler<GetLookupsQuery, Result<IReadOnlyList<MasterLookup>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    /// <summary>Initializes handler dependencies.</summary>
    public GetLookupsQueryHandler(IUnitOfWork unitOfWork, ITenantProvider tenantProvider)
    {
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<MasterLookup>>> Handle(GetLookupsQuery request, CancellationToken cancellationToken)
    {
        int tenantId = _tenantProvider.GetCurrentCompanyId();
        string category = request.Category.ToUpperInvariant();

        var list = await _unitOfWork.MasterLookups
            .GetByCategoryAsync(tenantId, category, cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<MasterLookup>>.Success(list);
    }
}
