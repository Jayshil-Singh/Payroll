using FijiPayroll.Application.Common.Models;
using MediatR;
using System.Collections.Generic;

namespace FijiPayroll.Application.Features.Auth.Queries.GetCompaniesForUser;

/// <summary>
/// DTO representing a simplified company lookup for the login workflow.
/// </summary>
public sealed record CompanyLookupDto(int Id, string LegalName, string TradingName);

/// <summary>
/// CQRS Query to retrieve all authorized companies for a given username.
/// </summary>
public sealed record GetCompaniesForUserQuery(string Username) : IRequest<Result<IReadOnlyList<CompanyLookupDto>>>;
