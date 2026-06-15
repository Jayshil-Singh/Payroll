using FijiPayroll.Domain.Entities.Company;
using System;

namespace FijiPayroll.Domain.Events;

/// <summary>
/// Domain event raised when a new employee is successfully created.
/// </summary>
public sealed class EmployeeCreatedEvent : IDomainEvent
{
    /// <summary>Gets the created employee entity reference.</summary>
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
    public EmployeeCreatedEvent(Employee employee)
    {
        Employee = employee ?? throw new ArgumentNullException(nameof(employee));
        OccurredOn = DateTime.UtcNow;
    }
}
