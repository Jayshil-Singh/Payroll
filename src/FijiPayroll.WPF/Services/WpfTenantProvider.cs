using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// WPF implementation of ITenantProvider. Resolves active tenant context
/// dynamically from the shared in-memory ApplicationStateStore session parameters.
/// </summary>
public sealed class WpfTenantProvider : ITenantProvider
{
    private readonly IApplicationStateStore _stateStore;

    public WpfTenantProvider(IApplicationStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    /// <inheritdoc />
    public int GetCurrentCompanyId()
    {
        int companyId = _stateStore.CurrentCompanyId;
        if (companyId <= 0)
        {
            throw new TenantContextException(
                "Tenant context is not established. A valid company must be selected before data access.");
        }

        return companyId;
    }

    /// <inheritdoc />
    public string GetCurrentTenantSecurityKey()
    {
        int companyId = GetCurrentCompanyId();
        return $"KEY_COMP_{companyId}";
    }
}
