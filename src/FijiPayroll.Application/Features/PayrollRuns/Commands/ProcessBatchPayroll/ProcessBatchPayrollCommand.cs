using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Services;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Commands.ProcessBatchPayroll;

/// <summary>
/// Batch payroll command. Delegates to the canonical payroll pipeline (same path as CalculatePayrollRun).
/// </summary>
public sealed record ProcessBatchPayrollCommand(
    int PayrollRunId,
    Guid CalculationRequestId
) : IRequest<Result>;

public sealed class ProcessBatchPayrollCommandHandler : IRequestHandler<ProcessBatchPayrollCommand, Result>
{
    private readonly PayrollPipelineService _pipeline;

    public ProcessBatchPayrollCommandHandler(PayrollPipelineService pipeline)
    {
        _pipeline = pipeline;
    }

    public Task<Result> Handle(ProcessBatchPayrollCommand request, CancellationToken cancellationToken)
        => _pipeline.ExecuteAsync(request.PayrollRunId, request.CalculationRequestId, cancellationToken);
}
