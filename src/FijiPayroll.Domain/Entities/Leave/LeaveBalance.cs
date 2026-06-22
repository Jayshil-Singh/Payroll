using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Exceptions;

namespace FijiPayroll.Domain.Entities.Leave;

/// <summary>
/// Tracks an employee's leave balance for a specific leave type in a given fiscal year.
/// Maps to <c>employee.EmployeeLeaveBalances</c>.
/// All balance mutations are controlled through domain methods to enforce business invariants.
/// </summary>
public sealed class LeaveBalance : AuditableEntity
{
    private LeaveBalance() { }

    /// <summary>Gets the company this balance belongs to (multi-tenant isolation).</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the employee who owns this leave balance.</summary>
    public int EmployeeId { get; private set; }

    /// <summary>Gets the leave type this balance is for.</summary>
    public int LeaveTypeId { get; private set; }

    /// <summary>Navigation property to the associated leave type.</summary>
    public LeaveType? LeaveType { get; private set; }

    /// <summary>
    /// Gets the fiscal year this balance applies to (e.g., 2026).
    /// </summary>
    public int FiscalYear { get; private set; }

    /// <summary>
    /// Gets the full annual entitlement in working days as configured on the leave type.
    /// </summary>
    public decimal Entitlement { get; private set; }

    /// <summary>
    /// Gets the total days accrued to date (may differ from entitlement for mid-year starters).
    /// </summary>
    public decimal Accrued { get; private set; }

    /// <summary>
    /// Gets the total days carried forward from the previous period.
    /// </summary>
    public decimal CarriedForward { get; private set; }

    /// <summary>
    /// Gets the total leave days already taken (from approved requests in payroll).
    /// </summary>
    public decimal Taken { get; private set; }

    /// <summary>
    /// Gets the total leave days in approved pending requests not yet processed by payroll.
    /// </summary>
    public decimal Pending { get; private set; }

    /// <summary>
    /// Gets the remaining available balance: Accrued + CarriedForward − Taken − Pending.
    /// </summary>
    public decimal Available => Accrued + CarriedForward - Taken - Pending;

    /// <summary>
    /// Gets the closing balance at the end of the period (set during period close).
    /// </summary>
    public decimal ClosingBalance { get; private set; }

    /// <summary>
    /// Factory method to initialise a new leave balance for an employee for a fiscal year.
    /// </summary>
    /// <param name="companyId">Company identifier.</param>
    /// <param name="employeeId">Employee identifier.</param>
    /// <param name="leaveTypeId">Leave type identifier.</param>
    /// <param name="fiscalYear">Fiscal year (e.g., 2026).</param>
    /// <param name="entitlement">Annual entitlement days.</param>
    /// <param name="carriedForward">Days brought forward from previous period.</param>
    /// <returns>A new <see cref="LeaveBalance"/> with zero taken/pending amounts.</returns>
    /// <exception cref="DomainException">Thrown when inputs violate business rules.</exception>
    public static LeaveBalance Initialise(
        int companyId,
        int employeeId,
        int leaveTypeId,
        int fiscalYear,
        decimal entitlement,
        decimal carriedForward = 0m)
    {
        if (companyId <= 0)
            throw new DomainException("CompanyId must be a positive integer.");

        if (employeeId <= 0)
            throw new DomainException("EmployeeId must be a positive integer.");

        if (leaveTypeId <= 0)
            throw new DomainException("LeaveTypeId must be a positive integer.");

        if (fiscalYear < 2000 || fiscalYear > 2100)
            throw new DomainException("FiscalYear must be a valid 4-digit year between 2000 and 2100.");

        if (entitlement < 0)
            throw new DomainException("Entitlement must not be negative.");

        if (carriedForward < 0)
            throw new DomainException("CarriedForward days must not be negative.");

        return new LeaveBalance
        {
            CompanyId = companyId,
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            FiscalYear = fiscalYear,
            Entitlement = entitlement,
            Accrued = entitlement,
            CarriedForward = carriedForward,
            Taken = 0m,
            Pending = 0m,
            ClosingBalance = 0m
        };
    }

    /// <summary>
    /// Accrues a number of days to this balance (used for pro-rata or periodic accrual).
    /// </summary>
    /// <param name="days">Days to accrue (must be positive).</param>
    /// <exception cref="DomainException">Thrown when days is non-positive.</exception>
    public void Accrue(decimal days)
    {
        if (days <= 0)
            throw new DomainException("Accrual days must be greater than zero.");

        Accrued += days;
    }

    /// <summary>
    /// Reserves (holds) days as pending when a leave request is approved but not yet in payroll.
    /// </summary>
    /// <param name="days">Days to reserve.</param>
    /// <exception cref="DomainException">Thrown when insufficient balance or invalid days.</exception>
    public void Reserve(decimal days)
    {
        if (days <= 0)
            throw new DomainException("Reserved days must be greater than zero.");

        if (days > Available)
            throw new DomainException(
                $"Insufficient leave balance. Requested {days} days but only {Available} days available.");

        Pending += days;
    }

    /// <summary>
    /// Releases previously reserved (pending) days back to available balance.
    /// Called when a leave request is cancelled after approval.
    /// </summary>
    /// <param name="days">Days to release.</param>
    /// <exception cref="DomainException">Thrown when releasing more than is pending.</exception>
    public void Release(decimal days)
    {
        if (days <= 0)
            throw new DomainException("Released days must be greater than zero.");

        if (days > Pending)
            throw new DomainException(
                $"Cannot release {days} days; only {Pending} days are currently reserved.");

        Pending -= days;
    }

    /// <summary>
    /// Processes payroll consumption of leave: moves reserved days from pending to taken.
    /// </summary>
    /// <param name="days">Days consumed in payroll processing.</param>
    /// <exception cref="DomainException">Thrown when days exceed pending or are invalid.</exception>
    public void ProcessPayroll(decimal days)
    {
        if (days <= 0)
            throw new DomainException("Processed days must be greater than zero.");

        if (days > Pending)
            throw new DomainException(
                $"Cannot process {days} days; only {Pending} days are in pending status.");

        Pending -= days;
        Taken += days;
    }

    /// <summary>
    /// Applies a manual adjustment to the balance (e.g., correction, carry-forward override).
    /// </summary>
    /// <param name="adjustmentDays">Positive to add, negative to subtract.</param>
    /// <param name="reason">Mandatory reason for audit trail.</param>
    /// <exception cref="DomainException">Thrown when the adjustment would make accrued negative or reason is empty.</exception>
    public void Adjust(decimal adjustmentDays, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("An adjustment reason must be provided.");

        decimal newAccrued = Accrued + adjustmentDays;
        if (newAccrued < 0)
            throw new DomainException("Adjustment would result in a negative accrued balance.");

        Accrued = newAccrued;
    }

    /// <summary>
    /// Closes the period: computes and stores the closing balance.
    /// Should be called at fiscal year end before carry-forward processing.
    /// </summary>
    public void ClosePeriod()
    {
        ClosingBalance = Available;
    }
}
