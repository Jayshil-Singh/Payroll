using FijiPayroll.Application.Common.Interfaces;
using System;
using System.Windows.Input;

namespace FijiPayroll.WPF.Commands;

/// <summary>
/// Base synchronous command providing claim checks and pre-execution hooks.
/// </summary>
public abstract class BaseCommand : ICommand
{
    private readonly ICurrentUserService? _currentUserService;
    private readonly string? _requiredPermission;

    protected BaseCommand(ICurrentUserService? currentUserService = null, string? requiredPermission = null)
    {
        _currentUserService = currentUserService;
        _requiredPermission = requiredPermission;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <inheritdoc />
    public virtual bool CanExecute(object? parameter)
    {
        if (_currentUserService != null && !string.IsNullOrEmpty(_requiredPermission))
        {
            return _currentUserService.HasPermission(_requiredPermission);
        }
        return true;
    }

    /// <inheritdoc />
    public abstract void Execute(object? parameter);
}

/// <summary>
/// Delegate implementation of BaseCommand for passing action methods.
/// </summary>
public sealed class DelegateCommand : BaseCommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public DelegateCommand(
        Action<object?> execute,
        Func<object?, bool>? canExecute = null,
        ICurrentUserService? currentUserService = null,
        string? requiredPermission = null)
        : base(currentUserService, requiredPermission)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public override bool CanExecute(object? parameter)
    {
        if (!base.CanExecute(parameter)) return false;
        return _canExecute == null || _canExecute(parameter);
    }

    public override void Execute(object? parameter)
    {
        _execute(parameter);
    }
}
