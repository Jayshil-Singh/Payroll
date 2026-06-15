using FijiPayroll.WPF.Infrastructure;
using FijiPayroll.WPF.Services;
using FijiPayroll.WPF.ViewModels;
using FijiPayroll.WPF.Views;
using Microsoft.Extensions.DependencyInjection;
using FijiPayroll.Application.Common.Interfaces;

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
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<EmployeeViewModel>();
        services.AddTransient<PayrollViewModel>();
        services.AddTransient<SetupViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<AdminViewModel>();
        services.AddTransient<LogViewerViewModel>();

        // Existing feature ViewModels
        services.AddTransient<PayrollComponentViewModel>();
        services.AddTransient<PayrollRunViewModel>();
        services.AddTransient<MasterLookupManagerViewModel>();

        // ── Views ────────────────────────────────────────────────────────────
        services.AddTransient<MainWindow>();
        services.AddTransient<PayrollComponentView>();
        services.AddTransient<PayrollRunView>();
        services.AddTransient<MasterLookupManagerView>();
        services.AddTransient<LogViewerView>();

        return services;
    }
}
