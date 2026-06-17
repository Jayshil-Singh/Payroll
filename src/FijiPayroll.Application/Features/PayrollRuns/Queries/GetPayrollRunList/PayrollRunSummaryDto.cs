using FijiPayroll.Domain.Enumerations;
using System;

namespace FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollRunList;

/// <summary>
/// Lightweight DTO representing a payroll run header summary.
/// </summary>
public sealed record PayrollRunSummaryDto
{
    public int Id { get; init; }
    public string RunCode { get; init; } = string.Empty;
    public string PeriodName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime PaymentDate { get; init; }
    public PayrollFrequencyType Frequency { get; init; }
    public PayrollRunStatus Status { get; init; }
    public string? Description { get; init; }
}
