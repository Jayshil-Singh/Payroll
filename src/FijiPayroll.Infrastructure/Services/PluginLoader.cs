using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Interfaces;

namespace FijiPayroll.Infrastructure.Services;

/// <summary>
/// Scans the application folder for dynamically loaded assemblies implementing IModulePlugin,
/// registers their dependencies at startup, and coordinates database updates.
/// </summary>
public sealed class PluginLoader
{
    private readonly List<IModulePlugin> _loadedPlugins = new();
    private readonly ILogger<PluginLoader>? _logger;

    /// <summary>Initializes the plugin loader.</summary>
    public PluginLoader(ILogger<PluginLoader>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>Gets all active, fully registered plugins.</summary>
    public IReadOnlyList<IModulePlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();

    /// <summary>
    /// Discovers dll assemblies in the 'Plugins' subdirectory, instantiates plugin entry points,
    /// and triggers their dependency registration logic.
    /// </summary>
    public void DiscoverAndRegisterPlugins(IServiceCollection services, IConfiguration configuration)
    {
        string pluginFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        if (!Directory.Exists(pluginFolder))
        {
            try
            {
                Directory.CreateDirectory(pluginFolder);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[PluginLoader] Could not create plugin folder at {Path}", pluginFolder);
                return;
            }
        }

        var files = Directory.GetFiles(pluginFolder, "*.dll");
        foreach (var file in files)
        {
            try
            {
                var assembly = Assembly.LoadFrom(file);
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IModulePlugin).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        var plugin = (IModulePlugin)Activator.CreateInstance(type)!;
                        plugin.RegisterServices(services, configuration);
                        _loadedPlugins.Add(plugin);
                        _logger?.LogInformation("[PluginLoader] Loaded and registered plugin: {Name} (v{Version})", plugin.ModuleName, plugin.Version);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[PluginLoader] Failed to load plugin assembly: {File}", Path.GetFileName(file));
            }
        }
    }

    /// <summary>
    /// Triggers database migrations or seeding steps for all successfully loaded plugins.
    /// </summary>
    public async Task InitializePluginsDatabaseAsync(IServiceProvider serviceProvider)
    {
        foreach (var plugin in _loadedPlugins)
        {
            try
            {
                _logger?.LogInformation("[PluginLoader] Initializing database for plugin: {Name}", plugin.ModuleName);
                await plugin.InitializeDatabaseAsync(serviceProvider).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[PluginLoader] Database initialization failed for plugin: {Name}", plugin.ModuleName);
            }
        }
    }
}
