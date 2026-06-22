using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using System;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model for storing employee-specific processing failures without stopping the entire payroll.
/// </summary>
public sealed class PayrollExceptionQueue : BaseEntity
{
    public int CompanyId { get; private set; }
    public int PayrollRunId { get; private set; }
    public int EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public PayrollValidationSeverity Severity { get; private set; }
    public string Recommendation { get; private set; } = string.Empty;
    public string? StackTrace { get; private set; }
    public string AuditId { get; private set; } = string.Empty;
    public string? OperatorResolution { get; private set; }
    public DateTime? ResolvedDate { get; private set; }
    public string? ResolvedBy { get; private set; }
    public bool IsResolved { get; private set; }

    private PayrollExceptionQueue() { } // For EF Core

    public static PayrollExceptionQueue Create(
        int companyId,
        int payrollRunId,
        int employeeId,
        string employeeName,
        string reason,
        PayrollValidationSeverity severity,
        string recommendation,
        string? stackTrace,
        string auditId)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (payrollRunId <= 0) throw new ArgumentOutOfRangeException(nameof(payrollRunId));
        if (employeeId <= 0) throw new ArgumentOutOfRangeException(nameof(employeeId));

        return new PayrollExceptionQueue
        {
            CompanyId = companyId,
            PayrollRunId = payrollRunId,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            Reason = reason,
            Severity = severity,
            Recommendation = recommendation ?? string.Empty,
            StackTrace = stackTrace,
            AuditId = auditId ?? Guid.NewGuid().ToString(),
            IsResolved = false
        };
    }

    public void Resolve(string resolution, string operatorUser)
    {
        if (IsResolved)
        {
            throw new InvalidOperationException("This exception is already resolved.");
        }

        IsResolved = true;
        OperatorResolution = resolution;
        ResolvedDate = DateTime.UtcNow;
        ResolvedBy = operatorUser;
    }
}
