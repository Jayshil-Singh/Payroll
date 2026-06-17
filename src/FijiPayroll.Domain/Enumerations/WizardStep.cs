namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the specific steps of the guided onboarding Company Setup Wizard.
/// </summary>
public enum WizardStep
{
    /// <summary>The introductory/welcome step of the onboarding setup.</summary>
    Welcome = 1,

    /// <summary>Configuration of basic company identity, TIN, and contact details.</summary>
    CompanyDetails = 2,

    /// <summary>Generation and validation of fiscal calendar year limits and periods.</summary>
    FiscalCalendar = 3,

    /// <summary>Setting up pay frequencies, cutoffs, and payment schedules.</summary>
    PayrollFrequency = 4,

    /// <summary>Configuration of company operating and payroll bank accounts.</summary>
    BankConfiguration = 5,

    /// <summary>Assignment of user security roles to approval verification lanes.</summary>
    Approvers = 6,

    /// <summary>Dry-run system pre-validation checklist and compliance verification.</summary>
    Validation = 7,

    /// <summary>Wizard completion and initialization of default dashboard configurations.</summary>
    Completed = 8
}
