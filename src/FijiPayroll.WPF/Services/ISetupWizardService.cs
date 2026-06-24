using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Service interface for first-time installation and guided onboarding Setup Wizard.
/// </summary>
public interface ISetupWizardService
{
    /// <summary>
    /// Checks if setup is completed. Returns false if the database is unconfigured,
    /// inaccessible, or if no completed company setup state exists.
    /// </summary>
    Task<bool> IsSetupCompletedAsync();

    /// <summary>
    /// Tests database connectivity with a given connection string.
    /// </summary>
    Task<bool> TestConnectionAsync(string connectionString);

    /// <summary>
    /// Saves the database connection string to appsettings.json.
    /// </summary>
    Task SaveConnectionStringAsync(string connectionString);

    /// <summary>
    /// Runs Entity Framework migrations and seeds standard reference data.
    /// </summary>
    Task RunMigrationsAndSeedAsync();

    /// <summary>
    /// Completes the guided wizard onboarding setup and commits all configurations.
    /// </summary>
    Task CompleteSetupAsync(SetupWizardData data);

    /// <summary>
    /// Parses an employee Excel or CSV file for wizard import.
    /// </summary>
    Task<List<SetupWizardEmployeeImport>> ParseEmployeesAsync(string filePath);
}

public sealed class SetupWizardData
{
    public SetupWizardCompanyData Company { get; set; } = new();
    public SetupWizardCalendarData Calendar { get; set; } = new();
    public SetupWizardDefaultsData Defaults { get; set; } = new();
    public bool UseDefaultComponents { get; set; } = true;
    public List<SetupWizardLeavePolicy> LeavePolicies { get; set; } = new();
    public List<SetupWizardEmployeeImport> Employees { get; set; } = new();
    public SetupWizardUserData Administrator { get; set; } = new();
}

public sealed class SetupWizardCompanyData
{
    public string CompanyName { get; set; } = string.Empty;
    public string TradingName { get; set; } = string.Empty;
    public string TIN { get; set; } = string.Empty;
    public string FnpfNumber { get; set; } = string.Empty;
    public string BusinessRegistrationNumber { get; set; } = string.Empty;
    public string PhysicalAddress { get; set; } = string.Empty;
    public string PostalAddress { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
}

public sealed class SetupWizardCalendarData
{
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1).AddDays(-1);
    public string PayrollFrequency { get; set; } = "Monthly"; // Weekly, Fortnightly, Monthly, Bi-Monthly
    public DateTime? FirstPayrollDate { get; set; }
}

public sealed class SetupWizardDefaultsData
{
    public string DefaultPayFrequency { get; set; } = "Monthly";
    public string Currency { get; set; } = "FJD";
    public int WorkingDaysPerWeek { get; set; } = 5;
    public decimal StandardHoursPerWeek { get; set; } = 40.0m;
    public string NegativeNetPayPolicy { get; set; } = "PartialDeduction"; // BlockDeduction, PartialDeduction, AllowNegativeNetPay
}

public sealed class SetupWizardLeavePolicy
{
    public string LeaveTypeName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // e.g. AnnualLeave
    public decimal EntitlementDays { get; set; }
    public string AccrualMethod { get; set; } = "Standard";
    public decimal MaxCarryOverDays { get; set; }
    public decimal MaximumBalance { get; set; }
}

public sealed class SetupWizardEmployeeImport
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string TIN { get; set; } = string.Empty;
    public string Fnpf { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal PayRate { get; set; }
}

public sealed class SetupWizardUserData
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
