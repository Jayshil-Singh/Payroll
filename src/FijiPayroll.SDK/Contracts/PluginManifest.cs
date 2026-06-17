using System;
using System.Collections.Generic;

namespace FijiPayroll.SDK.Contracts;

/// <summary>
/// Descriptor representing a pluggable module's metadata, capability parameters, and compatibility requirements.
/// </summary>
public sealed class PluginManifest
{
    /// <summary>Gets or sets the unique plugin identifier.</summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the plugin.</summary>
    public string PluginName { get; set; } = string.Empty;

    /// <summary>Gets or sets the developer or provider entity name.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the plugin assembly version.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of plugin features.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the minimal platform calculation engine version required.</summary>
    public string MinEngineVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets the date the plugin layout becomes active.</summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>Gets or sets the date the plugin layout ceases to be active (nullable).</summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>Gets or sets the capabilities registry keys supported by this plugin (e.g., specific bank codes or report names).</summary>
    public List<string> SupportedCapabilities { get; set; } = new();
}
