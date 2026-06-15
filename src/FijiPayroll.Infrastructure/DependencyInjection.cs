using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FijiPayroll.Infrastructure;

/// <summary>
/// Extension methods to register all Infrastructure layer services (EventBus, FileStorage, PluginLoader)
/// with the dependency injection container. Called from the WPF composition root.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Registers infrastructure services and the plugin loader mechanism.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the EventBus
        services.AddScoped<IEventBus, EventBus>();

        // Register the CorrelationContext
        services.AddScoped<ICorrelationContext, CorrelationContext>();

        // Register the File Storage Provider
        services.AddScoped<IFileStorageProvider, FileStorageProvider>();

        // Register the Import Engine
        services.AddScoped<IImportEngine, ImportEngine>();

        // Register the Search Service
        services.AddSingleton<ISearchService, SearchService>();

        // Register the Plugin Loader as a singleton
        var pluginLoader = new PluginLoader();
        pluginLoader.DiscoverAndRegisterPlugins(services, configuration);
        services.AddSingleton(pluginLoader);

        return services;
    }
}
