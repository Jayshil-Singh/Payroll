using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Exceptions;
using FijiPayroll.Shared.Guards;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain aggregate root representing a user account within a company.
/// Handles authentication state, failed login attempts, lockout logic, and password updates.
/// </summary>
public sealed class UserAccount : AuditableEntity
{
    private string _username = string.Empty;
    private string _passwordHash = string.Empty;
    private string _displayName = string.Empty;
    private readonly List<UserRole> _roles = new();

    private UserAccount() { } // For EF Core

    /// <summary>Gets the company ID context (multi-tenant boundary).</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the username.</summary>
    public string Username
    {
        get => _username;
        private set => _username = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>Gets the password hash (BCrypt hashed password).</summary>
    public string PasswordHash
    {
        get => _passwordHash;
        private set => _passwordHash = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>Gets the user's display name.</summary>
    public string DisplayName
    {
        get => _displayName;
        private set => _displayName = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>Gets whether the user account is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets whether the user is a system admin.</summary>
    public bool IsSystemAdmin { get; private set; }

    /// <summary>Gets the timestamp of the last successful login.</summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>Gets the consecutive failed login attempts counter.</summary>
    public int FailedLoginCount { get; private set; }

    /// <summary>Gets the lockout expiration timestamp (UTC).</summary>
    public DateTime? LockedUntil { get; private set; }

    /// <summary>Gets whether the user is required to change their password on the next login.</summary>
    public bool MustChangePassword { get; private set; }

    /// <summary>Gets the read-only collection of roles mapped to the user.</summary>
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    /// <summary>
    /// Factory method to construct a new UserAccount.
    /// </summary>
    public static UserAccount Create(
        int companyId,
        string username,
        string passwordHash,
        string displayName,
        bool isSystemAdmin = false,
        bool mustChangePassword = false)
    {
        if (companyId <= 0)
        {
            throw new DomainException("CompanyId must be a positive integer.");
        }

        return new UserAccount
        {
            CompanyId = companyId,
            Username = username,
            PasswordHash = passwordHash,
            DisplayName = displayName,
            IsActive = true,
            IsSystemAdmin = isSystemAdmin,
            FailedLoginCount = 0,
            MustChangePassword = mustChangePassword
        };
    }

    /// <summary>
    /// Records a successful authentication session and resets locks.
    /// </summary>
    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginCount = 0;
        LockedUntil = null;
    }

    /// <summary>
    /// Records a failed authentication attempt. Lockout triggers after 5 failures for 15 minutes.
    /// </summary>
    public void RecordFailedLogin()
    {
        FailedLoginCount++;
        if (FailedLoginCount >= 5)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(15);
        }
    }

    /// <summary>
    /// Checks if the account is currently locked out.
    /// </summary>
    public bool IsLockedOut()
    {
        return LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Assigns a new password hash and updates force change state.
    /// </summary>
    public void UpdatePassword(string newHash)
    {
        PasswordHash = newHash;
        MustChangePassword = false;
    }

    /// <summary>
    /// Flags the user as requiring a password change on their next login.
    /// </summary>
    public void ForcePasswordChange()
    {
        MustChangePassword = true;
    }

    /// <summary>
    /// Deactivates the user account, blocking all access.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Reactivates the user account.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Maps a role to the user.
    /// </summary>
    public void AddRole(UserRole role)
    {
        if (role == null) throw new ArgumentNullException(nameof(role));
        if (!_roles.Contains(role))
        {
            _roles.Add(role);
        }
    }

    /// <summary>
    /// Removes a role from the user.
    /// </summary>
    public void RemoveRole(UserRole role)
    {
        if (role == null) throw new ArgumentNullException(nameof(role));
        _roles.Remove(role);
    }
}
