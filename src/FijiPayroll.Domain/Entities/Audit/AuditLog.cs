using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Audit;

/// <summary>
/// Domain entity representing a change-tracking audit log entry.
/// </summary>
public sealed class AuditLog : BaseEntity
{
    private AuditLog() { }

    /// <summary>Gets the owner company ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the user ID / username who made the modification.</summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>Gets the entity type name.</summary>
    public string EntityName { get; private set; } = string.Empty;

    /// <summary>Gets the entity primary key representation.</summary>
    public string EntityId { get; private set; } = string.Empty;

    /// <summary>Gets the modification action (e.g. Insert, Update, Delete).</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Gets the changes formatted as a JSON string showing property differences.</summary>
    public string Changes { get; private set; } = string.Empty;

    /// <summary>Gets the UTC timestamp when the change was saved.</summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>Gets the correlation ID linking database operations together.</summary>
    public Guid CorrelationId { get; private set; }

    /// <summary>Factory method to construct an AuditLog entry.</summary>
    public static AuditLog Create(
        int companyId,
        string userId,
        string entityName,
        string entityId,
        string action,
        string changes,
        DateTime timestamp,
        Guid correlationId)
    {
        return new AuditLog
        {
            CompanyId = companyId,
            UserId = userId ?? "System",
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            Changes = changes ?? string.Empty,
            Timestamp = timestamp,
            CorrelationId = correlationId
        };
    }
}
