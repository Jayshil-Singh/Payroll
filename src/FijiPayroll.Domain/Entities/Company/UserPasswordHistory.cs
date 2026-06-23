using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing an entry in a user's password history to prevent reusing recent passwords.
/// </summary>
public sealed class UserPasswordHistory : BaseEntity
{
    private UserPasswordHistory() { }

    /// <summary>Gets the parent user account ID.</summary>
    public int UserAccountId { get; private set; }

    /// <summary>Gets the hashed password value.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Gets when the password was replaced.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Factory method to construct a new UserPasswordHistory entry.
    /// </summary>
    public static UserPasswordHistory Create(int userAccountId, string passwordHash)
    {
        if (userAccountId <= 0) throw new ArgumentOutOfRangeException(nameof(userAccountId));
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentNullException(nameof(passwordHash));

        return new UserPasswordHistory
        {
            UserAccountId = userAccountId,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
    }
}
