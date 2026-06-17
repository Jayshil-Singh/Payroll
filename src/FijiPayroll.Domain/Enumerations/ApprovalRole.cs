namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the specific verification role classifications for approvers in the approval engine.
/// </summary>
public enum ApprovalRole
{
    /// <summary>Data entry level administrator handling preliminary runs updates.</summary>
    PayrollOfficer = 1,

    /// <summary>Supervisor managing runs verification and locking operations.</summary>
    PayrollSupervisor = 2,

    /// <summary>Finance director signing off bank files disbursements.</summary>
    FinanceManager = 3,

    /// <summary>HR manager verifying leave, promotions, and contracts details.</summary>
    HRManager = 4,

    /// <summary>System level administrator holding global permission scopes.</summary>
    Administrator = 5
}
