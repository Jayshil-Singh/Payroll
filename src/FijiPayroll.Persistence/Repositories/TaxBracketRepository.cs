using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of the ITaxBracketRepository.
/// </summary>
public sealed class TaxBracketRepository : ITaxBracketRepository
{
    private readonly ApplicationDbContext _context;

    public TaxBracketRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TaxBracket>> GetBracketsByVersionAndFrequencyAsync(
        string taxVersion,
        PayrollFrequency frequency,
        CancellationToken cancellationToken = default)
    {
        return await _context.TaxBrackets
            .Where(b => b.TaxVersion == taxVersion 
                     && b.Frequency == frequency 
                     && b.IsActive)
            .OrderBy(b => b.LowerLimit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(TaxBracket bracket, CancellationToken cancellationToken = default)
    {
        await _context.TaxBrackets.AddAsync(bracket, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TaxBrackets.AnyAsync(cancellationToken);
    }
}
