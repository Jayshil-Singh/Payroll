using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Thread-safe manager to coordinate, execute, and monitor background tasks in a bounded concurrency execution pattern.
/// </summary>
public sealed class BackgroundJobManager
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundJobManager> _logger;
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _activeJobs = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundJobManager"/> class.
    /// </summary>
    public BackgroundJobManager(IServiceScopeFactory scopeFactory, ILogger<BackgroundJobManager> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Enqueues a new background job.
    /// </summary>
    public async Task<int> QueueJobAsync(int companyId, string jobType, string? parameters, string createdBy, DateTime scheduledUtc)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var job = BackgroundJob.Create(companyId, jobType, parameters, scheduledUtc, createdBy);
        await unitOfWork.BackgroundJobs.AddAsync(job);
        await unitOfWork.SaveChangesAsync();

        if (scheduledUtc <= DateTime.UtcNow)
        {
            _ = Task.Run(() => ExecuteJobAsync(job.Id));
        }

        return job.Id;
    }

    /// <summary>
    /// Executes a job by loading it from the database and running its worker logic.
    /// </summary>
    public async Task ExecuteJobAsync(int jobId)
    {
        var cts = new CancellationTokenSource();
        if (!_activeJobs.TryAdd(jobId, cts))
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var job = await unitOfWork.BackgroundJobs.GetByIdAsync(jobId);

        if (job == null || (job.Status != "Queued" && job.Status != "Running"))
        {
            _activeJobs.TryRemove(jobId, out _);
            return;
        }

        try
        {
            job.Start();
            unitOfWork.BackgroundJobs.Update(job);
            await unitOfWork.SaveChangesAsync();

            await RunWorkerLogicAsync(job, scope, cts.Token);

            job.Complete();
            unitOfWork.BackgroundJobs.Update(job);
            await unitOfWork.SaveChangesAsync();
        }
        catch (OperationCanceledException)
        {
            job.Cancel();
            unitOfWork.BackgroundJobs.Update(job);
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background job {JobId} of type {JobType} failed.", jobId, job.JobType);
            job.Fail(ex.Message);
            unitOfWork.BackgroundJobs.Update(job);
            await unitOfWork.SaveChangesAsync();
        }
        finally
        {
            _activeJobs.TryRemove(jobId, out _);
            cts.Dispose();
        }
    }

    /// <summary>
    /// Cancels a running background job.
    /// </summary>
    public void CancelJob(int jobId)
    {
        if (_activeJobs.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
        }
    }

    private async Task RunWorkerLogicAsync(BackgroundJob job, IServiceScope scope, CancellationToken cancellationToken)
    {
        // Simulate progress intervals for execution task representation
        for (int i = 1; i <= 5; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(100, cancellationToken);
            job.UpdateProgress(i * 20);

            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            unitOfWork.BackgroundJobs.Update(job);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
