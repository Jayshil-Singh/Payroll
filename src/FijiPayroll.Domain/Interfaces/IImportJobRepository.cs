using FijiPayroll.Domain.Entities.Audit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Contract interface for handling persistence operations on the <see cref="ImportJob"/> entity.
/// </summary>
public interface IImportJobRepository
{
    /// <summary>Adds a new ImportJob record asynchronously.</summary>
    Task AddAsync(ImportJob job, CancellationToken cancellationToken);

    /// <summary>Retrieves an ImportJob by its unique JobId Guid.</summary>
    Task<ImportJob?> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken);
}
