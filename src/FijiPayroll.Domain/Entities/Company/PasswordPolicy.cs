using System.Text.RegularExpressions;
using FijiPayroll.Domain.Exceptions;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain boundary policy to validate password strength and blacklist checks.
/// </summary>
public static class PasswordPolicy
{
    private static readonly string[] CommonPasswordBlacklist =
    [
        "password123", "password", "welcome1", "admin123", "fijipayroll", "fiji123", "fiji2026"
    ];

    /// <summary>
    /// Validates password strength against the pilot complexity criteria:
    /// - Minimum 12 characters.
    /// - At least one lowercase letter.
    /// - At least one uppercase letter.
    /// - At least one digit.
    /// - Must not be in the common password blacklist.
    /// </summary>
    /// <param name="password">Plaintext password candidate.</param>
    /// <exception cref="DomainException">Thrown if validation rules are violated.</exception>
    public static void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new DomainException("Password cannot be empty.");
        }

        if (password.Length < 12)
        {
            throw new DomainException("Password must be at least 12 characters long.");
        }

        if (!Regex.IsMatch(password, "[a-z]"))
        {
            throw new DomainException("Password must contain at least one lowercase letter.");
        }

        if (!Regex.IsMatch(password, "[A-Z]"))
        {
            throw new DomainException("Password must contain at least one uppercase letter.");
        }

        if (!Regex.IsMatch(password, "[0-9]"))
        {
            throw new DomainException("Password must contain at least one digit.");
        }

        string normalized = password.Trim().ToLowerInvariant();
        foreach (var blacklisted in CommonPasswordBlacklist)
        {
            if (normalized.StartsWith(blacklisted))
            {
                throw new DomainException("Password is too common or easily guessable. Please choose a more secure password.");
            }
        }
    }
}
