using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Enumerations;
using MediatR;

namespace FijiPayroll.Application.Features.PayrollComponents.Commands.UpdatePayrollComponent;

/// <summary>
/// Command to update an existing payroll component.
/// System components have restricted fields — only name and display order can be changed.
/// </summary>
/// <param name="Id">Primary key of the component to update.</param>
/// <param name="ComponentName">Updated display name. Max 200 chars.</param>
/// <param name="ComponentType">Updated classification (restricted for system components).</param>
/// <param name="CalculationMethod">Updated calculation method (restricted for system components).</param>
/// <param name="CalculationValue">Updated dollar amount or percentage.</param>
/// <param name="Formula">Updated formula expression.</param>
/// <param name="IsTaxable">Updated taxability flag.</param>
/// <param name="IsFnpfApplicable">Updated FNPF applicability flag.</param>
/// <param name="DisplayOrder">Updated display order.</param>
/// <param name="Description">Updated description. Max 500 chars.</param>
public sealed record UpdatePayrollComponentCommand(
    int Id,
    string ComponentName,
    ComponentType ComponentType,
    CalculationMethod CalculationMethod,
    decimal? CalculationValue,
    string? Formula,
    bool IsTaxable,
    bool IsFnpfApplicable,
    int DisplayOrder,
    string? Description
) : IRequest<Result>;
