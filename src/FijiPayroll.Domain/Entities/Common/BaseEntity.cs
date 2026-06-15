using System.Collections.Generic;
using FijiPayroll.Domain.Events;

namespace FijiPayroll.Domain.Entities.Common;

/// <summary>
/// Base entity providing a strongly-typed integer primary key
/// for all domain entities. No external dependencies.
/// </summary>
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Gets the read-only collection of domain events associated with the entity.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Registers a domain event.
    /// </summary>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears the domain events.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Database-generated primary key. Zero indicates a transient (unsaved) entity.
    /// </summary>
    public int Id { get; protected set; }

    /// <summary>
    /// Equality is determined by <see cref="Id"/>.
    /// Two transient entities (Id == 0) are never considered equal.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Id == 0 || other.Id == 0) return false;
        return Id == other.Id;
    }

    /// <inheritdoc/>
    public override int GetHashCode() => Id.GetHashCode();

    /// <inheritdoc/>
    public static bool operator ==(BaseEntity? left, BaseEntity? right)
        => left?.Equals(right) ?? right is null;

    /// <inheritdoc/>
    public static bool operator !=(BaseEntity? left, BaseEntity? right)
        => !(left == right);
}
