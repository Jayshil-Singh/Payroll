using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FijiPayroll.Persistence.Interceptors;

/// <summary>
/// EF Core SaveChanges interceptor that automatically stamps audit fields
/// (<c>CreatedAt</c>, <c>CreatedBy</c>, <c>ModifiedAt</c>, <c>ModifiedBy</c>)
/// on all <see cref="AuditableEntity"/> instances before they are written to the database.
///
/// This interceptor is registered in <see cref="FijiPayroll.Persistence.DependencyInjection"/>
/// and applied globally to the <c>ApplicationDbContext</c>.
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserAccessor _currentUserAccessor;

    /// <summary>Initialises the interceptor with user context access.</summary>
    public AuditableEntityInterceptor(ICurrentUserAccessor currentUserAccessor)
    {
        _currentUserAccessor = currentUserAccessor;
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────────

    private void UpdateAuditFields(DbContext? context)
    {
        if (context is null) return;

        var utcNow  = DateTime.UtcNow;
        var username = _currentUserAccessor.Username;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = username;
                    entry.Entity.CreatedAt = utcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedBy = username;
                    entry.Entity.ModifiedAt = utcNow;
                    break;
            }
        }
    }
}

/// <summary>
/// Minimal accessor interface allowing the interceptor to obtain the current username
/// without taking a full dependency on <c>ICurrentUserService</c>.
/// Implemented in the Infrastructure layer by the session service.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>Current username. Never null; defaults to "System" for background tasks.</summary>
    string Username { get; }
}
