using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Defines the startup contract for modular system plugins.
/// Plugins register dependencies and execute db schema updates during initialization.
/// </summary>
public interface IModulePlugin
{
    /// <summary>Unique name of the plugin.</summary>
    string ModuleName { get; }

    /// <summary>Semantic version number.</summary>
    string Version { get; }

    /// <summary>Registers plugin-specific dependencies in the main DI container.</summary>
    void RegisterServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>Executes initial data migrations or database seedings using the resolved scope.</summary>
    Task InitializeDatabaseAsync(IServiceProvider serviceProvider);
}
