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
    /// Creates a new payroll component.
    /// </summary>
    /// <param name="companyId">The owning company identifier.</param>
    /// <param name="componentCode">Unique uppercase code. Max 20 chars.</param>
    /// <param name="componentName">Human-readable name. Max 200 chars.</param>
    /// <param name="componentType">Classification: Earning, Deduction, Allowance, Benefit, Statutory.</param>
    /// <param name="calculationMethod">Fixed, Percentage, Formula, or Manual.</param>
    /// <param name="calculationValue">Dollar amount or % value; null for Formula/Manual.</param>
    /// <param name="formula">Formula expression; null for non-Formula methods.</param>
    /// <param name="isTaxable">Whether this component contributes to PAYE taxable income.</param>
    /// <param name="isFnpfApplicable">Whether this component contributes to FNPF-applicable gross.</param>
    /// <param name="displayOrder">Non-negative sort order on payslips.</param>
    /// <param name="description">Optional internal description. Max 500 chars.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the new component's ID.</returns>
    Task<Result<int>> CreateAsync(
        int companyId,
        string componentCode,
        string componentName,
        ComponentType componentType,
        CalculationMethod calculationMethod,
        decimal? calculationValue,
        string? formula,
        bool isTaxable,
        bool isFnpfApplicable,
        int displayOrder,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing payroll component.
    /// System components restrict changes to name and display order only.
    /// </summary>
    /// <param name="id">Primary key of the component to update.</param>
    /// <param name="componentName">Updated display name. Max 200 chars.</param>
    /// <param name="componentType">Updated classification (restricted for system components).</param>
    /// <param name="calculationMethod">Updated calculation method (restricted for system components).</param>
    /// <param name="calculationValue">Updated dollar amount or percentage.</param>
    /// <param name="formula">Updated formula expression.</param>
    /// <param name="isTaxable">Updated taxability flag.</param>
    /// <param name="isFnpfApplicable">Updated FNPF applicability flag.</param>
    /// <param name="displayOrder">Updated display order.</param>
    /// <param name="description">Updated description. Max 500 chars.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result representing success or failure.</returns>
    Task<Result> UpdateAsync(
        int id,
        string componentName,
        ComponentType componentType,
        CalculationMethod calculationMethod,
        decimal? calculationValue,
        string? formula,
        bool isTaxable,
        bool isFnpfApplicable,
        int displayOrder,
        string? description,
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
