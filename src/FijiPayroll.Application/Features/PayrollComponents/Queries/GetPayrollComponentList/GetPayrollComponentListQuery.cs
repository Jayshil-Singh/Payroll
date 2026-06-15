using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Enumerations;
using MediatR;

namespace FijiPayroll.Application.Features.PayrollComponents.Queries.GetPayrollComponentList;

/// <summary>
/// Query to retrieve a filtered, paginated list of payroll components for a company.
/// Corresponds to the component list grid in Phase05-Configuration.md §9.1.
/// </summary>
/// <param name="CompanyId">The company to retrieve components for.</param>
/// <param name="SearchTerm">Optional text search on ComponentCode or ComponentName.</param>
/// <param name="ComponentTypeFilter">Optional filter by component type. Null = all types.</param>
/// <param name="ActiveOnly">When <c>true</c>, returns only active components. Default: <c>true</c>.</param>
/// <param name="PageNumber">1-based page number. Default: 1.</param>
/// <param name="PageSize">Records per page. Default: 25.</param>
public sealed record GetPayrollComponentListQuery(
    int CompanyId,
    string? SearchTerm = null,
    ComponentType? ComponentTypeFilter = null,
    bool ActiveOnly = true,
    int PageNumber = 1,
    int PageSize = 25
) : IRequest<Result<PagedResult<PayrollComponentSummaryDto>>>;
