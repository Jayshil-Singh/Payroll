using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that captures entity modifications (added, modified, deleted) and domain events,
/// serialises the audit log and publishes the domain events to the outbox table in the same transaction.
/// </summary>
public sealed class AuditLogInterceptor : SaveChangesInterceptor
{
    private readonly ICorrelationContext _correlationContext;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ITenantProvider _tenantProvider;
    private readonly IEventBus _eventBus;
    private bool _isAuditing;

    private readonly List<AuditEntry> _pendingAudits = new();
    private readonly List<IDomainEvent> _pendingEvents = new();

    /// <summary>Initialises the interceptor.</summary>
    public AuditLogInterceptor(
        ICorrelationContext correlationContext,
        ICurrentUserAccessor currentUserAccessor,
        ITenantProvider tenantProvider,
        IEventBus eventBus)
    {
        _correlationContext = correlationContext;
        _currentUserAccessor = currentUserAccessor;
        _tenantProvider = tenantProvider;
        _eventBus = eventBus;
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (_isAuditing) return base.SavingChanges(eventData, result);

        var (audits, events) = CaptureChanges(eventData.Context);
        _pendingAudits.AddRange(audits);
        _pendingEvents.AddRange(events);

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (_isAuditing) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var (audits, events) = CaptureChanges(eventData.Context);
        _pendingAudits.AddRange(audits);
        _pendingEvents.AddRange(events);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        if (_isAuditing || (_pendingAudits.Count == 0 && _pendingEvents.Count == 0))
        {
            return result;
        }

        try
        {
            _isAuditing = true;
            SaveAuditLogsAndEvents(eventData.Context);
        }
        finally
        {
            _isAuditing = false;
            _pendingAudits.Clear();
            _pendingEvents.Clear();
        }

        return result;
    }

