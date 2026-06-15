using FijiPayroll.Domain.Entities.Company;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Repository interface for <see cref="MasterLookup"/> entity operations.
/// </summary>
public interface IMasterLookupRepository
{
    /// <summary>Retrieves a lookup by its primary key.</summary>
    Task<MasterLookup?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all lookups for a specific category within a company.</summary>
    Task<IReadOnlyList<MasterLookup>> GetByCategoryAsync(int companyId, string category, CancellationToken cancellationToken = default);

    /// <summary>Checks if a lookup code already exists in a category for a company.</summary>
    Task<bool> CodeExistsAsync(int companyId, string category, string code, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>Adds a new lookup item.</summary>
    Task AddAsync(MasterLookup lookup, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing lookup item.</summary>
    void Update(MasterLookup lookup);
}
