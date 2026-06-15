using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Enumerations;
using MediatR;
using System.Collections.Generic;

namespace FijiPayroll.Application.Features.Employees.Commands.UpdateEmployee;

/// <summary>
/// CQRS Command to update an existing Employee record.
/// </summary>
public sealed record UpdateEmployeeCommand(
    int Id,
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
    IReadOnlyList<FijiPayroll.Application.Features.Employees.Commands.CreateEmployee.PaymentMethodInput> PaymentMethods
) : IRequest<Result>;
