using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.WPF.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FijiPayroll.WPF.Commands;

/// <summary>
/// Base asynchronous command integrating with loading spinner and exceptions tracing.
/// </summary>
public abstract class BaseAsyncCommand : ICommand
{
    private readonly ILoadingService? _loadingService;
    private readonly ILogger? _logger;
    private readonly ICurrentUserService? _currentUserService;
    private readonly string? _requiredPermission;
    private bool _isExecuting;

    protected BaseAsyncCommand(
        ILoadingService? loadingService = null,
        ILogger? logger = null,
        ICurrentUserService? currentUserService = null,
        string? requiredPermission = null)
    {
        _loadingService = loadingService;
        _logger = logger;
        _currentUserService = currentUserService;
        _requiredPermission = requiredPermission;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            if (_isExecuting == value) return;
            _isExecuting = value;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <inheritdoc />
    public virtual bool CanExecute(object? parameter)
    {
        if (IsExecuting) return false;

        if (_currentUserService != null && !string.IsNullOrEmpty(_requiredPermission))
        {
            return _currentUserService.HasPermission(_requiredPermission);
        }

        return true;
    }

    /// <inheritdoc />
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        IsExecuting = true;
        _loadingService?.Show(GetLoadingMessage());

        try
        {
            await ExecuteAsync(parameter);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Asynchronous command execution failed in {CommandType}.", GetType().Name);
            HandleException(ex);
        }
        finally
        {
            _loadingService?.Hide();
            IsExecuting = false;
        }
    }

    protected abstract Task ExecuteAsync(object? parameter);

    protected virtual string GetLoadingMessage() => "Executing payroll operation, please wait...";

    protected virtual void HandleException(Exception exception)
    {
        // Exception is caught and logged; subclasses can override for custom dialog popups.
    }
}

/// <summary>
/// Delegate implementation of BaseAsyncCommand for passing async methods.
/// </summary>
public sealed class DelegateAsyncCommand : BaseAsyncCommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private readonly string? _loadingMessage;

    public DelegateAsyncCommand(
        Func<object?, Task> execute,
        Func<object?, bool>? canExecute = null,
        ILoadingService? loadingService = null,
        ILogger? logger = null,
        ICurrentUserService? currentUserService = null,
        string? requiredPermission = null,
        string? loadingMessage = null)
        : base(loadingService, logger, currentUserService, requiredPermission)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _loadingMessage = loadingMessage;
    }

    public override bool CanExecute(object? parameter)
    {
        if (!base.CanExecute(parameter)) return false;
        return _canExecute == null || _canExecute(parameter);
    }

    protected override Task ExecuteAsync(object? parameter)
    {
        return _execute(parameter);
    }

    protected override string GetLoadingMessage()
    {
        return _loadingMessage ?? base.GetLoadingMessage();
    }
}
