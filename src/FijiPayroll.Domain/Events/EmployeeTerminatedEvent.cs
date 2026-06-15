using FijiPayroll.Domain.Entities.Company;
using System;

namespace FijiPayroll.Domain.Events;

/// <summary>
/// Domain event raised when an employee is terminated (deactivated).
/// </summary>
public sealed class EmployeeTerminatedEvent : IDomainEvent
{
    /// <summary>Gets the terminated employee entity reference.</summary>
    public Employee Employee { get; }

    /// <summary>Gets the owner company ID.</summary>
    public int CompanyId => Employee.CompanyId;

    /// <summary>Gets the employee's unique identifier.</summary>
    public int EmployeeId => Employee.Id;

    /// <summary>Gets the username of who terminated the employee.</summary>
    public string TerminatedBy { get; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; }

    /// <summary>Initializes the event.</summary>
    public EmployeeTerminatedEvent(Employee employee, string terminatedBy)
    {
        Employee = employee ?? throw new ArgumentNullException(nameof(employee));
        TerminatedBy = terminatedBy ?? "System";
        OccurredOn = DateTime.UtcNow;
    }
}
