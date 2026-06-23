using FijiPayroll.Domain.Entities.Company;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Repository interface for SystemSettings entity operations.
/// </summary>
public interface ISystemSettingsRepository
{
    Task<SystemSettings?> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task AddAsync(SystemSettings settings, CancellationToken cancellationToken = default);
    void Update(SystemSettings settings);
}
