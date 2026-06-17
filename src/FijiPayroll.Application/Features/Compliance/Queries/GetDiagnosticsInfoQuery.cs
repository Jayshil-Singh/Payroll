using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using MediatR;

namespace FijiPayroll.Application.Features.Compliance.Queries;

/// <summary>Represents diagnostics HUD layout values.</summary>
public sealed record DiagnosticsInfoModel(
    int HealthScore,
    long DatabaseLatencyMs,
    double MemoryUsedMb,
    int BusyWorkerThreads,
    int ActiveWorkerThreads,
    int PendingJobsCount,
    int PendingNotificationsCount,
    double CpuUsagePercent
);

/// <summary>
/// CQRS Query to retrieve real-time system performance diagnostics and thread metrics.
/// </summary>
public sealed record GetDiagnosticsInfoQuery : IRequest<Result<DiagnosticsInfoModel>>;

/// <summary>
/// Handles GetDiagnosticsInfoQuery.
/// </summary>
public sealed class GetDiagnosticsInfoQueryHandler : IRequestHandler<GetDiagnosticsInfoQuery, Result<DiagnosticsInfoModel>>
{
    private readonly IUnitOfWork _unitOfWork;
    private static readonly object CpuLock = new();
    private static TimeSpan _lastCpuTime = TimeSpan.Zero;
    private static DateTime _lastSampleTime = DateTime.MinValue;
    private static double _lastCpuUsagePercent = 2.5;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDiagnosticsInfoQueryHandler"/> class.
    /// </summary>
    public GetDiagnosticsInfoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc/>
    public async Task<Result<DiagnosticsInfoModel>> Handle(GetDiagnosticsInfoQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Measure database query latency using the active jobs count repository query
            var sw = Stopwatch.StartNew();
            _ = await _unitOfWork.Compliance.GetActiveJobsCountAsync(cancellationToken);
            sw.Stop();
            long dbLatencyMs = sw.ElapsedMilliseconds;

            // Get process memory metrics
            using var process = Process.GetCurrentProcess();
            long memoryUsedBytes = process.WorkingSet64;
            double memoryUsedMb = Math.Round((double)memoryUsedBytes / (1024 * 1024), 2);

            // Get Thread Pool counts
            int activeWorkerThreads = (int)ThreadPool.ThreadCount;
            ThreadPool.GetAvailableThreads(out int availableWorkerThreads, out int availableIoThreads);
            ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxIoThreads);
            int busyWorkerThreads = maxWorkerThreads - availableWorkerThreads;

            // Count pending compliance jobs
            int pendingJobsCount = await _unitOfWork.Compliance.GetActiveJobsCountAsync(cancellationToken);

            int pendingNotificationsCount = await _unitOfWork.Compliance.GetPendingNotificationsCountAsync(cancellationToken);

            // Calculate real-time CPU usage
            double cpuUsagePercent = CalculateCpuUsage();

            // Compute system health score
            double healthScore = 100.0;
            if (dbLatencyMs > 200) healthScore -= 10;
            if (pendingJobsCount > 10) healthScore -= 5;
            if (busyWorkerThreads > 20) healthScore -= 5;
            healthScore = Math.Max(0, healthScore);

            var model = new DiagnosticsInfoModel(
                HealthScore: (int)healthScore,
                DatabaseLatencyMs: dbLatencyMs,
                MemoryUsedMb: memoryUsedMb,
                BusyWorkerThreads: busyWorkerThreads,
                ActiveWorkerThreads: activeWorkerThreads,
                PendingJobsCount: pendingJobsCount,
                PendingNotificationsCount: pendingNotificationsCount,
                CpuUsagePercent: cpuUsagePercent
            );

            return Result<DiagnosticsInfoModel>.Success(model);
        }
        catch (Exception ex)
        {
            return Result<DiagnosticsInfoModel>.Failure($"Diagnostics query failed: {ex.Message}");
        }
    }

    private static double CalculateCpuUsage()
    {
        lock (CpuLock)
        {
            var now = DateTime.UtcNow;
            using var process = Process.GetCurrentProcess();
            var cpuTime = process.TotalProcessorTime;

            if (_lastSampleTime == DateTime.MinValue)
            {
                _lastCpuTime = cpuTime;
                _lastSampleTime = now;
                return _lastCpuUsagePercent;
            }

            var elapsed = now - _lastSampleTime;
            if (elapsed.TotalMilliseconds <= 100)
            {
                return _lastCpuUsagePercent;
            }

            var systemTimeDelta = elapsed.TotalMilliseconds * Environment.ProcessorCount;
            var processTimeDelta = (cpuTime - _lastCpuTime).TotalMilliseconds;

            double usage = (processTimeDelta / systemTimeDelta) * 100.0;
            _lastCpuUsagePercent = Math.Clamp(Math.Round(usage, 1), 0.0, 100.0);
            _lastCpuTime = cpuTime;
            _lastSampleTime = now;

            return _lastCpuUsagePercent;
        }
    }
}
