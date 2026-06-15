using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Model representing a captured diagnostic log entry in the system.
/// </summary>
public sealed record LogEntry(
    DateTime Timestamp,
    LogLevel Level,
    string Category,
    string Message,
    Exception? Exception);

/// <summary>
/// Service coordinating the in-memory log buffer for real-time diagnostics.
/// </summary>
public interface ILogBuffer
{
    /// <summary>
    /// Event raised when a new log entry is appended to the buffer.
    /// </summary>
    event Action<LogEntry>? LogAdded;

    /// <summary>
    /// Appends a new log entry to the buffer.
    /// </summary>
    void Write(LogEntry entry);

    /// <summary>
    /// Retrieves a snapshot of all currently stored log entries.
    /// </summary>
    IReadOnlyList<LogEntry> GetLogs();

    /// <summary>
    /// Clears all log entries from the buffer.
    /// </summary>
    void Clear();
}
