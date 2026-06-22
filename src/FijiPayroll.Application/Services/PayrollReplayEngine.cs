using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using System;
using System.Linq;
using System.Text.Json;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Execution engine for loading a frozen payroll snapshot, running computation, and asserting hash equivalence.
/// </summary>
public sealed class PayrollReplayEngine
{
    private readonly PayrollCalculationEngine _calculationEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="PayrollReplayEngine"/> class.
    /// </summary>
    public PayrollReplayEngine(PayrollCalculationEngine calculationEngine)
    {
        _calculationEngine = calculationEngine ?? throw new ArgumentNullException(nameof(calculationEngine));
    }

    /// <summary>
    /// Replays a frozen snapshot and verifies that the recalculated hash matches the stored snapshot hash.
    /// </summary>
    public bool Replay(
        PayrollSnapshot snapshot,
        out string calculatedHash,
        out decimal totalGross,
        out decimal totalPAYE,
        out decimal totalNet)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

        var context = JsonSerializer.Deserialize<PayrollExecutionContext>(snapshot.JsonPayload);
        if (context == null)
        {
            throw new InvalidOperationException("Failed to deserialize payroll execution context from snapshot.");
        }

        // Run calculations stateless in-memory
        var result = _calculationEngine.Calculate(context);

        // Compute snapshot hash
        calculatedHash = PayrollSnapshotHasher.GenerateHash(context.Employees, context.TaxVersion, context.Components);

        totalGross = result.Employees.Sum(x => x.GrossPay);
        totalPAYE = result.Employees.Sum(x => x.PayeTax);
        totalNet = result.Employees.Sum(x => x.NetPay);

        return string.Equals(calculatedHash, snapshot.Hash, StringComparison.OrdinalIgnoreCase);
    }
}
