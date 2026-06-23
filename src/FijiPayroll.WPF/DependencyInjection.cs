using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Services;
using FijiPayroll.Persistence.Interceptors;
using FijiPayroll.WPF.Infrastructure;
using FijiPayroll.WPF.Services;
using FijiPayroll.WPF.ViewModels;
using FijiPayroll.WPF.ViewModels.Auth;
using FijiPayroll.WPF.ViewModels.Settings;
using FijiPayroll.WPF.Views;
using FijiPayroll.WPF.Views.Auth;
using FijiPayroll.WPF.Views.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace FijiPayroll.WPF;

/// <summary>
/// Dependency Injection registration for the WPF presentation layer.
/// Registers all views, view models, UI services, and infrastructure monitors.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers WPF view models, views, UI services, and hardening infrastructure
    /// into the DI container.
    /// </summary>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        // ── Core UI Services (Singletons) ────────────────────────────────────
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ILoadingService, LoadingService>();
        services.AddSingleton<IApplicationStateStore, ApplicationStateStore>();
        services.AddSingleton<ITenantProvider, WpfTenantProvider>();
        services.AddSingleton<SessionManager>();
        services.AddSingleton<IAuthSessionStore, AuthSessionStore>();
        services.AddSingleton<WpfSessionIdentityService>();
        services.AddSingleton<ICurrentUserService>(sp => sp.GetRequiredService<WpfSessionIdentityService>());
        services.AddSingleton<ICurrentUserAccessor>(sp => sp.GetRequiredService<WpfSessionIdentityService>());

        // ILogBuffer is registered in App.xaml.cs before BuildServiceProvider (needs shared instance)
        // so we do not re-register it here.

        // ── Infrastructure Monitors (Singletons) ─────────────────────────────
        // PriorityDispatcherQueue is instantiated in App.xaml.cs and passed in directly.
        // The following are registered via App.xaml.cs before AddPresentation:
        //   - PriorityDispatcherQueue
        //   - SystemHealthMonitor
        //   - SystemIntegrityValidator
        //   - MemorySmoothingScheduler
        //   - ViewModelLeakDetector

        // ── ViewModels (Main & Module Shells) ────────────────────────────────
        services.AddSingleton<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ESSHomeViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<EmployeeViewModel>();
        services.AddTransient<PayrollViewModel>();
        services.AddTransient<SetupViewModel>();
        services.AddTransient<CompanySetupDashboardViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<AdminViewModel>();
        services.AddTransient<LogViewerViewModel>();
        services.AddTransient<ComplianceCenterViewModel>();
        services.AddTransient<DiagnosticsDashboardViewModel>();
        services.AddTransient<PayrollConsoleViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Existing feature ViewModels
        services.AddTransient<PayrollComponentViewModel>();
        // PayrollComponentFormViewModel requires a runtime companyId; resolved via factory
        services.AddTransient<PayrollComponentFormViewModel>(sp =>
            new PayrollComponentFormViewModel(
                sp.GetRequiredService<IPayrollComponentService>(),
                sp.GetRequiredService<ITenantProvider>().GetCurrentCompanyId()));
        services.AddTransient<PayrollRunViewModel>();
        services.AddTransient<MasterLookupManagerViewModel>();
        services.AddTransient<ApprovalDashboardViewModel>();
        services.AddTransient<PendingApprovalsViewModel>();
        services.AddTransient<ComponentSimulationViewModel>();
        services.AddTransient<CloneWizardViewModel>();
        services.AddTransient<PackageManagerViewModel>();
        services.AddTransient<StagedImportViewModel>();

        // ── Views ────────────────────────────────────────────────────────────
        services.AddTransient<MainWindow>();
        services.AddTransient<ESSShellWindow>();
        services.AddTransient<LoginView>();
        services.AddTransient<ESSHomeView>();
        services.AddTransient<PayrollComponentView>();
        services.AddTransient<PayrollComponentEditorWindow>();
        services.AddTransient<PayrollRunView>();
        services.AddTransient<MasterLookupManagerView>();
        services.AddTransient<LogViewerView>();
        services.AddTransient<ApprovalDashboardView>();
        services.AddTransient<PendingApprovalsView>();
        services.AddTransient<ComponentSimulationWindow>();
        services.AddTransient<CloneWizardWindow>();
        services.AddTransient<PackageManagerWindow>();
        services.AddTransient<StagedImportWindow>();
        services.AddTransient<CompanySetupDashboardView>();
        services.AddTransient<ComplianceCenterView>();
        services.AddTransient<DiagnosticsDashboardView>();
        services.AddTransient<PayrollConsoleView>();
        services.AddTransient<SettingsView>();

        return services;
    }
}
