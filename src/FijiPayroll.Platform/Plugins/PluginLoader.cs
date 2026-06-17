using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FijiPayroll.SDK.Contracts;
using FijiPayroll.SDK.Interfaces;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Platform.Plugins;

/// <summary>
/// Dynamically loads plugin assemblies and instantiates implementation types.
/// </summary>
public sealed class PluginLoader
{
    private readonly ILogger<PluginLoader> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLoader"/> class.
    /// </summary>
    public PluginLoader(ILogger<PluginLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Scan and load instances of type <typeparamref name="T"/> from assembly files in the specified directory.
    /// </summary>
    /// <typeparam name="T">The interface or contract type to discover.</typeparam>
    /// <param name="directoryPath">The absolute path to the plugins directory.</param>
    /// <returns>A sequence of instantiated plugin components.</returns>
    public IEnumerable<T> LoadPlugins<T>(string directoryPath) where T : class
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Directory path cannot be empty.", nameof(directoryPath));
        }

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning("Plugin directory does not exist: {DirectoryPath}", directoryPath);
            yield break;
        }

        var loadedInstances = new List<T>();
        var files = Directory.GetFiles(directoryPath, "*.dll", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin assembly: {FilePath}", file);
                continue;
            }

            foreach (var type in assembly.GetTypes())
            {
                if (typeof(T).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    T? instance = null;
                    try
                    {
                        instance = Activator.CreateInstance(type) as T;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to instantiate plugin type '{TypeName}' from '{FilePath}'", type.FullName, file);
                    }

                    if (instance != null)
                    {
                        loadedInstances.Add(instance);
                        _logger.LogInformation("Successfully loaded plugin type '{TypeName}'", type.FullName);
                    }
                }
            }
        }

        foreach (var instance in loadedInstances)
        {
            yield return instance;
        }
    }
}
