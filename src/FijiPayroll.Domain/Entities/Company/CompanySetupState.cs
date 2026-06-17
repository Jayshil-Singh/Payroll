using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using System;
using System.Collections.Generic;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing the active wizard state of a company onboarding process.
/// Enforces uniqueness constraints (only one active setup state per tenant).
/// </summary>
public sealed class CompanySetupState : SoftDeleteEntity
{
    private string _wizardVersion = "1.0.0";
    private readonly List<CompanySetupTask> _tasks = new();

    private CompanySetupState() { }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the current step of the guided setup wizard.</summary>
    public WizardStep CurrentStep { get; private set; } = WizardStep.Welcome;

    /// <summary>Gets a value indicating whether this setup workflow has finished.</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>Gets the version of the wizard layout deployed during this onboarding.</summary>
    public string WizardVersion
    {
        get => _wizardVersion;
        private set => _wizardVersion = value ?? "1.0.0";
    }

    /// <summary>Gets the list of individual setup tasks executed during onboarding.</summary>
    public IReadOnlyCollection<CompanySetupTask> Tasks => _tasks.AsReadOnly();

    /// <summary>Factory method to create a new CompanySetupState.</summary>
    public static CompanySetupState Create(int companyId, string wizardVersion = "1.0.0")
    {
        if (companyId <= 0)
            throw new ArgumentException("Company ID must be a positive integer.", nameof(companyId));

        return new CompanySetupState
        {
            CompanyId = companyId,
            CurrentStep = WizardStep.Welcome,
            IsCompleted = false,
            WizardVersion = wizardVersion
        };
    }

    /// <summary>Transitions the onboarding state to the specified wizard step.</summary>
    public void TransitionToStep(WizardStep nextStep)
    {
        if (IsCompleted)
            throw new InvalidOperationException("SETUP_ERROR: Cannot transition steps on a completed setup workflow.");

        CurrentStep = nextStep;
    }

    /// <summary>Adds or registers a task configuration status checkpoint.</summary>
    public void AssociateTask(CompanySetupTask task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));
        _tasks.Add(task);
    }

    /// <summary>Marks this setup state as finalized.</summary>
    public void CompleteSetup()
    {
        IsCompleted = true;
        CurrentStep = WizardStep.Completed;
    }
}
