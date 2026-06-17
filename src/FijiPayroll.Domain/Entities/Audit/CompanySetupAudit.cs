using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using System;

namespace FijiPayroll.Domain.Entities.Audit;

/// <summary>
/// Domain entity representing a detailed onboarding audit log tracking setup actions and results.
/// </summary>
public sealed class CompanySetupAudit : SoftDeleteEntity
{
    private CompanySetupAudit() { }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the step of the setup wizard (e.g. Welcome, Calendar).</summary>
    public string Step { get; private set; } = string.Empty;

    /// <summary>Gets the action performed (e.g. Generated periods, Installed rules).</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Gets the timestamp when the audited action was completed.</summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>Gets the result summary message of the audited action.</summary>
    public string Result { get; private set; } = string.Empty;

    /// <summary>Gets the audit status outcome of the action.</summary>
    public SetupAuditStatus Status { get; private set; } = SetupAuditStatus.Success;

    /// <summary>Gets the client machine IP address.</summary>
    public string IPAddress { get; private set; } = string.Empty;

    /// <summary>Gets the host machine name performing setup.</summary>
    public string MachineName { get; private set; } = string.Empty;

    /// <summary>Gets the application build version.</summary>
    public string ApplicationVersion { get; private set; } = string.Empty;

    /// <summary>Gets the correlation ID linking database operations trace.</summary>
    public Guid CorrelationId { get; private set; }

    /// <summary>Gets the wizard execution ID.</summary>
    public Guid ExecutionId { get; private set; }

    /// <summary>Factory method to create a new CompanySetupAudit.</summary>
    public static CompanySetupAudit Create(
        int companyId,
        string step,
        string action,
        string result,
        SetupAuditStatus status,
        string ipAddress,
        string machineName,
        string appVersion,
        Guid correlationId,
        Guid executionId)
    {
        if (companyId <= 0)
            throw new ArgumentException("Company ID must be positive.", nameof(companyId));
        if (string.IsNullOrWhiteSpace(step))
            throw new ArgumentException("Step cannot be empty.", nameof(step));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty.", nameof(action));

        return new CompanySetupAudit
        {
            CompanyId = companyId,
            Step = step,
            Action = action,
            Timestamp = DateTime.UtcNow,
            Result = result ?? string.Empty,
            Status = status,
            IPAddress = ipAddress ?? "127.0.0.1",
            MachineName = machineName ?? Environment.MachineName,
            ApplicationVersion = appVersion ?? "1.0.0",
            CorrelationId = correlationId,
            ExecutionId = executionId
        };
    }
}
