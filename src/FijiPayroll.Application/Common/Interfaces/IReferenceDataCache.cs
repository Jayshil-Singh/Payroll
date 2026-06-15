using FijiPayroll.Domain.Entities.Company;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Interface for a thread-safe, memory-cached lookup reference data service.
/// Reduces database queries for static lookup fields during high load.
/// </summary>
public interface IReferenceDataCache
{
    /// <summary>Retrieves cached lookup items for a specific category within the current tenant context.</summary>
    Task<IReadOnlyList<MasterLookup>> GetLookupsByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a single cached lookup item by its unique category and code.</summary>
    Task<MasterLookup?> GetLookupByCodeAsync(string category, string code, CancellationToken cancellationToken = default);

    /// <summary>Clears cache blocks for a specific category.</summary>
    void InvalidateCategory(string category);

    /// <summary>Clears all cached reference items.</summary>
    void InvalidateAll();
}
