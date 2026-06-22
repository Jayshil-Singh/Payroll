using System;

namespace FijiPayroll.Application.Common.Exceptions;

/// <summary>
/// Thrown when a request is forbidden due to missing permissions.
/// </summary>
public sealed class ForbiddenException : Exception
{
    /// <summary>Gets the permission code that was required but not held.</summary>
    public string Permission { get; }

    /// <inheritdoc/>
    public ForbiddenException(string permission)
        : base($"Access denied. Required permission: '{permission}'.")
    {
        Permission = permission;
    }
}
