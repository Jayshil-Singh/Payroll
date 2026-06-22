namespace FijiPayroll.Application.Common.Behaviours;

/// <summary>
/// Interface indicating that a command request should generate an explicit command-level audit log.
/// </summary>
public interface IAuditableCommand
{
    /// <summary>Gets the description of the action being audited (e.g., "Approve Payroll Run").</summary>
    string AuditAction { get; }

    /// <summary>Gets the logical entity name being affected (e.g., "PayrollRun").</summary>
    string AuditEntity { get; }
}
