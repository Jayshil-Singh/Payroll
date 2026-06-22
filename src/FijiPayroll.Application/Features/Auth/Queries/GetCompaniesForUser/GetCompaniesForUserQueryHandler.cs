using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Auth.Queries.GetCompaniesForUser;

/// <summary>
/// Query handler for resolving companies associated with a username.
/// </summary>
public sealed class GetCompaniesForUserQueryHandler : IRequestHandler<GetCompaniesForUserQuery, Result<IReadOnlyList<CompanyLookupDto>>>
{
    private readonly IUserRepository _userRepository;

    public GetCompaniesForUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<IReadOnlyList<CompanyLookupDto>>> Handle(GetCompaniesForUserQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return Result<IReadOnlyList<CompanyLookupDto>>.Failure("Username is required.");
        }

        try
        {
            var companies = await _userRepository.GetCompaniesByUsernameAsync(request.Username, cancellationToken);
            var dtos = companies
                .Select(c => new CompanyLookupDto(c.Id, c.LegalName, string.IsNullOrWhiteSpace(c.TradingName) ? c.LegalName : c.TradingName))
                .ToList();

            return Result<IReadOnlyList<CompanyLookupDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CompanyLookupDto>>.Failure($"Failed to retrieve user companies: {ex.Message}");
        }
    }
}
