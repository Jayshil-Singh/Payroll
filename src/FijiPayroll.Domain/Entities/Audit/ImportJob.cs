using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Audit;

/// <summary>
/// Domain entity representing a tracking record for spreadsheet imports.
/// </summary>
public sealed class ImportJob : AuditableEntity
{
    private ImportJob() { }

    /// <summary>Gets the unique Job ID.</summary>
    public Guid JobId { get; private set; }

    /// <summary>Gets the owner company ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the target module (e.g. Employees, Lookups).</summary>
    public string ModuleName { get; private set; } = string.Empty;

    /// <summary>Gets the source file name.</summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>Gets the processing status (Pending, Completed, Failed).</summary>
    public string Status { get; private set; } = "Pending";

    /// <summary>Gets the number of processed records.</summary>
    public int ProcessedCount { get; private set; }

    /// <summary>Gets the number of successfully validated records.</summary>
    public int SuccessCount { get; private set; }

    /// <summary>Gets the number of failed records.</summary>
    public int FailureCount { get; private set; }

    /// <summary>Gets the JSON payload containing the validated records to import.</summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>Gets the processing error message, if any.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Factory method to create a new ImportJob.</summary>
    public static ImportJob Create(
        Guid jobId,
        int companyId,
        string moduleName,
        string fileName,
        int processedCount,
        int successCount,
        int failureCount,
        string payload,
        string status = "Pending",
        string? errorMessage = null)
    {
        return new ImportJob
        {
            JobId = jobId,
            CompanyId = companyId,
            ModuleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName)),
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName)),
            ProcessedCount = processedCount,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Payload = payload ?? string.Empty,
            Status = status,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>Updates the job status to Completed.</summary>
    public void Complete()
    {
        Status = "Completed";
    }

    /// <summary>Updates the job status to Failed with an error message.</summary>
    public void Fail(string errorMessage)
    {
        Status = "Failed";
        ErrorMessage = errorMessage;
    }
}
