namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Status values for a <see cref="FijiPayroll.Domain.Entities.Leave.LeaveRequest"/> lifecycle.
/// Models the approval state machine enforced by the domain aggregate.
/// </summary>
public enum LeaveStatus
{
    /// <summary>Leave request submitted by employee, awaiting manager decision.</summary>
    Pending,

    /// <summary>Leave request approved by authorised manager.</summary>
    Approved,

    /// <summary>Leave request rejected by manager.</summary>
    Rejected,

    /// <summary>Leave request cancelled by the employee or an admin.</summary>
    Cancelled
}
