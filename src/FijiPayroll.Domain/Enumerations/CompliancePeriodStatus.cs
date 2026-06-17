namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Status of a compliance reporting period (e.g. dynamic monthly cycle).
/// </summary>
public enum CompliancePeriodStatus
{
    /// <summary>Period is open for transactions and modifications.</summary>
    Open = 1,

    /// <summary>Period is locked for review and verification; no further entries permitted.</summary>
    Locked = 2,

    /// <summary>Period is finalized and closed; historical ledger frozen.</summary>
    Closed = 3
}
