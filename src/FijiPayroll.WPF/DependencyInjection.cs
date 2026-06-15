using FijiPayroll.WPF.ViewModels;
using FijiPayroll.WPF.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FijiPayroll.WPF;

/// <summary>
/// Dependency Injection registration for the WPF presentation layer.
/// Registers all views and view models in the service collection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers WPF view models and views in the DI container.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        // Register ViewModels
        services.AddTransient<PayrollComponentViewModel>();
        services.AddTransient<PayrollRunViewModel>();

        // Register Views
        services.AddTransient<PayrollComponentView>();
        services.AddTransient<PayrollRunView>();

        return services;
    }
}
