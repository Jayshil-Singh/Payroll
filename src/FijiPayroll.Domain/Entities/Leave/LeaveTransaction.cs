using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Exceptions;

namespace FijiPayroll.Domain.Entities.Leave;

/// <summary>
/// Immutable ledger record representing leave taken within a payroll run period.
/// Maps to <c>payroll.LeaveTransactions</c>.
/// Once created via the factory method, all properties are read-only to preserve audit integrity.
/// </summary>
public sealed class LeaveTransaction : AuditableEntity
{
    private LeaveTransaction() { }

    /// <summary>Gets the company this transaction belongs to (multi-tenant isolation).</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the payroll run that generated this transaction.</summary>
    public int PayrollRunId { get; private set; }

    /// <summary>Gets the employee this leave transaction applies to.</summary>
    public int EmployeeId { get; private set; }

    /// <summary>Gets the leave type consumed in this transaction.</summary>
    public int LeaveTypeId { get; private set; }

    /// <summary>Navigation property to the associated leave type.</summary>
    public LeaveType? LeaveType { get; private set; }

    /// <summary>Gets the associated leave request (if originated from a request).</summary>
    public int? LeaveRequestId { get; private set; }

    /// <summary>Gets the first day of the leave period recorded in this transaction.</summary>
    public DateTime PeriodStart { get; private set; }

    /// <summary>Gets the last day of the leave period recorded in this transaction.</summary>
    public DateTime PeriodEnd { get; private set; }

    /// <summary>Gets the number of working days deducted from the balance in this transaction.</summary>
    public decimal DaysDeducted { get; private set; }

    /// <summary>
    /// Gets the calculated leave pay amount for this transaction.
    /// Zero for unpaid leave types.
    /// </summary>
    public decimal LeavePay { get; private set; }

    /// <summary>
    /// Gets the leave loading amount (25% of leave pay) if applicable.
    /// Zero if leave loading is not configured for the leave type.
    /// </summary>
    public decimal LeaveLoading { get; private set; }

    /// <summary>Gets the total leave payment: LeavePay + LeaveLoading.</summary>
    public decimal TotalLeavePay => LeavePay + LeaveLoading;

    /// <summary>
    /// Factory method to record a leave transaction from a payroll run.
    /// All fields are immutable after creation.
    /// </summary>
    /// <param name="companyId">Company identifier.</param>
    /// <param name="payrollRunId">Payroll run generating the transaction.</param>
    /// <param name="employeeId">Employee identifier.</param>
    /// <param name="leaveTypeId">Leave type consumed.</param>
    /// <param name="leaveRequestId">Optional originating leave request.</param>
    /// <param name="periodStart">First day of the leave window.</param>
    /// <param name="periodEnd">Last day of the leave window.</param>
    /// <param name="daysDeducted">Working days deducted.</param>
    /// <param name="leavePay">Calculated leave pay (daily rate × days).</param>
    /// <param name="leaveLoading">Leave loading amount (may be zero).</param>
    /// <returns>An immutable <see cref="LeaveTransaction"/> record.</returns>
    /// <exception cref="DomainException">Thrown when inputs violate business rules.</exception>
    public static LeaveTransaction Record(
        int companyId,
        int payrollRunId,
        int employeeId,
        int leaveTypeId,
        int? leaveRequestId,
        DateTime periodStart,
        DateTime periodEnd,
        decimal daysDeducted,
        decimal leavePay,
        decimal leaveLoading)
    {
        if (companyId <= 0)
            throw new DomainException("CompanyId must be a positive integer.");

        if (payrollRunId <= 0)
            throw new DomainException("PayrollRunId must be a positive integer.");

        if (employeeId <= 0)
            throw new DomainException("EmployeeId must be a positive integer.");

        if (leaveTypeId <= 0)
            throw new DomainException("LeaveTypeId must be a positive integer.");

        if (periodStart.Date > periodEnd.Date)
            throw new DomainException("Leave period start must not be after end date.");

        if (daysDeducted <= 0)
            throw new DomainException("DaysDeducted must be greater than zero.");

        if (leavePay < 0)
            throw new DomainException("LeavePay must not be negative.");

        if (leaveLoading < 0)
            throw new DomainException("LeaveLoading must not be negative.");

        return new LeaveTransaction
        {
            CompanyId = companyId,
            PayrollRunId = payrollRunId,
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            LeaveRequestId = leaveRequestId,
            PeriodStart = periodStart.Date,
            PeriodEnd = periodEnd.Date,
            DaysDeducted = daysDeducted,
            LeavePay = leavePay,
            LeaveLoading = leaveLoading
        };
    }
}
