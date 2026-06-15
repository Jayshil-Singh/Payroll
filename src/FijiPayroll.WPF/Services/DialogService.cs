using System;
using System.Threading.Tasks;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Implementation of IDialogService that leverages TaskCompletionSource to allow async await dialog flows.
/// </summary>
public sealed class DialogService : IDialogService
{
    /// <inheritdoc />
    public event Action<string, string, Action>? MessageRequested;

    /// <inheritdoc />
    public event Action<string, string, Action<bool>>? ConfirmationRequested;

    /// <inheritdoc />
    public Task ShowMessageAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource();
        MessageRequested?.Invoke(title, message, () => tcs.SetResult());
        return tcs.Task;
    }

    /// <inheritdoc />
    public Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource<bool>();
        ConfirmationRequested?.Invoke(title, message, result => tcs.SetResult(result));
        return tcs.Task;
    }
}
