using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.PayrollComponents.Queries.GetPayrollComponentById;
using FijiPayroll.Application.Features.PayrollComponents.Queries.GetPayrollComponentList;
using FijiPayroll.Domain.Enumerations;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Application service interface for managing payroll components.
/// Acts as the orchestration layer between WPF ViewModels and MediatR CQRS handlers.
/// </summary>
public interface IPayrollComponentService
{
    /// <summary>
    /// Retrieves a detailed view of a payroll component by ID.
    /// </summary>
    /// <param name="id">The component primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the component details.</returns>
    Task<Result<PayrollComponentDetailDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a filtered, paginated list of payroll component summaries.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="searchTerm">Optional search text.</param>
    /// <param name="typeFilter">Optional component type filter.</param>
    /// <param name="activeOnly">Whether to return active components only.</param>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the paginated summaries.</returns>
    Task<Result<PagedResult<PayrollComponentSummaryDto>>> GetListAsync(
        int companyId,
        string? searchTerm = null,
        ComponentType? typeFilter = null,
        bool activeOnly = true,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates or deactivates a payroll component.
    /// </summary>
    /// <param name="id">The component identifier.</param>
    /// <param name="setActive">Whether to activate or deactivate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result representing success or failure.</returns>
    Task<Result> ToggleActiveAsync(
        int id,
        bool setActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a duplicate clone of a payroll component.
    /// </summary>
    /// <param name="sourceId">The source component ID to clone.</param>
    /// <param name="newCode">Alphanumeric code for the new component.</param>
    /// <param name="newName">Display name for the new component.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the new component's ID.</returns>
    Task<Result<int>> DuplicateAsync(
        int sourceId,
        string newCode,
        string newName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a payroll component.
    /// </summary>
    /// <param name="id">The component identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result representing success or failure.</returns>
    Task<Result> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}
