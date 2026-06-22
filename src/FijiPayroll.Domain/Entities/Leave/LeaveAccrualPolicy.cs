using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Exceptions;
using FijiPayroll.Shared.Guards;

namespace FijiPayroll.Domain.Entities.Leave;

/// <summary>
/// Company-level policy governing how leave balances are accrued.
/// Maps to <c>leave.LeaveAccrualPolicies</c>.
/// Supports both annual-grant and periodic-accrual models.
/// </summary>
public sealed class LeaveAccrualPolicy : SoftDeleteEntity
{
    private string _policyName = string.Empty;

    private LeaveAccrualPolicy() { }

    /// <summary>Gets the company this policy belongs to (multi-tenant isolation).</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the leave type this accrual policy applies to.</summary>
    public int LeaveTypeId { get; private set; }

    /// <summary>Navigation property to the associated leave type.</summary>
    public LeaveType? LeaveType { get; private set; }

    /// <summary>Gets the descriptive name of this policy.</summary>
    public string PolicyName
    {
        get => _policyName;
        private set => _policyName = Guard.AgainstNullOrWhiteSpace(value, nameof(PolicyName));
    }

    /// <summary>
    /// Gets the accrual method:
    /// <list type="bullet">
    ///   <item><term>AnnualGrant</term><description>Full entitlement granted at start of year.</description></item>
    ///   <item><term>PeriodicAccrual</term><description>Leave accrues each pay period.</description></item>
    ///   <item><term>DailyAccrual</term><description>Leave accrues daily (e.g., 0.03846 days/working day).</description></item>
    /// </list>
    /// </summary>
    public string AccrualMethod { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the accrual rate per working day.
    /// Per PayrollRules.md §10.1: 10 days / 260 working days = 0.03846 days/day.
    /// Only used when AccrualMethod is DailyAccrual or PeriodicAccrual.
    /// </summary>
    public decimal AccrualRatePerDay { get; private set; }

    /// <summary>
    /// Gets the maximum number of days that can be carried over at year end.
    /// Null means unlimited carry-over (subject to leave type configuration).
    /// </summary>
    public decimal? MaxCarryOverDays { get; private set; }

    /// <summary>Gets whether the policy is currently active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the date from which this policy is effective.</summary>
    public DateTime EffectiveFrom { get; private set; }

    /// <summary>Gets the optional expiry date for this policy.</summary>
    public DateTime? EffectiveTo { get; private set; }

    /// <summary>
    /// Factory method to create a new leave accrual policy.
    /// </summary>
    /// <param name="companyId">Company identifier.</param>
    /// <param name="leaveTypeId">Leave type this policy governs.</param>
    /// <param name="policyName">Descriptive name.</param>
    /// <param name="accrualMethod">AnnualGrant | PeriodicAccrual | DailyAccrual</param>
    /// <param name="accrualRatePerDay">Accrual rate per working day (0 for AnnualGrant).</param>
    /// <param name="maxCarryOverDays">Optional carry-over cap in days.</param>
    /// <param name="effectiveFrom">Start date for this policy.</param>
    /// <param name="effectiveTo">Optional end date for this policy.</param>
    /// <returns>A new, valid <see cref="LeaveAccrualPolicy"/>.</returns>
    /// <exception cref="DomainException">Thrown when business rules are violated.</exception>
    public static LeaveAccrualPolicy Create(
        int companyId,
        int leaveTypeId,
        string policyName,
        string accrualMethod,
        decimal accrualRatePerDay,
        decimal? maxCarryOverDays,
        DateTime effectiveFrom,
        DateTime? effectiveTo = null)
    {
        if (companyId <= 0)
            throw new DomainException("CompanyId must be a positive integer.");

        if (leaveTypeId <= 0)
            throw new DomainException("LeaveTypeId must be a positive integer.");

        var validMethods = new[] { "AnnualGrant", "PeriodicAccrual", "DailyAccrual" };
        if (!validMethods.Contains(accrualMethod))
            throw new DomainException($"AccrualMethod must be one of: {string.Join(", ", validMethods)}.");

        if (accrualRatePerDay < 0)
            throw new DomainException("AccrualRatePerDay must not be negative.");

        if (maxCarryOverDays.HasValue && maxCarryOverDays.Value < 0)
            throw new DomainException("MaxCarryOverDays must not be negative.");

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new DomainException("EffectiveTo must not be before EffectiveFrom.");

        return new LeaveAccrualPolicy
        {
            CompanyId = companyId,
            LeaveTypeId = leaveTypeId,
            PolicyName = policyName,
            AccrualMethod = accrualMethod,
            AccrualRatePerDay = accrualRatePerDay,
            MaxCarryOverDays = maxCarryOverDays,
            EffectiveFrom = effectiveFrom.Date,
            EffectiveTo = effectiveTo?.Date,
            IsActive = true
        };
    }

    /// <summary>Deactivates this accrual policy.</summary>
    /// <exception cref="DomainException">Thrown if deleted or already inactive.</exception>
    public void Deactivate()
    {
        if (IsDeleted)
            throw new DomainException("Cannot deactivate a deleted accrual policy.");

        if (!IsActive)
            throw new DomainException("Accrual policy is already inactive.");

        IsActive = false;
    }
}
