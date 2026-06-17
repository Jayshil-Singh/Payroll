using FijiPayroll.Domain.Enumerations;
using System;
using System.Collections.Generic;

namespace FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollRunById;

/// <summary>
/// Detailed DTO for a payroll run, containing its computed employees snapshots and status rules.
/// </summary>
public sealed record PayrollRunDetailDto
{
    public int Id { get; init; }
    public int CompanyId { get; init; }
    public string RunCode { get; init; } = string.Empty;
    public string PeriodName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime PaymentDate { get; init; }
    public PayrollFrequencyType Frequency { get; init; }
    public PayrollRunStatus Status { get; init; }
    public string? Description { get; init; }
    public string? SnapshotHash { get; init; }
    public Guid? CurrentRequestId { get; init; }
    public IReadOnlyList<PayrollRunEmployeeDto> Employees { get; init; } = Array.Empty<PayrollRunEmployeeDto>();
}
