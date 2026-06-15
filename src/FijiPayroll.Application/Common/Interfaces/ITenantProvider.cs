namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Service supplying context for the currently authenticated tenant/company.
/// Used to enforce database query filters and data isolation constraints.
/// </summary>
public interface ITenantProvider
{
    /// <summary>Gets the ID of the current active company tenant.</summary>
    int GetCurrentCompanyId();

    /// <summary>Gets the security isolation key for the current active tenant.</summary>
    string GetCurrentTenantSecurityKey();
}
