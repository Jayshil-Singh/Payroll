using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Domain.Entities.Company;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Domain repository interface for UserAccount management.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Fetches a user by their username and target company ID.
    /// </summary>
    Task<UserAccount?> GetByUsernameAsync(string username, int companyId, CancellationToken ct);

    /// <summary>
    /// Fetches a user by their primary key identifier.
    /// </summary>
    Task<UserAccount?> GetByIdAsync(int userId, CancellationToken ct);

    /// <summary>
    /// Adds a user account to the context.
    /// </summary>
    Task AddAsync(UserAccount user, CancellationToken ct);

    /// <summary>
    /// Resolves all distinct permissions mapped to the user via their active roles.
    /// </summary>
    Task<IReadOnlyList<string>> GetPermissionsForUserAsync(int userId, CancellationToken ct);

    /// <summary>
    /// Resolves all companies associated with an active username.
    /// </summary>
    Task<IReadOnlyList<Company>> GetCompaniesByUsernameAsync(string username, CancellationToken ct);

    /// <summary>
    /// Returns all user accounts for the specified company.
    /// </summary>
    Task<IReadOnlyList<UserAccount>> GetAllByCompanyAsync(int companyId, CancellationToken ct);

    /// <summary>
    /// Updates an existing UserAccount entity in the persistence store.
    /// </summary>
    void Update(UserAccount user);
}
