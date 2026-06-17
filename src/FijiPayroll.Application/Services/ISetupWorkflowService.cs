using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Service interface for managing the guided Company Setup Wizard onboarding workflow.
/// </summary>
public interface ISetupWorkflowService
{
    /// <summary>
    /// Retrieves the current wizard setup state of a company, creating it if it doesn't exist.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task returning the current CompanySetupState.</returns>
    Task<CompanySetupState> GetOrCreateSetupStateAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all completed setup tasks for a company setup wizard.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task returning a read-only list of completed CompanySetupTask.</returns>
    Task<IReadOnlyList<CompanySetupTask>> GetSetupTasksAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes the specified step in the guided setup wizard, transitioning to the next step.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="step">The step to mark as completed.</param>
    /// <param name="completedBy">The username of the administrator who completed the step.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task returning a Result indicating success or list of errors.</returns>
    Task<Result> CompleteStepAsync(int companyId, WizardStep step, string completedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Skips the specified wizard step.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="step">The step to skip.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task returning a Result indicating success or list of errors.</returns>
    Task<Result> SkipStepAsync(int companyId, WizardStep step, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the company setup wizard state to the Welcome step and clears all completed tasks.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task returning a Result indicating success or list of errors.</returns>
    Task<Result> ResetSetupAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a comprehensive dry-run validation checks checklist across all wizard steps.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task returning a ValidationResultDto containing validation results.</returns>
    Task<ValidationResultDto> ValidateSetupAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the company details by identifier.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task returning the Company entity, or null if not found.</returns>
    Task<Company?> GetCompanyAsync(int companyId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Data Transfer Object representing dry-run wizard validation checklist results.
/// </summary>
public sealed record ValidationResultDto
{
    /// <summary>
    /// Initialises a new instance of the <see cref="ValidationResultDto"/> record.
    /// </summary>
    /// <param name="isValid">True if all requirements are satisfied.</param>
    /// <param name="errors">The list of blocking error messages.</param>
    /// <param name="warnings">The list of non-blocking warning messages.</param>
    public ValidationResultDto(bool isValid, IReadOnlyList<string> errors, IReadOnlyList<string> warnings)
    {
        IsValid = isValid;
        Errors = errors;
        Warnings = warnings;
    }

    /// <summary>
    /// Gets a value indicating whether the wizard is in a valid state for finalization.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the list of blocking error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; }

    /// <summary>
    /// Gets the list of non-blocking warning messages.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; }
}
