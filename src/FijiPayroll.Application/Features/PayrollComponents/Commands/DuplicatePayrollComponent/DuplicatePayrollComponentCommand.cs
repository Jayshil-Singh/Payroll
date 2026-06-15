using FijiPayroll.Application.Common.Models;
using MediatR;

namespace FijiPayroll.Application.Features.PayrollComponents.Commands.DuplicatePayrollComponent;

/// <summary>
/// Command to create a copy of an existing payroll component with a new code and name.
/// The duplicate will have <c>IsSystemComponent = false</c> and will be active.
/// </summary>
/// <param name="SourceId">Primary key of the component to clone.</param>
/// <param name="NewCode">Unique code for the new component. Max 20 chars, uppercase.</param>
/// <param name="NewName">Display name for the new component. Max 200 chars.</param>
public sealed record DuplicatePayrollComponentCommand(
    int SourceId,
    string NewCode,
    string NewName
) : IRequest<Result<int>>;
