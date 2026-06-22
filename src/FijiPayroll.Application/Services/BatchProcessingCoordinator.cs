using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Thread-safe coordinator managing the runtime state of active parallel batch processing calculations.
/// Supports pausing, resuming, querying progress, and cancellation check signals.
/// </summary>
public sealed class BatchProcessingCoordinator
{
    private readonly ConcurrentDictionary<int, BatchProcessingState> _states = new();

    /// <summary>
    /// Gets or creates the batch processing state for a given run ID.
    /// </summary>
    public BatchProcessingState GetOrCreateState(int runId)
    {
        return _states.GetOrAdd(runId, id => new BatchProcessingState(id));
    }

    /// <summary>
    /// Removes the tracking state for a completed or aborted run ID.
    /// </summary>
    public void RemoveState(int runId)
    {
        _states.TryRemove(runId, out _);
    }
}

/// <summary>
/// Running state of a single payroll batch run process.
/// </summary>
public sealed class BatchProcessingState
{
    private volatile bool _isPaused;
    private readonly SemaphoreSlim _pauseSignal = new(1, 1);

    /// <summary>Gets the unique payroll run ID.</summary>
    public int PayrollRunId { get; }

    /// <summary>Gets or sets the current execution progress percentage (0 to 100).</summary>
    public double Progress { get; set; }

    /// <summary>Gets or sets a value indicating whether the execution is paused.</summary>
    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (_isPaused == value) return;
            _isPaused = value;
            if (_isPaused)
            {
                // Acquire/lock the semaphore to pause workers
                _pauseSignal.Wait();
            }
            else
            {
                // Release/signal the semaphore to resume workers
                _pauseSignal.Release();
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchProcessingState"/> class.
    /// </summary>
    public BatchProcessingState(int payrollRunId)
    {
        PayrollRunId = payrollRunId;
    }

    /// <summary>
    /// Pauses worker threads if the state is configured as paused.
    /// </summary>
    public async Task WaitIfPausedAsync(CancellationToken cancellationToken)
    {
        if (_isPaused)
        {
            await _pauseSignal.WaitAsync(cancellationToken);
            _pauseSignal.Release();
        }
    }
}
