using System.Threading;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Execution context wrapper to track ongoing reset operations across handlers.
/// </summary>
public static class ResetOperationContext
{
    private static readonly ThreadLocal<bool> _isResetting = new(() => false);

    /// <summary>
    /// Gets or sets a value indicating whether the current execution thread is resetting a payroll run.
    /// </summary>
    public static bool IsResetting
    {
        get => _isResetting.Value;
        set => _isResetting.Value = value;
    }
}
