using FijiPayroll.Application.Common.Models;
using MediatR;

namespace FijiPayroll.Application.Features.PayrollComponents.Commands.DeletePayrollComponent;

/// <summary>
/// Command to soft-delete a payroll component.
/// System components are protected and will return a failure result.
/// Components used in any payroll run detail records will also be protected.
/// </summary>
/// <param name="Id">Primary key of the component to delete.</param>
public sealed record DeletePayrollComponentCommand(int Id) : IRequest<Result>;
