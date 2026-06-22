using System;
using System.Collections.Generic;
using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Shared.Guards;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain aggregate root/child entity representing a user's role mapping.
/// Links to permissions governing features accessibility.
/// </summary>
public sealed class UserRole : BaseEntity
{
    private string _roleName = string.Empty;
    private readonly List<UserPermission> _permissions = new();

    private UserRole() { } // For EF Core

    /// <summary>Gets the parent user account ID.</summary>
    public int UserAccountId { get; private set; }

    /// <summary>Gets the display name of the role.</summary>
    public string RoleName
    {
        get => _roleName;
        private set => _roleName = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>Gets the collection of user permissions mapped to this role.</summary>
    public IReadOnlyCollection<UserPermission> Permissions => _permissions.AsReadOnly();

    /// <summary>
    /// Factory method to construct a new UserRole.
    /// </summary>
    public static UserRole Create(int userAccountId, string roleName)
    {
        if (userAccountId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userAccountId));
        }

        return new UserRole
        {
            UserAccountId = userAccountId,
            RoleName = roleName
        };
    }

    /// <summary>
    /// Adds a permission to the role.
    /// </summary>
    public void AddPermission(UserPermission permission)
    {
        if (permission == null) throw new ArgumentNullException(nameof(permission));
        if (!_permissions.Contains(permission))
        {
            _permissions.Add(permission);
        }
    }

    /// <summary>
    /// Removes a permission from the role.
    /// </summary>
    public void RemovePermission(UserPermission permission)
    {
        if (permission == null) throw new ArgumentNullException(nameof(permission));
        _permissions.Remove(permission);
    }
}
