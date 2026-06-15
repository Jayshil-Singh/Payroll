namespace FijiPayroll.Application.Common.Exceptions;

/// <summary>
/// Thrown by query handlers when a requested entity does not exist
/// or has been soft-deleted. Translated to a user-friendly message in the UI.
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>The entity type that was not found.</summary>
    public string EntityName { get; }

    /// <summary>The key used to look up the entity.</summary>
    public object Key { get; }

    /// <inheritdoc/>
    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with key '{key}' was not found.")
    {
        EntityName = entityName;
        Key        = key;
    }
}
