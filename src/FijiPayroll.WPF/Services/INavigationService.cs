using System;
using FijiPayroll.WPF.ViewModels.Base;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Service contract for WPF shell view model navigation with history and breadcrumbs.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets the currently active view model.
    /// </summary>
    ViewModelBase? CurrentView { get; }

    /// <summary>
    /// Gets a value indicating whether back navigation is possible.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Gets a value indicating whether forward navigation is possible.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// Gets the derived breadcrumb trail based on the current view model.
    /// </summary>
    string DerivedBreadcrumbPath { get; }

    /// <summary>
    /// Event raised when the current view model navigation target changes.
    /// </summary>
    event Action? CurrentViewChanged;

    /// <summary>
    /// Navigates to the specified view model type by resolving it from DI.
    /// </summary>
    /// <typeparam name="TViewModel">The type of view model to navigate to.</typeparam>
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;

    /// <summary>
    /// Navigates to the specified view model type, optionally preserving history stacks.
    /// </summary>
    void NavigateTo(Type viewModelType, bool clearForwardHistory = true);

    /// <summary>
    /// Navigates back in history.
    /// </summary>
    void GoBack();

    /// <summary>
    /// Navigates forward in history.
    /// </summary>
    void GoForward();

    /// <summary>
    /// Restores the last active view model state from saved configurations.
    /// </summary>
    void RestoreLastState();
}

