using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Enumerations;
using MediatR;
using System.Collections.Generic;

namespace FijiPayroll.Application.Features.Employees.Commands.CreateEmployee;

/// <summary>
/// Input model representing a payment method allocation for an employee.
/// </summary>
public sealed record PaymentMethodInput(
    PaymentMethodType MethodType,
    decimal Percentage,
    bool IsPrimary,
    string? BankName = null,
    string? BankAccountNumber = null,
    string? BankSortCode = null,
    string? MobileNumber = null
);

/// <summary>
/// CQRS Command to create a new Employee master data record.
/// </summary>
public sealed record CreateEmployeeCommand(
    int CompanyId,
    string FullName,
    string Tin,
    string FnpfNumber,
    string ResidencyStatus,
    string Department,
    decimal BaseSalary,
    PayrollFrequency Frequency,
    bool IsFnpfExempt,
    bool IsTaxExempt,
    bool IsActive,
    EmploymentType EmploymentType,
    string Branch,
    string Position,
    string Email,
    IReadOnlyList<PaymentMethodInput> PaymentMethods
) : IRequest<Result<int>>;
