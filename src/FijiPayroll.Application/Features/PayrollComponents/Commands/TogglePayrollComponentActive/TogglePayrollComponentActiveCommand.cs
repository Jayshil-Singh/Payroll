using FijiPayroll.Application.Common.Models;
using MediatR;

namespace FijiPayroll.Application.Features.PayrollComponents.Commands.TogglePayrollComponentActive;

/// <summary>
/// Command to activate or deactivate a payroll component.
/// System components cannot be deactivated — the handler returns a failure result.
/// </summary>
/// <param name="Id">Primary key of the component to toggle.</param>
/// <param name="SetActive">
/// <c>true</c> to activate; <c>false</c> to deactivate.
/// System components cannot be set to <c>false</c>.
/// </param>
public sealed record TogglePayrollComponentActiveCommand(int Id, bool SetActive) : IRequest<Result>;