    /// <inheritdoc/>
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_isAuditing || (_pendingAudits.Count == 0 && _pendingEvents.Count == 0))
        {
            return result;
        }

        try
        {
            _isAuditing = true;
            await SaveAuditLogsAndEventsAsync(eventData.Context, cancellationToken);
        }
        finally
        {
            _isAuditing = false;
            _pendingAudits.Clear();
            _pendingEvents.Clear();
        }

        return result;
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────────

    private (List<AuditEntry> Audits, List<IDomainEvent> Events) CaptureChanges(DbContext? context)
    {
        var audits = new List<AuditEntry>();
        var events = new List<IDomainEvent>();
        if (context is null) return (audits, events);

        // 1. Collect domain events from entities inheriting from BaseEntity
        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.Entity.DomainEvents.Any())
            {
                events.AddRange(entry.Entity.DomainEvents);
                entry.Entity.ClearDomainEvents();
            }
        }

        // 2. Capture audit modifications
        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.Entity is AuditLog || entry.Entity is EntityEvent)
            {
                continue;
            }

            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                string action;
                var changes = SerializeChanges(entry, out action);
                if (changes != "{}" || action == "Delete")
                {
                    audits.Add(new AuditEntry(entry.Entity, entry.State, action, changes));
                }
            }
        }

        return (audits, events);
    }

    private string SerializeChanges(EntityEntry entry, out string action)
    {
        action = entry.State switch
        {
            EntityState.Added => "Create",
            EntityState.Deleted => "Delete",
            EntityState.Modified => "Update",
            _ => "None"
        };

        // Detect soft deletion
        if (entry.State == EntityState.Modified && entry.Entity is SoftDeleteEntity)
        {
            var isDeletedProp = entry.Property("IsDeleted");
            if (isDeletedProp.IsModified && (bool)isDeletedProp.CurrentValue!)
            {
                action = "Delete";
            }
        }

        var changesDict = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            var propertyName = property.Metadata.Name;

            // Skip auditing audit metadata fields
            if (propertyName is "CreatedAt" or "CreatedBy" or "ModifiedAt" or "ModifiedBy" or "DeletedAt" or "DeletedBy" or "RowVersion")
            {
                continue;
            }

            if (action == "Create")
            {
                changesDict[propertyName] = new { New = property.CurrentValue };
            }
            else if (action == "Delete")
            {
                changesDict[propertyName] = new { Original = property.OriginalValue };
            }
            else if (action == "Update")
            {
                if (property.IsModified)
                {
                    if (!Equals(property.OriginalValue, property.CurrentValue))
                    {
                        changesDict[propertyName] = new
                        {
                            Original = property.OriginalValue,
                            New = property.CurrentValue
                        };
                    }
                }
            }
        }

        return System.Text.Json.JsonSerializer.Serialize(changesDict);
    }

    private int GetCompanyId(BaseEntity entity)
    {
        var companyIdProp = entity.GetType().GetProperty("CompanyId");
        if (companyIdProp is not null)
        {
            var val = companyIdProp.GetValue(entity);
            if (val is int id)
            {
                return id;
            }
        }

        return _tenantProvider.GetCurrentCompanyId();
    }

    private void SaveAuditLogsAndEvents(DbContext? context)
    {
        if (context is null) return;

        var correlationId = _correlationContext.CorrelationId;
        var userId = _currentUserAccessor.Username;
        var companyId = _tenantProvider.GetCurrentCompanyId();
        var timestamp = DateTime.UtcNow;

        var auditLogs = new List<AuditLog>();
        foreach (var audit in _pendingAudits)
        {
            var entityCompanyId = GetCompanyId(audit.Entity);
            var auditLog = AuditLog.Create(
                companyId: entityCompanyId,
                userId: userId,
                entityName: audit.Entity.GetType().Name,
                entityId: audit.Entity.Id.ToString(),
                action: audit.Action,
                changes: audit.Changes,
                timestamp: timestamp,
                correlationId: correlationId);
            auditLogs.Add(auditLog);
        }

        var entityEvents = new List<EntityEvent>();
        foreach (var @event in _pendingEvents)
        {
            var eventCompanyId = companyId;
            var companyIdProp = @event.GetType().GetProperty("CompanyId");
            if (companyIdProp is not null && companyIdProp.GetValue(@event) is int cid)
            {
                eventCompanyId = cid;
            }

            var payload = System.Text.Json.JsonSerializer.Serialize(@event, @event.GetType());
            var entityEvent = EntityEvent.Create(
                companyId: eventCompanyId,
                eventType: @event.GetType().Name,
                payload: payload,
                occurredOn: @event.OccurredOn,
                correlationId: correlationId);
            entityEvents.Add(entityEvent);
        }

        if (auditLogs.Any()) context.Set<AuditLog>().AddRange(auditLogs);
        if (entityEvents.Any()) context.Set<EntityEvent>().AddRange(entityEvents);

        if (auditLogs.Any() || entityEvents.Any())
        {
            context.SaveChanges();
        }

        // Fire-and-forget publish
        foreach (var @event in _pendingEvents)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _eventBus.PublishAsync(@event);
                }
                catch
                {
                    // Fail-safe
                }
            });
        }
    }

    private async Task SaveAuditLogsAndEventsAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null) return;

        var correlationId = _correlationContext.CorrelationId;
        var userId = _currentUserAccessor.Username;
        var companyId = _tenantProvider.GetCurrentCompanyId();
        var timestamp = DateTime.UtcNow;

        var auditLogs = new List<AuditLog>();
        foreach (var audit in _pendingAudits)
        {
            var entityCompanyId = GetCompanyId(audit.Entity);
            var auditLog = AuditLog.Create(
                companyId: entityCompanyId,
                userId: userId,
                entityName: audit.Entity.GetType().Name,
                entityId: audit.Entity.Id.ToString(),
                action: audit.Action,
                changes: audit.Changes,
                timestamp: timestamp,
                correlationId: correlationId);
            auditLogs.Add(auditLog);
        }

        var entityEvents = new List<EntityEvent>();
        foreach (var @event in _pendingEvents)
        {
            var eventCompanyId = companyId;
            var companyIdProp = @event.GetType().GetProperty("CompanyId");
            if (companyIdProp is not null && companyIdProp.GetValue(@event) is int cid)
            {
                eventCompanyId = cid;
            }

            var payload = System.Text.Json.JsonSerializer.Serialize(@event, @event.GetType());
            var entityEvent = EntityEvent.Create(
                companyId: eventCompanyId,
                eventType: @event.GetType().Name,
                payload: payload,
                occurredOn: @event.OccurredOn,
                correlationId: correlationId);
            entityEvents.Add(entityEvent);
        }

        if (auditLogs.Any()) context.Set<AuditLog>().AddRange(auditLogs);
        if (entityEvents.Any()) context.Set<EntityEvent>().AddRange(entityEvents);

        if (auditLogs.Any() || entityEvents.Any())
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        foreach (var @event in _pendingEvents)
        {
            await _eventBus.PublishAsync(@event, cancellationToken);
        }
    }

    private class AuditEntry
    {
        public BaseEntity Entity { get; }
        public EntityState State { get; }
        public string Action { get; }
        public string Changes { get; }

        public AuditEntry(BaseEntity entity, EntityState state, string action, string changes)
        {
            Entity = entity;
            State = state;
            Action = action;
            Changes = changes;
        }
    }
}
