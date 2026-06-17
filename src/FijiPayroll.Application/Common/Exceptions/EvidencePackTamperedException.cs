using System;

namespace FijiPayroll.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when cryptographic verification, hash validation, or tenant isolation check fails.
/// </summary>
public sealed class EvidencePackTamperedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EvidencePackTamperedException"/> class.
    /// </summary>
    /// <param name="message">The message explaining the tamper detection details.</param>
    public EvidencePackTamperedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvidencePackTamperedException"/> class.
    /// </summary>
    /// <param name="message">The message explaining the tamper detection details.</param>
    /// <param name="innerException">The inner exception.</param>
    public EvidencePackTamperedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
