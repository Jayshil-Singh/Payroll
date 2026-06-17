using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Repository interface for configuration-driven tax brackets.
/// </summary>
public interface ITaxBracketRepository
{
    /// <summary>
    /// Returns tax brackets matching version and frequency, ordered by LowerLimit ascending.
    /// </summary>
    Task<IReadOnlyList<TaxBracket>> GetBracketsByVersionAndFrequencyAsync(
        string taxVersion,
        PayrollFrequencyType frequency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new tax bracket.
    /// </summary>
    Task AddAsync(TaxBracket bracket, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any tax bracket exists (used in seeders).
    /// </summary>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
