using FijiPayroll.Domain.Entities.Common;

namespace FijiPayroll.Domain.Entities.Audit;

/// <summary>
/// Tracks execution counts, latencies, and failures for business rules.
/// </summary>
public sealed class RuleExecutionMetric : BaseEntity
{
    private RuleExecutionMetric() { }

    public RuleExecutionMetric(
        int ruleId,
        long executionCount,
        double averageExecutionTime,
        double maximumExecutionTime,
        long failureCount,
        DateTime lastExecuted)
    {
        RuleId = ruleId;
        ExecutionCount = executionCount;
        AverageExecutionTime = averageExecutionTime;
        MaximumExecutionTime = maximumExecutionTime;
        FailureCount = failureCount;
        LastExecuted = lastExecuted;
    }

    public int RuleId { get; private set; }
    public long ExecutionCount { get; private set; }
    public double AverageExecutionTime { get; private set; }
    public double MaximumExecutionTime { get; private set; }
    public long FailureCount { get; private set; }
    public DateTime LastExecuted { get; private set; }

    /// <summary>
    /// Records a new execution metric sample.
    /// </summary>
    public void UpdateMetrics(double durationMs, bool success)
    {
        var totalTime = AverageExecutionTime * ExecutionCount + durationMs;
        ExecutionCount++;
        AverageExecutionTime = totalTime / ExecutionCount;
        if (durationMs > MaximumExecutionTime)
        {
            MaximumExecutionTime = durationMs;
        }
        if (!success)
        {
            FailureCount++;
        }
        LastExecuted = DateTime.UtcNow;
    }
}
