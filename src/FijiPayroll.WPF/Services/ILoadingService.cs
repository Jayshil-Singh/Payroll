using System;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Service contract for displaying global busy loading indicator screens.
/// </summary>
public interface ILoadingService
{
    /// <summary>
    /// Gets a value indicating whether loading is in progress.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Gets the optional message description to show on the busy loader screen.
    /// </summary>
    string? Message { get; }

    /// <summary>
    /// Event fired when loading state changes.
    /// </summary>
    event Action? LoadingStateChanged;

    /// <summary>
    /// Shows the global loading overlay with an optional status message.
    /// </summary>
    void Show(string? message = null);

    /// <summary>
    /// Hides the global loading overlay.
    /// </summary>
    void Hide();
}
