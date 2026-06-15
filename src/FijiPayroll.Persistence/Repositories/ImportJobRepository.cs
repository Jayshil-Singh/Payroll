using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Repositories;

/// <summary>
/// EF Core repository implementation for the <see cref="ImportJob"/> entity.
/// </summary>
public sealed class ImportJobRepository : IImportJobRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes the repository with the DbContext.</summary>
    public ImportJobRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(ImportJob job, CancellationToken cancellationToken)
    {
        await _context.Set<ImportJob>().AddAsync(job, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ImportJob?> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return await _context.Set<ImportJob>()
            .FirstOrDefaultAsync(x => x.JobId == jobId, cancellationToken);
    }
}
