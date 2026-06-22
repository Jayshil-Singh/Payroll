using FijiPayroll.WPF.ViewModels.Base;
using FijiPayroll.WPF.Services;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using System.Windows;

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

    /// <summary>
    /// Initialises a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel(
        INavigationService navigationService,
        ILoadingService loadingService,
        INotificationService notificationService,
        IDialogService dialogService)
    {
        _navigationService = navigationService;
        _loadingService = loadingService;
        _notificationService = notificationService;
        _dialogService = dialogService;

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
        NavigateReportsCommand = new RelayCommand(() => _navigationService.NavigateTo<ReportsViewModel>());
        NavigateApprovalsCommand = new RelayCommand(() => _navigationService.NavigateTo<ApprovalDashboardViewModel>());
        NavigateAdminCommand = new RelayCommand(() => _navigationService.NavigateTo<AdminViewModel>());
        NavigateLogViewerCommand = new RelayCommand(() => _navigationService.NavigateTo<LogViewerViewModel>());
        NavigateComplianceCommand = new RelayCommand(() => _navigationService.NavigateTo<ComplianceCenterViewModel>());
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
    public bool HasReportsAccess => true;
    public bool HasAdminAccess => true;

    // Sidebar interaction commands
    public ICommand NavigateDashboardCommand { get; }
    public ICommand NavigateEmployeeCommand { get; }
    public ICommand NavigatePayrollCommand { get; }
    public ICommand NavigateSetupCommand { get; }
    public ICommand NavigateCompanySetupWizardCommand { get; }
    public ICommand NavigateReportsCommand { get; }
    public ICommand NavigateApprovalsCommand { get; }
    public ICommand NavigateAdminCommand { get; }
    public ICommand NavigateLogViewerCommand { get; }
    public ICommand NavigateComplianceCommand { get; }
    public ICommand NavigateDiagnosticsCommand { get; }

    // History and Utility Commands
    public ICommand GoBackCommand { get; }
    public ICommand GoForwardCommand { get; }
    public ICommand ToggleThemeCommand { get; }
}

