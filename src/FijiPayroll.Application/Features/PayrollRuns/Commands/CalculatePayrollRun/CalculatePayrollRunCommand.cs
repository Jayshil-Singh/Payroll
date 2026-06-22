using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Services;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Commands.CalculatePayrollRun;

/// <summary>
/// Legacy command name retained for API compatibility. Delegates to the canonical payroll pipeline.
/// </summary>
public sealed record CalculatePayrollRunCommand(
    int PayrollRunId,
    Guid CalculationRequestId
) : IRequest<Result>;

public sealed class CalculatePayrollRunCommandHandler : IRequestHandler<CalculatePayrollRunCommand, Result>
{
    private readonly PayrollPipelineService _pipeline;

    public CalculatePayrollRunCommandHandler(PayrollPipelineService pipeline)
    {
        _pipeline = pipeline;
    }

    public Task<Result> Handle(CalculatePayrollRunCommand request, CancellationToken cancellationToken)
        => _pipeline.ExecuteAsync(request.PayrollRunId, request.CalculationRequestId, cancellationToken);
}
