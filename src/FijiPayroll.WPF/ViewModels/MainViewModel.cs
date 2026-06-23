using FijiPayroll.WPF.ViewModels.Base;
using FijiPayroll.WPF.Services;
using FijiPayroll.WPF.ViewModels.Settings;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using System.Windows;
using FijiPayroll.Application.Features.Notifications.Queries;
using FijiPayroll.Application.Features.Notifications.Commands;
using FijiPayroll.Application.Common.Interfaces;
using MediatR;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// Principal View Model coordinating layout panels, global loaders, toast signals, and navigation bounds.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ILoadingService _loadingService;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly FijiPayroll.Infrastructure.Services.Licensing.LicenseValidator _licenseValidator;
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    private bool _isNotificationFeedOpen;
    private int _unreadNotificationsCount;

    public ObservableCollection<DesktopNotificationDto> RecentNotifications { get; } = new();

    public bool IsNotificationFeedOpen
    {
        get => _isNotificationFeedOpen;
        set => SetProperty(ref _isNotificationFeedOpen, value);
    }

    public int UnreadNotificationsCount
    {
        get => _unreadNotificationsCount;
        set => SetProperty(ref _unreadNotificationsCount, value);
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel(
        INavigationService navigationService,
        ILoadingService loadingService,
        INotificationService notificationService,
        IDialogService dialogService,
        FijiPayroll.Infrastructure.Services.Licensing.LicenseValidator licenseValidator,
        IMediator mediator,
        ITenantProvider tenantProvider)
    {
        _navigationService = navigationService;
        _loadingService = loadingService;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _licenseValidator = licenseValidator;
        _mediator = mediator;
        _tenantProvider = tenantProvider;

        // Subscribe to navigation changes
        _navigationService.CurrentViewChanged += () =>
        {
            OnPropertyChanged(nameof(CurrentViewModel));
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
            OnPropertyChanged(nameof(BreadcrumbPath));
            
            // Re-evaluate history button states
            (GoBackCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (GoForwardCommand as RelayCommand)?.NotifyCanExecuteChanged();
        };

        // Subscribe to global loading states changes
        _loadingService.LoadingStateChanged += () =>
        {
            OnPropertyChanged(nameof(IsLoadingGlobal));
            OnPropertyChanged(nameof(LoadingMessageGlobal));
        };

        // Command registrations
        NavigateDashboardCommand = new RelayCommand(() => _navigationService.NavigateTo<DashboardViewModel>());
        NavigateEmployeeCommand = new RelayCommand(() => _navigationService.NavigateTo<EmployeeViewModel>());
        NavigatePayrollCommand = new RelayCommand(() => _navigationService.NavigateTo<PayrollConsoleViewModel>());
        NavigateSetupCommand = new RelayCommand(() => _navigationService.NavigateTo<SetupViewModel>());
        NavigateCompanySetupWizardCommand = new RelayCommand(() => _navigationService.NavigateTo<CompanySetupDashboardViewModel>());
        
        NavigateReportsCommand = new RelayCommand(() =>
        {
            if (!_licenseValidator.IsLicensed)
            {
                _notificationService.Error("A valid application license is required to access Reports.", "License Error");
                return;
            }
            if (!_licenseValidator.HasFeature("Reports"))
            {
                _notificationService.Error("Your license does not include the Reports module.", "License Restricted");
                return;
            }
            _navigationService.NavigateTo<ReportsViewModel>();
        });

        NavigateApprovalsCommand = new RelayCommand(() => _navigationService.NavigateTo<ApprovalDashboardViewModel>());
        NavigateAdminCommand = new RelayCommand(() => _navigationService.NavigateTo<AdminViewModel>());
        NavigateSettingsCommand = new RelayCommand(() => _navigationService.NavigateTo<SettingsViewModel>());
        NavigateLogViewerCommand = new RelayCommand(() => _navigationService.NavigateTo<LogViewerViewModel>());

        NavigateComplianceCommand = new RelayCommand(() =>
        {
            if (!_licenseValidator.IsLicensed)
            {
                _notificationService.Error("A valid application license is required to access Compliance.", "License Error");
                return;
            }
            if (!_licenseValidator.HasFeature("Compliance"))
            {
                _notificationService.Error("Your license does not include the Compliance Center.", "License Restricted");
                return;
            }
            _navigationService.NavigateTo<ComplianceCenterViewModel>();
        });

        NavigateDiagnosticsCommand = new RelayCommand(() => _navigationService.NavigateTo<DiagnosticsDashboardViewModel>());

        // Navigation History Commands
        GoBackCommand = new RelayCommand(() => _navigationService.GoBack(), () => _navigationService.CanGoBack);
        GoForwardCommand = new RelayCommand(() => _navigationService.GoForward(), () => _navigationService.CanGoForward);

        // Theme Switching Command
        ToggleThemeCommand = new RelayCommand(() =>
        {
            if (System.Windows.Application.Current is App app)
            {
                app.ToggleTheme();
                OnPropertyChanged(nameof(CurrentTheme));
            }
        });

        // Notification Center Commands
        ToggleNotificationFeedCommand = new RelayCommand(() =>
        {
            IsNotificationFeedOpen = !IsNotificationFeedOpen;
            if (IsNotificationFeedOpen)
            {
                _ = LoadNotificationsAsync();
            }
        });
        LoadNotificationsCommand = new AsyncRelayCommand(LoadNotificationsAsync);
        MarkNotificationReadCommand = new AsyncRelayCommand<DesktopNotificationDto>(MarkNotificationReadAsync);
        MarkAllNotificationsReadCommand = new AsyncRelayCommand(MarkAllNotificationsReadAsync);
        CloseNotificationFeedCommand = new RelayCommand(() => IsNotificationFeedOpen = false);
    }

    /// <summary>
    /// Gets the current navigation View Model view target.
    /// </summary>
    public ViewModelBase? CurrentViewModel => _navigationService.CurrentView;

    /// <summary>
    /// Gets a value indicating whether global loading overlay screen is active.
    /// </summary>
    public bool IsLoadingGlobal => _loadingService.IsLoading;

    /// <summary>
    /// Gets the current status message on global loader indicator.
    /// </summary>
    public string? LoadingMessageGlobal => _loadingService.Message;

    /// <summary>
    /// Gets a value indicating whether the user can navigate back in history.
    /// </summary>
    public bool CanGoBack => _navigationService.CanGoBack;

    /// <summary>
    /// Gets a value indicating whether the user can navigate forward in history.
    /// </summary>
    public bool CanGoForward => _navigationService.CanGoForward;

    /// <summary>
    /// Gets the derived breadcrumb path for display.
    /// </summary>
    public string BreadcrumbPath => _navigationService.DerivedBreadcrumbPath;

    /// <summary>
    /// Gets the active theme name.
    /// </summary>
    public string CurrentTheme => (System.Windows.Application.Current as App)?.CurrentTheme ?? "Dark";

    // Role-based visibility flags (permissions mapped, defaults to allowed)
    public bool HasEmployeeAccess => true;
    public bool HasPayrollAccess => true;
    public bool HasSetupAccess => true;
    public bool HasReportsAccess => _licenseValidator.IsLicensed && _licenseValidator.HasFeature("Reports");
    public bool HasComplianceAccess => _licenseValidator.IsLicensed && _licenseValidator.HasFeature("Compliance");
    public bool HasAdminAccess => true;

    // Warning Banner properties
    public bool ShowLicenseWarning => !_licenseValidator.IsLicensed || _licenseValidator.DaysRemaining < 30;

    public string LicenseWarningMessage => _licenseValidator.IsLicensed
        ? $"Warning: Your license for '{_licenseValidator.Company}' expires in {_licenseValidator.DaysRemaining} days (Expiry: {_licenseValidator.ExpiryDate:yyyy-MM-dd}). Please renew."
        : $"Warning: {_licenseValidator.ErrorMessage}";

    // Sidebar interaction commands
    public ICommand NavigateDashboardCommand { get; }
    public ICommand NavigateEmployeeCommand { get; }
    public ICommand NavigatePayrollCommand { get; }
    public ICommand NavigateSetupCommand { get; }
    public ICommand NavigateCompanySetupWizardCommand { get; }
    public ICommand NavigateReportsCommand { get; }
    public ICommand NavigateApprovalsCommand { get; }
    public ICommand NavigateAdminCommand { get; }
    public ICommand NavigateSettingsCommand { get; }
    public ICommand NavigateLogViewerCommand { get; }
    public ICommand NavigateComplianceCommand { get; }
    public ICommand NavigateDiagnosticsCommand { get; }

    // History and Utility Commands
    public ICommand GoBackCommand { get; }
    public ICommand GoForwardCommand { get; }
    public ICommand ToggleThemeCommand { get; }

    // Notification Center Commands
    public ICommand ToggleNotificationFeedCommand { get; }
    public ICommand LoadNotificationsCommand { get; }
    public ICommand MarkNotificationReadCommand { get; }
    public ICommand MarkAllNotificationsReadCommand { get; }
    public ICommand CloseNotificationFeedCommand { get; }

    // ── Notification Async Methods ─────────────────────────────────────────

    private async Task LoadNotificationsAsync()
    {
        try
        {
            var companyId = _tenantProvider.GetCurrentCompanyId();
            var result = await _mediator.Send(new GetDesktopNotificationsQuery(companyId, 30));
            if (result.IsSuccess)
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    RecentNotifications.Clear();
                    foreach (var n in result.Value!)
                        RecentNotifications.Add(n);
                    UnreadNotificationsCount = result.Value!.Count(n => !n.IsRead);
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NotificationFeed] Load failed: {ex.Message}");
        }
    }

    private async Task MarkNotificationReadAsync(DesktopNotificationDto? dto)
    {
        if (dto == null || dto.IsRead) return;
        try
        {
            var result = await _mediator.Send(new MarkNotificationReadCommand(dto.Id));
            if (result.IsSuccess)
            {
                dto.IsRead = true;
                UnreadNotificationsCount = RecentNotifications.Count(n => !n.IsRead);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NotificationFeed] MarkRead failed: {ex.Message}");
        }
    }

    private async Task MarkAllNotificationsReadAsync()
    {
        try
        {
            var companyId = _tenantProvider.GetCurrentCompanyId();
            var result = await _mediator.Send(new MarkAllNotificationsReadCommand(companyId));
            if (result.IsSuccess)
            {
                foreach (var n in RecentNotifications)
                    n.IsRead = true;
                UnreadNotificationsCount = 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NotificationFeed] MarkAllRead failed: {ex.Message}");
        }
    }
}

