using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Platform.Scheduler;

/// <summary>
/// A thread-safe, robust background scheduler that processes daily, weekly, fortnightly, monthly, quarterly, yearly, and manual tasks.
/// </summary>
public sealed class BackgroundScheduler : IDisposable
{
    private readonly ILogger<BackgroundScheduler> _logger;
    private readonly ConcurrentDictionary<string, ScheduledJob> _jobs = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _schedulerLoopTask;
    private int _isStarted;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundScheduler"/> class.
    /// </summary>
    public BackgroundScheduler(ILogger<BackgroundScheduler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers or updates a scheduled background job.
    /// </summary>
    public void RegisterJob(string jobId, Func<CancellationToken, Task> action, string interval, DateTime nextRunTime)
    {
        var job = new ScheduledJob(jobId, action, interval, nextRunTime);
        _jobs.AddOrUpdate(jobId, job, (_, _) => job);
        _logger.LogInformation("Job '{JobId}' registered. Next run scheduled at: {NextRunTime} with interval: {Interval}", jobId, nextRunTime, interval);
    }

    /// <summary>
    /// Triggers a registered job immediately.
    /// </summary>
    public void TriggerJob(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Manually triggering job '{JobId}'", jobId);
                    await job.ExecuteAsync(_cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing manually triggered job '{JobId}'", jobId);
                }
            });
        }
        else
        {
            _logger.LogWarning("Cannot trigger job '{JobId}' because it is not registered.", jobId);
        }
    }

    /// <summary>
    /// Starts the scheduler background loop.
    /// </summary>
    public void Start()
    {
        if (Interlocked.CompareExchange(ref _isStarted, 1, 0) == 0)
        {
            _schedulerLoopTask = Task.Run(RunSchedulerLoopAsync);
            _logger.LogInformation("Background scheduler loop started.");
        }
    }

    /// <summary>
    /// Stops the scheduler background loop.
    /// </summary>
    public void Stop()
    {
        if (Interlocked.CompareExchange(ref _isStarted, 0, 1) == 1)
        {
            _cts.Cancel();
            try
            {
                _schedulerLoopTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
            }
            _logger.LogInformation("Background scheduler loop stopped.");
        }
    }

    private async Task RunSchedulerLoopAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                foreach (var job in _jobs.Values)
                {
                    if (now >= job.NextRunTime && !job.IsRunning)
                    {
                        // Fire and forget execution of the job
                        _ = ExecuteJobAsync(job, _cts.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in scheduler loop step.");
            }

            // Check every 10 seconds
            await Task.Delay(TimeSpan.FromSeconds(10), _cts.Token);
        }
    }

    private async Task ExecuteJobAsync(ScheduledJob job, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting scheduled job '{JobId}'", job.JobId);
            await job.ExecuteAsync(cancellationToken);
            _logger.LogInformation("Completed scheduled job '{JobId}'", job.JobId);

            // Compute next run time
            job.NextRunTime = CalculateNextRunTime(job.NextRunTime, job.Interval);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scheduled job '{JobId}'", job.JobId);
            // In case of error, shift next run time to prevent infinite fast loops
            job.NextRunTime = DateTime.UtcNow.AddMinutes(5);
        }
    }

    private static DateTime CalculateNextRunTime(DateTime baseTime, string interval)
    {
        return interval.ToLowerInvariant() switch
        {
            "daily" => baseTime.AddDays(1),
            "weekly" => baseTime.AddDays(7),
            "fortnightly" => baseTime.AddDays(14),
            "monthly" => baseTime.AddMonths(1),
            "quarterly" => baseTime.AddMonths(3),
            "yearly" => baseTime.AddYears(1),
            _ => baseTime.AddDays(1) // Default fallback
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
        _cts.Dispose();
    }

    private sealed class ScheduledJob
    {
        private readonly Func<CancellationToken, Task> _action;
        private int _isRunningState;

        public string JobId { get; }
        public string Interval { get; }
        public DateTime NextRunTime { get; set; }
        public bool IsRunning => _isRunningState == 1;

        public ScheduledJob(string jobId, Func<CancellationToken, Task> action, string interval, DateTime nextRunTime)
        {
            JobId = jobId;
            _action = action;
            Interval = interval;
            NextRunTime = nextRunTime;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _isRunningState, 1, 0) == 0)
            {
                try
                {
                    await _action(cancellationToken);
                }
                finally
                {
                    Interlocked.Exchange(ref _isRunningState, 0);
                }
            }
        }
    }
}
