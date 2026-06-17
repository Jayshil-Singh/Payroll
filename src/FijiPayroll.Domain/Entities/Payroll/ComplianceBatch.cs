using System;
using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model grouping multiple submission documents into an audit-ready, digitally signed batch.
/// </summary>
public sealed class ComplianceBatch : AuditableEntity
{
    /// <summary>Gets the multi-tenant company identifier.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the target compliance period ID.</summary>
    public int CompliancePeriodId { get; private set; }

    /// <summary>Gets the user-defined batch name.</summary>
    public string BatchName { get; private set; } = string.Empty;

    /// <summary>Gets the active status of this compliance batch.</summary>
    public ComplianceBatchStatus Status { get; private set; }

    /// <summary>Gets the base64-encoded cryptographic digital signature for verification.</summary>
    public string? DigitalSignature { get; private set; }

    /// <summary>Gets the certificate thumbprint used for the digital signature.</summary>
    public string? CertificateThumbprint { get; private set; }

    /// <summary>Gets the SHA256 file hash of the batch payload.</summary>
    public string? FileHash { get; private set; }

    private ComplianceBatch() { } // For EF Core

    /// <summary>
    /// Factory method to construct a new ComplianceBatch.
    /// </summary>
    public static ComplianceBatch Create(int companyId, int compliancePeriodId, string batchName)
    {
        if (string.IsNullOrWhiteSpace(batchName)) throw new ArgumentException("Batch name cannot be null or empty.", nameof(batchName));

        return new ComplianceBatch
        {
            CompanyId = companyId,
            CompliancePeriodId = compliancePeriodId,
            BatchName = batchName,
            Status = ComplianceBatchStatus.Active
        };
    }

    /// <summary>
    /// Digtially signs this batch with certificate validation parameters.
    /// </summary>
    public void Sign(string signature, string thumbprint, string fileHash)
    {
        if (string.IsNullOrWhiteSpace(signature)) throw new ArgumentException("Signature cannot be empty.", nameof(signature));
        if (string.IsNullOrWhiteSpace(thumbprint)) throw new ArgumentException("Certificate thumbprint cannot be empty.", nameof(thumbprint));
        if (string.IsNullOrWhiteSpace(fileHash)) throw new ArgumentException("File hash cannot be empty.", nameof(fileHash));

        if (Status != ComplianceBatchStatus.Active)
        {
            throw new InvalidOperationException("Only active batches can be signed.");
        }

        DigitalSignature = signature;
        CertificateThumbprint = thumbprint;
        FileHash = fileHash;
        Status = ComplianceBatchStatus.Submitted;
    }

    /// <summary>
    /// Archives the batch.
    /// </summary>
    public void Archive()
    {
        if (Status != ComplianceBatchStatus.Submitted)
        {
            throw new InvalidOperationException("Only submitted batches can be archived.");
        }
        Status = ComplianceBatchStatus.Archived;
    }
}
