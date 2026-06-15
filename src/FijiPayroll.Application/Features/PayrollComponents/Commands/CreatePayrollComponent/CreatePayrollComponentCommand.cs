using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Enumerations;
using MediatR;

namespace FijiPayroll.Application.Features.PayrollComponents.Commands.CreatePayrollComponent;

/// <summary>
/// Command to create a new payroll component for a company.
/// Sent by the <c>PayrollComponentFormViewModel</c> in the WPF UI.
///
/// Validation is enforced by <see cref="CreatePayrollComponentCommandValidator"/>
/// via the MediatR pipeline before the handler executes.
/// </summary>
/// <param name="CompanyId">The owning company (must match current user's access).</param>
/// <param name="ComponentCode">Unique uppercase code for the company. Max 20 chars.</param>
/// <param name="ComponentName">Human-readable name displayed on payslips. Max 200 chars.</param>
/// <param name="ComponentType">Classification: Earning, Deduction, Allowance, Benefit, Statutory.</param>
/// <param name="CalculationMethod">Fixed, Percentage, Formula, or Manual.</param>
/// <param name="CalculationValue">Dollar amount or % value. Required for Fixed and Percentage methods.</param>
/// <param name="Formula">Formula expression. Required for Formula method.</param>
/// <param name="IsTaxable">Whether this component contributes to PAYE taxable income.</param>
/// <param name="IsFnpfApplicable">Whether this component contributes to FNPF-applicable gross.</param>
/// <param name="DisplayOrder">Non-negative sort order on payslips.</param>
/// <param name="Description">Optional internal description. Max 500 chars.</param>
public sealed record CreatePayrollComponentCommand(
    int CompanyId,
    string ComponentCode,
    string ComponentName,
    ComponentType ComponentType,
    CalculationMethod CalculationMethod,
    decimal? CalculationValue,
    string? Formula,
    bool IsTaxable,
    bool IsFnpfApplicable,
    int DisplayOrder,
    string? Description
) : IRequest<Result<int>>;
