namespace FijiPayroll.Domain.Entities.Common;

/// <summary>
/// Extends <see cref="BaseEntity"/> with full audit trail fields as defined
/// in Architecture.md §11. All mutable entities must inherit from this class.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    /// <summary>Username of the user who created this record (UTC).</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>UTC timestamp when this record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Username of the user who last modified this record. Null if never modified.</summary>
    public string? ModifiedBy { get; set; }

    /// <summary>UTC timestamp of the last modification. Null if never modified.</summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// EF Core-managed concurrency token (maps to SQL ROWVERSION).
    /// Prevents lost-update anomalies in concurrent multi-user scenarios.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}
