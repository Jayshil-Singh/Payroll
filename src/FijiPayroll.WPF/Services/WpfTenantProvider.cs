using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.WPF.Services;
using System;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// WPF implementation of ITenantProvider. Resolves active tenant context
/// dynamically from the shared in-memory ApplicationStateStore session parameters.
/// </summary>
public sealed class WpfTenantProvider : ITenantProvider
{
    private readonly IApplicationStateStore _stateStore;

    /// <summary>Initializes the tenant provider with the session state store.</summary>
    public WpfTenantProvider(IApplicationStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    /// <inheritdoc />
    public int GetCurrentCompanyId()
    {
        return _stateStore.CurrentCompanyId;
    }

    /// <inheritdoc />
    public string GetCurrentTenantSecurityKey()
    {
        // Yields a deterministic isolation token mapped to the active company ID
        return $"KEY_COMP_{_stateStore.CurrentCompanyId}";
    }
}
