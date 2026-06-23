using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Features.Users.Commands;
using FijiPayroll.Application.Features.Users.Queries;
using FijiPayroll.WPF.Services;
using FijiPayroll.WPF.ViewModels.Base;
using FijiPayroll.WPF.Views.Dialogs;
using MediatR;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel for the Administration panel.
/// Manages the user account list with live search, toggle active/inactive,
/// password reset, and new user creation capabilities.
/// </summary>
public sealed class AdminViewModel : ViewModelBase
{
    private readonly IMediator           _mediator;
    private readonly ITenantProvider     _tenantProvider;
    private readonly INotificationService _notificationService;
    private readonly IDialogService      _dialogService;

    // ── State ─────────────────────────────────────────────────────────────────

    private string _searchText        = string.Empty;
    private string _statusMessage     = "Load users to begin.";
    private bool   _hasError;
    private UserListItemDto? _selectedUser;

    // ── New User Form ─────────────────────────────────────────────────────────

    private bool   _isNewUserPanelOpen;
    private string _newUsername        = string.Empty;
    private string _newDisplayName     = string.Empty;
    private string _newPassword        = string.Empty;
    private bool   _newIsSystemAdmin;

    public AdminViewModel(
        IMediator mediator,
        ITenantProvider tenantProvider,
        INotificationService notificationService,
        IDialogService dialogService)
    {
        _mediator            = mediator            ?? throw new ArgumentNullException(nameof(mediator));
        _tenantProvider      = tenantProvider      ?? throw new ArgumentNullException(nameof(tenantProvider));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _dialogService       = dialogService       ?? throw new ArgumentNullException(nameof(dialogService));

        Users     = new ObservableCollection<UserListItemDto>();
        UsersView = CollectionViewSource.GetDefaultView(Users);
        UsersView.Filter = FilterUsers;

        LoadUsersCommand      = new AsyncRelayCommand(LoadUsersAsync);
        ToggleStatusCommand   = new AsyncRelayCommand<UserListItemDto>(ToggleStatusAsync, u => u != null);
        ResetPasswordCommand  = new AsyncRelayCommand<UserListItemDto>(ResetPasswordAsync,  u => u != null);
        OpenNewUserCommand    = new RelayCommand(() => IsNewUserPanelOpen = true);
        CancelNewUserCommand  = new RelayCommand(CancelNewUser);
        CreateUserCommand     = new AsyncRelayCommand(CreateUserAsync, CanCreateUser);

        // Auto-load
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Loaded,
            async () => await LoadUsersAsync());
    }

    public string Title => "Administration";

    // ── Commands ──────────────────────────────────────────────────────────────

    public IAsyncRelayCommand                     LoadUsersCommand      { get; }
    public IAsyncRelayCommand<UserListItemDto>    ToggleStatusCommand   { get; }
    public IAsyncRelayCommand<UserListItemDto>    ResetPasswordCommand  { get; }
    public IRelayCommand                          OpenNewUserCommand    { get; }
    public IRelayCommand                          CancelNewUserCommand  { get; }
    public IAsyncRelayCommand                     CreateUserCommand     { get; }

    // ── Collection ────────────────────────────────────────────────────────────

    public ObservableCollection<UserListItemDto> Users     { get; }
    public System.ComponentModel.ICollectionView UsersView { get; }

    // ── Selection / Search ────────────────────────────────────────────────────

    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            UsersView.Refresh();
        }
    }

    public UserListItemDto? SelectedUser
    {
        get => _selectedUser;
        set => SetProperty(ref _selectedUser, value);
    }

    // ── Status ────────────────────────────────────────────────────────────────

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    // ── New User Form ─────────────────────────────────────────────────────────

    public bool IsNewUserPanelOpen
    {
        get => _isNewUserPanelOpen;
        set => SetProperty(ref _isNewUserPanelOpen, value);
    }

    public string NewUsername
    {
        get => _newUsername;
        set { SetProperty(ref _newUsername, value); CreateUserCommand.NotifyCanExecuteChanged(); }
    }

    public string NewDisplayName
    {
        get => _newDisplayName;
        set { SetProperty(ref _newDisplayName, value); CreateUserCommand.NotifyCanExecuteChanged(); }
    }

    public string NewPassword
    {
        get => _newPassword;
        set { SetProperty(ref _newPassword, value); CreateUserCommand.NotifyCanExecuteChanged(); }
    }

    public bool NewIsSystemAdmin
    {
        get => _newIsSystemAdmin;
        set => SetProperty(ref _newIsSystemAdmin, value);
    }

    // ── Filter ────────────────────────────────────────────────────────────────

    private bool FilterUsers(object obj)
    {
        if (obj is not UserListItemDto u) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        var s = SearchText.Trim().ToLowerInvariant();
        return u.DisplayName.ToLowerInvariant().Contains(s)
            || u.Username.ToLowerInvariant().Contains(s)
            || u.PrimaryRole.ToLowerInvariant().Contains(s);
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task LoadUsersAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        HasError = false;
        StatusMessage = "Loading user accounts...";

        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var result = await _mediator.Send(new GetUserListQuery(companyId), ct);

            if (!result.IsSuccess)
            {
                HasError = true;
                StatusMessage = $"Error: {result.Error}";
                return;
            }

            Users.Clear();
            foreach (var dto in result.Value!)
                Users.Add(dto);

            UsersView.Refresh();
            StatusMessage = $"{Users.Count} user account(s) loaded — {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Load failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ToggleStatusAsync(UserListItemDto? dto, CancellationToken ct = default)
    {
        if (dto == null || IsBusy) return;

        bool activate = !dto.IsActive;
        string action = activate ? "activate" : "deactivate";

        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Confirm Status Change",
            $"Are you sure you want to {action} the account for '{dto.DisplayName}'?");

        if (!confirmed) return;

        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new ToggleUserStatusCommand(dto.Id, activate), ct);
            if (!result.IsSuccess)
            {
                _notificationService.Error(result.Error ?? "Unknown error", "Toggle Failed");
                return;
            }

            _notificationService.Success(
                $"Account '{dto.DisplayName}' has been {action}d.",
                "User Updated");

            await LoadUsersAsync(ct);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ResetPasswordAsync(UserListItemDto? dto, CancellationToken ct = default)
    {
        if (dto == null || IsBusy) return;

        // Use a native Windows MessageBox with an InputDialog pattern
        // For desktop WPF app, a simple custom input window is the right approach.
        // We open a lightweight inline prompt via notification + confirm cycle.
        bool proceed = await _dialogService.ShowConfirmationAsync(
            "Reset Password",
            $"Reset the password for '{dto.DisplayName}'? A temporary password will be set.");

        if (!proceed) return;

        // Open a minimal temp-password input window on the UI thread
        string? newPwd = null;
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var dlg = new PasswordInputDialog(dto.DisplayName);
            if (dlg.ShowDialog() == true)
                newPwd = dlg.EnteredPassword;
        });

        if (string.IsNullOrWhiteSpace(newPwd)) return;
        if (newPwd.Length < 8)
        {
            _notificationService.Error("Password must be at least 8 characters.", "Validation");
            return;
        }

        IsBusy = true;
        try
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(newPwd);
            var result = await _mediator.Send(new ResetUserPasswordCommand(dto.Id, hash), ct);

            if (!result.IsSuccess)
            {
                _notificationService.Error(result.Error ?? "Unknown error", "Reset Failed");
                return;
            }

            _notificationService.Success(
                $"Password reset for '{dto.DisplayName}'. They will be prompted to change it on next login.",
                "Password Reset");

            await LoadUsersAsync(ct);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanCreateUser() =>
        !string.IsNullOrWhiteSpace(NewUsername) &&
        !string.IsNullOrWhiteSpace(NewDisplayName) &&
        NewPassword.Length >= 8;

    private async Task CreateUserAsync(CancellationToken ct = default)
    {
        if (IsBusy || !CanCreateUser()) return;

        IsBusy = true;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            string hash   = BCrypt.Net.BCrypt.HashPassword(NewPassword);

            var result = await _mediator.Send(new CreateUserCommand(
                companyId, NewUsername.Trim(), NewDisplayName.Trim(), hash, NewIsSystemAdmin), ct);

            if (!result.IsSuccess)
            {
                _notificationService.Error(result.Error ?? "Unknown error", "Create Failed");
                return;
            }

            _notificationService.Success(
                $"User '{NewDisplayName}' created successfully. They will be prompted to set a new password on first login.",
                "User Created");

            CancelNewUser();
            await LoadUsersAsync(ct);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CancelNewUser()
    {
        NewUsername     = string.Empty;
        NewDisplayName  = string.Empty;
        NewPassword     = string.Empty;
        NewIsSystemAdmin = false;
        IsNewUserPanelOpen = false;
    }
}
