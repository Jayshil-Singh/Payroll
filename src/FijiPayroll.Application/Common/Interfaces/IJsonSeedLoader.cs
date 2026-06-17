using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Service interface responsible for loading static compliance and lookup reference data from JSON files.
/// </summary>
public interface IJsonSeedLoader
{
    /// <summary>
    /// Loads banks and branches reference data for the specified company tenant.
    /// </summary>
    Task SeedBanksAsync(int companyId, CancellationToken cancellationToken = default);
}
