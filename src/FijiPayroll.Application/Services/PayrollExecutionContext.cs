using FijiPayroll.Domain.Enumerations;
using System;
using System.Collections.Generic;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Immutable, thread-safe in-memory context representing the exact execution state for a payroll run.
/// Enforces immutability via init-only properties and read-only lists.
/// </summary>
public sealed class PayrollExecutionContext
{
    public int PayrollRunId { get; init; }
    public int CompanyId { get; init; }
    public string RunCode { get; init; } = string.Empty;
    public string PeriodName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public PayrollFrequency Frequency { get; init; }
    public string TaxVersion { get; init; } = string.Empty;
    public Guid CalculationRequestId { get; init; }

    /// <summary>
    /// Snapshots of employees to compute, ordered by EmployeeId at database level.
    /// </summary>
    public IReadOnlyList<EmployeeSnapshot> Employees { get; init; } = Array.Empty<EmployeeSnapshot>();

    /// <summary>
    /// Active tax bracket snapshots matching the requested tax version.
    /// </summary>
    public IReadOnlyList<FijiPayroll.Domain.Entities.Company.TaxBracket> TaxRules { get; init; } = Array.Empty<FijiPayroll.Domain.Entities.Company.TaxBracket>();

    /// <summary>
    /// Active payroll component definitions at calculation time.
    /// </summary>
    public IReadOnlyList<PayrollComponentSnapshot> Components { get; init; } = Array.Empty<PayrollComponentSnapshot>();
}

/// <summary>
/// Immutable snapshot of employee details.
/// </summary>
public sealed class EmployeeSnapshot
{
    public int EmployeeId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Tin { get; init; } = string.Empty;
    public string FnpfNumber { get; init; } = string.Empty;
    public string ResidencyStatus { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public decimal BaseSalary { get; init; }
    public bool IsFnpfExempt { get; init; }
    public bool IsTaxExempt { get; init; }
    public decimal HoursWorked { get; init; }
    public decimal OvertimeHours { get; init; }

    /// <summary>
    /// Component overrides (manual entries) specifically for this pay period.
    /// </summary>
    public IReadOnlyList<EmployeeComponentOverrideSnapshot> ComponentOverrides { get; init; } = Array.Empty<EmployeeComponentOverrideSnapshot>();
}

/// <summary>
/// Immutable snapshot of employee component overrides.
/// </summary>
public sealed class EmployeeComponentOverrideSnapshot
{
    public string ComponentCode { get; init; } = string.Empty;
    public decimal Value { get; init; }
}

/// <summary>
/// Immutable snapshot of active components configuration.
/// </summary>
public sealed class PayrollComponentSnapshot
{
    public int Id { get; init; }
    public string ComponentCode { get; init; } = string.Empty;
    public string ComponentName { get; init; } = string.Empty;
    public ComponentType ComponentType { get; init; }
    public CalculationMethod CalculationMethod { get; init; }
    public decimal? CalculationValue { get; init; }
    public string? Formula { get; init; }
    public bool IsTaxable { get; init; }
    public bool IsFnpfApplicable { get; init; }
    public int DisplayOrder { get; init; }
}

