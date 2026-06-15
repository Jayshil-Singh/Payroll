using System;
using System.Collections.Generic;
using System.Linq;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Thread-safe in-memory ring-buffer logging sink capped at 10,000 entries.
/// </summary>
public sealed class LogBuffer : ILogBuffer
{
    private const int MaxEntries = 10000;
    private readonly Queue<LogEntry> _logs = new(MaxEntries);
    private readonly object _lock = new();

    /// <inheritdoc />
    public event Action<LogEntry>? LogAdded;

    /// <inheritdoc />
    public void Write(LogEntry entry)
    {
        bool wasAdded = false;

        lock (_lock)
        {
            // Enforce ring-buffer cap
            while (_logs.Count >= MaxEntries)
            {
                _logs.Dequeue();
            }

            _logs.Enqueue(entry);
            wasAdded = true;
        }

        if (wasAdded)
        {
            LogAdded?.Invoke(entry);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<LogEntry> GetLogs()
    {
        lock (_lock)
        {
            return _logs.ToList().AsReadOnly();
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _logs.Clear();
        }
    }
}
