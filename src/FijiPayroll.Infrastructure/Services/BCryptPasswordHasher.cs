using FijiPayroll.Application.Common.Interfaces;

namespace FijiPayroll.Infrastructure.Services;

/// <summary>
/// Implementation of IPasswordHasher using BCrypt.Net.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <inheritdoc />
    public string Hash(string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            throw new ArgumentException("Password cannot be empty or whitespace.", nameof(plaintext));
        }

        return BCrypt.Net.BCrypt.HashPassword(plaintext, WorkFactor);
    }

    /// <inheritdoc />
    public bool Verify(string plaintext, string hash)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(plaintext, hash);
        }
        catch
        {
            return false;
        }
    }
}
