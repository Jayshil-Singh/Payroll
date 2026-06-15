namespace FijiPayroll.Application.Common.Exceptions;

/// <summary>
/// Thrown by command handlers when the current user does not hold
/// the required permission to perform the requested operation.
/// Translated to a user-friendly error dialog in the WPF UI.
/// </summary>
public sealed class ForbiddenAccessException : Exception
{
    /// <summary>The permission code that was required but not held.</summary>
    public string RequiredPermission { get; }

    /// <inheritdoc/>
    public ForbiddenAccessException(string requiredPermission)
        : base($"Access denied. Required permission: '{requiredPermission}'.")
    {
        RequiredPermission = requiredPermission;
    }
}
