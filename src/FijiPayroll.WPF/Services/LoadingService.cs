using System;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Service managing the loading overlay status.
/// </summary>
public sealed class LoadingService : ILoadingService
{
    private bool _isLoading;
    private string? _message;

    /// <inheritdoc />
    public bool IsLoading => _isLoading;

    /// <inheritdoc />
    public string? Message => _message;

    /// <inheritdoc />
    public event Action? LoadingStateChanged;

    /// <inheritdoc />
    public void Show(string? message = null)
    {
        _isLoading = true;
        _message = message;
        LoadingStateChanged?.Invoke();
    }

    /// <inheritdoc />
    public void Hide()
    {
        _isLoading = false;
        _message = null;
        LoadingStateChanged?.Invoke();
    }
}
