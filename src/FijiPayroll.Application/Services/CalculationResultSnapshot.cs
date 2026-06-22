using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Events;
using System;
using System.Collections.Generic;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Output data structure holding the results of in-memory payroll calculation.
/// </summary>
public sealed class CalculationResultSnapshot
{
    public int PayrollRunId { get; init; }
    public string SnapshotHash { get; init; } = string.Empty;
    public Guid CalculationRequestId { get; init; }
    public IReadOnlyList<CalculatedEmployeeResult> Employees { get; init; } = Array.Empty<CalculatedEmployeeResult>();
    public IReadOnlyList<PayrollAuditEvent> AuditEvents { get; init; } = Array.Empty<PayrollAuditEvent>();
}

/// <summary>
/// Computed employee output snapshot.
/// </summary>
public sealed class CalculatedEmployeeResult
{
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string Tin { get; init; } = string.Empty;
    public string FnpfNumber { get; init; } = string.Empty;
    public string ResidencyStatus { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public decimal BaseSalary { get; init; }
    public decimal GrossPay { get; init; }
    public decimal TotalAllowances { get; init; }
    public decimal TotalDeductions { get; init; }
    public decimal NetPay { get; init; }
    public decimal PayeTax { get; init; }
    public decimal FnpfEmployeeContribution { get; init; }
    public decimal FnpfEmployerContribution { get; init; }
    public string TaxVersionUsed { get; init; } = string.Empty;
    public string TraceText { get; init; } = string.Empty;
    public IReadOnlyList<CalculatedLineItemResult> LineItems { get; init; } = Array.Empty<CalculatedLineItemResult>();
    public IReadOnlyList<PayrollAuditEvent> AuditEvents { get; init; } = Array.Empty<PayrollAuditEvent>();
    public bool IsSuccess { get; init; } = true;
    public string? ErrorMessage { get; init; }
    public string? ErrorStackTrace { get; init; }
}

/// <summary>
/// Computed line item result.
/// </summary>
public sealed class CalculatedLineItemResult
{
    public int ComponentId { get; init; }
    public string ComponentCode { get; init; } = string.Empty;
    public string ComponentName { get; init; } = string.Empty;
    public ComponentType ComponentType { get; init; }
    public decimal Amount { get; set; }
    public bool IsTaxable { get; init; }
    public bool AffectsFnpf { get; init; }
    public bool EmployerContributionFlag { get; init; }
    public int ReferenceComponentId { get; init; }
}
