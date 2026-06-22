using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Detailed-level audit history log capturing payroll run modifications.
/// </summary>
public sealed class PayrollRunHistory : BaseEntity
{
    public int CompanyId { get; private set; }
    public int PayrollRunId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public string User { get; private set; } = string.Empty;
    public string Machine { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }

    private PayrollRunHistory() { } // For EF Core

    public static PayrollRunHistory Create(
        int companyId,
        int payrollRunId,
        string action,
        string user,
        string machine,
        string correlationId,
        string description,
        string? oldValues,
        string? newValues)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (payrollRunId <= 0) throw new ArgumentOutOfRangeException(nameof(payrollRunId));
        if (string.IsNullOrWhiteSpace(action)) throw new ArgumentException("Action is required.", nameof(action));

        return new PayrollRunHistory
        {
            CompanyId = companyId,
            PayrollRunId = payrollRunId,
            Action = action,
            Timestamp = DateTime.UtcNow,
            User = user ?? "System",
            Machine = machine ?? "Unknown",
            CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
            Description = description ?? string.Empty,
            OldValues = oldValues,
            NewValues = newValues
        };
    }
}
