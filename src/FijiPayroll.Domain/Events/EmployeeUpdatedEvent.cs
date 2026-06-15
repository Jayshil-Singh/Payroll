using FijiPayroll.Domain.Entities.Company;
using System;

namespace FijiPayroll.Domain.Events;

/// <summary>
/// Domain event raised when an employee's properties are updated.
/// </summary>
public sealed class EmployeeUpdatedEvent : IDomainEvent
{
    /// <summary>Gets the updated employee entity reference.</summary>
    public Employee Employee { get; }

    /// <summary>Gets the owner company ID.</summary>
    public int CompanyId => Employee.CompanyId;

    /// <summary>Gets the employee's unique identifier.</summary>
    public int EmployeeId => Employee.Id;

    /// <summary>Gets the employee's full name.</summary>
    public string FullName => Employee.FullName;

    /// <summary>Gets the employee's email address.</summary>
    public string Email => Employee.Email;

    /// <inheritdoc />
    public DateTime OccurredOn { get; }

    /// <summary>Initializes the event.</summary>
    public EmployeeUpdatedEvent(Employee employee)
    {
        Employee = employee ?? throw new ArgumentNullException(nameof(employee));
        OccurredOn = DateTime.UtcNow;
    }
}
