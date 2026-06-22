using System;
using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Shared.Guards;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing a permission assigned to a role.
/// </summary>
public sealed class UserPermission : BaseEntity
{
    private string _permissionCode = string.Empty;

    private UserPermission() { } // For EF Core

    /// <summary>Gets the parent role ID.</summary>
    public int UserRoleId { get; private set; }

    /// <summary>Gets the permission code matching PermissionConstants.</summary>
    public string PermissionCode
    {
        get => _permissionCode;
        private set => _permissionCode = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Factory method to construct a new UserPermission.
    /// </summary>
    public static UserPermission Create(int userRoleId, string permissionCode)
    {
        if (userRoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userRoleId));
        }

        return new UserPermission
        {
            UserRoleId = userRoleId,
            PermissionCode = permissionCode
        };
    }
}
