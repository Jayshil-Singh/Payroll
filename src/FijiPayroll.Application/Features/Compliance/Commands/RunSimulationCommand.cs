using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Services;
using MediatR;

namespace FijiPayroll.Application.Features.Compliance.Commands;

/// <summary>
/// CQRS Command to execute statutory rule changes simulation.
/// </summary>
public sealed record RunSimulationCommand(
    int CompanyId,
    int PayrollRunId,
    List<RuleSimulationEngine.RuleOverride> Overrides
) : IRequest<Result<RuleSimulationEngine.RuleSimulationResult>>;

/// <summary>
/// Handles RunSimulationCommand.
/// </summary>
public sealed class RunSimulationCommandHandler : IRequestHandler<RunSimulationCommand, Result<RuleSimulationEngine.RuleSimulationResult>>
{
    private readonly RuleSimulationEngine _simulationEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunSimulationCommandHandler"/> class.
    /// </summary>
    public RunSimulationCommandHandler(RuleSimulationEngine simulationEngine)
    {
        _simulationEngine = simulationEngine ?? throw new ArgumentNullException(nameof(simulationEngine));
    }

    /// <inheritdoc/>
    public async Task<Result<RuleSimulationEngine.RuleSimulationResult>> Handle(RunSimulationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var simResult = await _simulationEngine.SimulateRuleChangeAsync(
                request.CompanyId,
                request.PayrollRunId,
                request.Overrides,
                cancellationToken);

            return Result<RuleSimulationEngine.RuleSimulationResult>.Success(simResult);
        }
        catch (Exception ex)
        {
            return Result<RuleSimulationEngine.RuleSimulationResult>.Failure($"Simulation execution failed: {ex.Message}");
        }
    }
}
