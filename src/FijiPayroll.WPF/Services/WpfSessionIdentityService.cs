using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Persistence.Interceptors;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Session-bound identity service. No permission bypass is permitted.
/// </summary>
public sealed class WpfSessionIdentityService : ICurrentUserService, ICurrentUserAccessor
{
    private readonly IAuthSessionStore _sessionStore;
    private readonly ITenantProvider _tenantProvider;

    public WpfSessionIdentityService(IAuthSessionStore sessionStore, ITenantProvider tenantProvider)
    {
        _sessionStore = sessionStore;
        _tenantProvider = tenantProvider;
    }

    public int UserId => RequireSession().UserId;

    public string Username => RequireSession().Username;

    public IReadOnlyList<int> CompanyIds => RequireSession().CompanyIds;

    public IReadOnlyList<string> Permissions => RequireSession().Permissions;

    public bool HasPermission(string permissionCode)
    {
        var session = _sessionStore.Current;
        if (!session.IsAuthenticated)
        {
            return false;
        }

        return session.Permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }

    public bool HasCompanyAccess(int companyId)
    {
        var session = _sessionStore.Current;
        if (!session.IsAuthenticated)
        {
            return false;
        }

        if (companyId <= 0)
        {
            return false;
        }

        return session.CompanyIds.Contains(companyId);
    }

    private AuthenticatedSession RequireSession()
    {
        var session = _sessionStore.Current;
        if (!session.IsAuthenticated)
        {
            throw new ForbiddenAccessException("AuthenticatedSession");
        }

        _ = _tenantProvider.GetCurrentCompanyId();
        return session;
    }
}
