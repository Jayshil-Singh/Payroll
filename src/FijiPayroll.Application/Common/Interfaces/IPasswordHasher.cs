namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Service abstraction to hash and verify passwords.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Generates a secure hash of the plaintext password.
    /// </summary>
    string Hash(string plaintext);

    /// <summary>
    /// Verifies if a plaintext password matches a secure hash.
    /// </summary>
    bool Verify(string plaintext, string hash);
}
