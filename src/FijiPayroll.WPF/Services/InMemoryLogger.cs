using Microsoft.Extensions.Logging;
using System;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Logger provider creating instances of InMemoryLogger to write diagnostics to the shared ILogBuffer.
/// </summary>
public sealed class InMemoryLoggerProvider : ILoggerProvider
{
    private readonly ILogBuffer _logBuffer;

    public InMemoryLoggerProvider(ILogBuffer logBuffer)
    {
        _logBuffer = logBuffer;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return new InMemoryLogger(categoryName, _logBuffer);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No-op
    }
}

/// <summary>
/// Custom ILogger writing diagnostic messages directly into the ring-buffer.
/// </summary>
public sealed class InMemoryLogger : ILogger
{
    private readonly string _category;
    private readonly ILogBuffer _logBuffer;

    public InMemoryLogger(string category, ILogBuffer logBuffer)
    {
        _category = category;
        _logBuffer = logBuffer;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null; // Scopes not supported in this simple buffer
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        string message = formatter(state, exception);
        var entry = new LogEntry(
            DateTime.Now, // Use local time for user display in UI viewer
            logLevel,
            _category,
            message,
            exception);

        // Write is thread-safe and non-blocking O(1)
        _logBuffer.Write(entry);
    }
}
