using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Persistence.Interceptors;
using System.Collections.Generic;
using System.Linq;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Implementation of ICurrentUserService and ICurrentUserAccessor for WPF application.
/// Provides current user details and permissions to satisfy command orchestration and audit layers.
/// </summary>
public sealed class WpfCurrentUserService : ICurrentUserService, ICurrentUserAccessor
{
    /// <inheritdoc />
    public int UserId => 1;

    /// <inheritdoc />
    public string Username => "admin@fijipayroll.gov.fj";

    /// <inheritdoc />
    public IReadOnlyList<int> CompanyIds => new List<int> { 1 };

    /// <inheritdoc />
    public IReadOnlyList<string> Permissions => new List<string> { "All" };

    /// <inheritdoc />
    public bool HasPermission(string permissionCode)
    {
        // Admin user has all permissions in this WPF shell demo stage
        return true;
    }

    /// <inheritdoc />
    public bool HasCompanyAccess(int companyId)
    {
        return CompanyIds.Contains(companyId);
    }
}
