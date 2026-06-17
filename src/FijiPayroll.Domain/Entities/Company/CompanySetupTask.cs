using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using System;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing a normalized, queryable, and auditable setup task inside onboarding workflows.
/// </summary>
public sealed class CompanySetupTask : SoftDeleteEntity
{
    private CompanySetupTask() { }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the parent setup state identifier.</summary>
    public int CompanySetupStateId { get; private set; }

    /// <summary>Gets the specific wizard step represented by this task.</summary>
    public WizardStep Step { get; private set; }

    /// <summary>Gets a value indicating whether this task was successfully completed.</summary>
    public bool Completed { get; private set; }

    /// <summary>Gets the timestamp when this task was marked completed.</summary>
    public DateTime? CompletedUtc { get; private set; }

    /// <summary>Gets the username of the user who marked this task completed.</summary>
    public string CompletedBy { get; private set; } = string.Empty;

    /// <summary>Gets the version of the task layout used.</summary>
    public string Version { get; private set; } = "1.0.0";

    /// <summary>Factory method to create a new CompanySetupTask.</summary>
    public static CompanySetupTask Create(
        int companyId,
        int setupStateId,
        WizardStep step,
        string completedBy,
        string version = "1.0.0")
    {
        if (companyId <= 0)
            throw new ArgumentException("Company ID must be positive.", nameof(companyId));
        if (setupStateId <= 0)
            throw new ArgumentException("Setup State ID must be positive.", nameof(setupStateId));
        if (string.IsNullOrWhiteSpace(completedBy))
            throw new ArgumentException("Completed by username is required.", nameof(completedBy));

        return new CompanySetupTask
        {
            CompanyId = companyId,
            CompanySetupStateId = setupStateId,
            Step = step,
            Completed = true,
            CompletedUtc = DateTime.UtcNow,
            CompletedBy = completedBy,
            Version = version
        };
    }
}
