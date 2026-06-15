using System;

namespace FijiPayroll.Domain.Entities.Common;

/// <summary>
/// Extends <see cref="SoftDeleteEntity"/> with archiving attributes
/// for Master Data entities that require audit-compliant archiving before any potential purge.
/// </summary>
public abstract class ArchivableEntity : SoftDeleteEntity
{
    /// <summary>Gets a value indicating whether this record has been archived.</summary>
    public bool IsArchived { get; private set; }

    /// <summary>Gets the username of the user who archived this record.</summary>
    public string? ArchivedBy { get; private set; }

    /// <summary>Gets the UTC timestamp when this record was archived.</summary>
    public DateTime? ArchivedDate { get; private set; }

    /// <summary>Gets the reason why this record was archived.</summary>
    public string? ArchiveReason { get; private set; }

    /// <summary>Places this entity in the archived state.</summary>
    public void Archive(string archivedBy, string reason)
    {
        IsArchived = true;
        ArchivedBy = archivedBy;
        ArchivedDate = DateTime.UtcNow;
        ArchiveReason = reason;
    }

    /// <summary>Restores an archived entity to an active state.</summary>
    public void Unarchive()
    {
        IsArchived = false;
        ArchivedBy = null;
        ArchivedDate = null;
        ArchiveReason = null;
    }
}
