using System;
using System.Collections.Generic;

namespace FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollRunById;

/// <summary>
/// DTO representing calculated employee records and calculation traces.
/// </summary>
public sealed record PayrollRunEmployeeDto
{
    public int Id { get; init; }
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
    public bool IsSuperseded { get; init; }
    public string? TraceText { get; init; }
    public IReadOnlyList<PayrollRunLineItemDto> LineItems { get; init; } = Array.Empty<PayrollRunLineItemDto>();
}

/// <summary>
/// DTO representing individual calculation components.
/// </summary>
public sealed record PayrollRunLineItemDto
{
    public int Id { get; init; }
    public int ComponentId { get; init; }
    public string ComponentCode { get; init; } = string.Empty;
    public string ComponentName { get; init; } = string.Empty;
    public string ComponentType { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public bool IsTaxable { get; init; }
    public bool AffectsFnpf { get; init; }
    public bool EmployerContributionFlag { get; init; }
}
