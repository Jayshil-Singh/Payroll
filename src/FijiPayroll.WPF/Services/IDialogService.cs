using System;
using System.Threading.Tasks;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Service coordinating overlay messages and confirmation dialogs in WPF.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Event fired when a basic message box dialog is requested.
    /// </summary>
    event Action<string, string, Action>? MessageRequested;

    /// <summary>
    /// Event fired when a confirmation (Yes/No) dialog is requested.
    /// </summary>
    event Action<string, string, Action<bool>>? ConfirmationRequested;

    /// <summary>
    /// Shows an alert dialog and awaits the user closing it.
    /// </summary>
    Task ShowMessageAsync(string title, string message);

    /// <summary>
    /// Shows a confirmation dialog and awaits the user's choice.
    /// </summary>
    /// <returns>True if user confirmed, false otherwise.</returns>
    Task<bool> ShowConfirmationAsync(string title, string message);
}
