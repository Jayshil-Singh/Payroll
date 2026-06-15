using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of the IMasterLookupRepository.
/// </summary>
public sealed class MasterLookupRepository : IMasterLookupRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes repository dependencies.</summary>
    public MasterLookupRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<MasterLookup?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.MasterLookups
            .FirstOrDefaultAsync(ml => ml.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MasterLookup>> GetByCategoryAsync(int companyId, string category, CancellationToken cancellationToken = default)
    {
        string upperCategory = category.ToUpperInvariant();
        return await _context.MasterLookups
            .Where(ml => ml.CompanyId == companyId && ml.Category == upperCategory)
            .OrderBy(ml => ml.DisplayOrder)
            .ThenBy(ml => ml.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> CodeExistsAsync(int companyId, string category, string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        string upperCategory = category.ToUpperInvariant();
        string upperCode = code.ToUpperInvariant();
        return await _context.MasterLookups
            .AnyAsync(ml => ml.CompanyId == companyId 
                         && ml.Category == upperCategory 
                         && ml.Code == upperCode 
                         && (!excludeId.HasValue || ml.Id != excludeId.Value), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddAsync(MasterLookup lookup, CancellationToken cancellationToken = default)
    {
        await _context.MasterLookups.AddAsync(lookup, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Update(MasterLookup lookup)
    {
        _context.MasterLookups.Update(lookup);
    }
}
