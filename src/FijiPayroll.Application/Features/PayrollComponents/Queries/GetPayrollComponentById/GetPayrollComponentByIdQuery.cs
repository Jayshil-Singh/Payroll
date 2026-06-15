using FijiPayroll.Application.Common.Models;
using MediatR;

namespace FijiPayroll.Application.Features.PayrollComponents.Queries.GetPayrollComponentById;

/// <summary>
/// Query to retrieve the full details of a single payroll component.
/// </summary>
/// <param name="Id">Primary key of the component.</param>
public sealed record GetPayrollComponentByIdQuery(int Id)
    : IRequest<Result<PayrollComponentDetailDto>>;
