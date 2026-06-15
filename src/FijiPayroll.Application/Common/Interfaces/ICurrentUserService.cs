namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Provides the identity of the currently authenticated application user.
/// Injected into command handlers to stamp audit fields and enforce company isolation.
/// Implemented in the WPF presentation layer via the active login session.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The database primary key of the authenticated user.
    /// Returns 0 for unauthenticated/system operations (e.g., seed jobs).
    /// </summary>
    int UserId { get; }

    /// <summary>
    /// The username (login name) of the authenticated user.
    /// Never null; returns "System" for background jobs.
    /// </summary>
    string Username { get; }

    /// <summary>
    /// All company IDs that the authenticated user is authorised to access.
    /// Empty for a user with no company access.
    /// </summary>
    IReadOnlyList<int> CompanyIds { get; }

    /// <summary>
    /// All permission codes granted to this user via their role assignments.
    /// </summary>
    IReadOnlyList<string> Permissions { get; }

    /// <summary>
    /// Returns <c>true</c> if the user holds the specified permission code.
    /// </summary>
    /// <param name="permissionCode">Permission code from <c>PermissionConstants</c>.</param>
    bool HasPermission(string permissionCode);

    /// <summary>
    /// Returns <c>true</c> if the user is authorised to access the specified company.
    /// </summary>
    /// <param name="companyId">The company primary key.</param>
    bool HasCompanyAccess(int companyId);
}
