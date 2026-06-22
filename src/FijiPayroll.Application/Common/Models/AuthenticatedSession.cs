namespace FijiPayroll.Application.Common.Models;

/// <summary>
/// Immutable authenticated user session bound to company scope and permissions.
/// </summary>
public sealed class AuthenticatedSession
{
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public IReadOnlyList<int> CompanyIds { get; init; } = Array.Empty<int>();
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
    public bool MustChangePassword { get; init; }
    public bool IsAuthenticated => UserId > 0 && !string.IsNullOrWhiteSpace(Username);
}
