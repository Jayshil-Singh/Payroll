using FijiPayroll.Domain.Entities.Common;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing settings for a company tenant.
/// </summary>
public sealed class SystemSettings : BaseEntity
{
    private SystemSettings() { } // For EF Core

    public int CompanyId { get; private set; }

    // Payroll Default Configurations
    public string DefaultPayFrequency { get; set; } = "Weekly";
    public string DefaultPayrollCalendar { get; set; } = "Standard 2026";
    public string NegativePayPolicy { get; set; } = "PartialDeduction";

    // Compliance Defaults
    public string DefaultSubmissionPaths { get; set; } = "C:\\FijiPayroll\\Submissions";

    // System Directories
    public string BackupDirectory { get; set; } = "C:\\FijiPayroll\\Backups";
    public string ExportDirectory { get; set; } = "C:\\FijiPayroll\\Exports";
    public string ImportDirectory { get; set; } = "C:\\FijiPayroll\\Imports";

    // Email SMTP Configurations
    public string SmtpHost { get; set; } = "smtp.mailtrap.io";
    public int SmtpPort { get; set; } = 2525;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool SmtpSslEnabled { get; set; } = true;

    /// <summary>
    /// Factory method to construct dynamic system settings.
    /// </summary>
    public static SystemSettings Create(int companyId)
    {
        return new SystemSettings
        {
            CompanyId = companyId
        };
    }
}
