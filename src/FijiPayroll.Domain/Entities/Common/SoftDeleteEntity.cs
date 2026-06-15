namespace FijiPayroll.Domain.Entities.Common;

/// <summary>
/// Extends <see cref="AuditableEntity"/> with soft-delete capability
/// as specified in Database.md §1 (Soft Delete policy).
/// Records are never physically deleted; <see cref="IsDeleted"/> filters them from queries.
/// </summary>
public abstract class SoftDeleteEntity : AuditableEntity
{
    /// <summary>
    /// <c>true</c> if this record has been logically deleted.
    /// EF Core global query filters exclude records where this is <c>true</c>.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>Username of the user who deleted this record. Null if not deleted.</summary>
    public string? DeletedBy { get; private set; }

    /// <summary>UTC timestamp when this record was deleted. Null if not deleted.</summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Marks this entity as logically deleted.
    /// </summary>
    /// <param name="deletedBy">Username performing the deletion.</param>
    /// <exception cref="InvalidOperationException">Thrown if the entity is already deleted.</exception>
    public void SoftDelete(string deletedBy)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Entity is already deleted.");
        }

        IsDeleted = true;
        DeletedBy = deletedBy;
        DeletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restores a soft-deleted entity to an active state.
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedBy = null;
        DeletedAt = null;
    }
}
