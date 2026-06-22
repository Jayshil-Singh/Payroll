using FijiPayroll.Domain.Entities.Common;
using System;
using System.Collections.Generic;

namespace FijiPayroll.Domain.Entities.Audit;

/// <summary>
/// Domain entity representing a staging import session.
/// </summary>
public sealed class ImportSession : AuditableEntity
{
    private ImportSession() { }

    public ImportSession(
        int companyId,
        Guid sessionId,
        string originalFileName,
        long uploadedSize,
        string mimeType,
        DateTime started,
        string importedBy,
        string importSource,
        string importHash,
        string status,
        bool rollbackSupported)
    {
        CompanyId = companyId;
        SessionId = sessionId;
        OriginalFileName = originalFileName;
        UploadedSize = uploadedSize;
        MimeType = mimeType;
        Started = started;
        ImportedBy = importedBy;
        ImportSource = importSource;
        ImportHash = importHash;
        Status = status;
        RollbackSupported = rollbackSupported;
        SuccessCount = 0;
        FailureCount = 0;
    }

    public int CompanyId { get; private set; }
    public Guid SessionId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public long UploadedSize { get; private set; }
    public string MimeType { get; private set; } = string.Empty;
    public DateTime Started { get; private set; }
    public DateTime? Validated { get; private set; }
    public DateTime? Approved { get; private set; }
    public DateTime? Committed { get; private set; }
    public DateTime? Archived { get; private set; }
    public string ImportedBy { get; private set; } = string.Empty;
    public string ImportSource { get; private set; } = string.Empty;
    public string ImportHash { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public bool RollbackSupported { get; private set; }

    // Navigation
    public ICollection<ImportSessionRow> Rows { get; private set; } = new List<ImportSessionRow>();

    public void MarkValidated(int successCount, int failureCount)
    {
        Validated = DateTime.UtcNow;
        SuccessCount = successCount;
        FailureCount = failureCount;
        Status = "Validated";
    }

    public void MarkApproved()
    {
        Approved = DateTime.UtcNow;
        Status = "Approved";
    }

    public void MarkCommitted()
    {
        Committed = DateTime.UtcNow;
        Status = "Committed";
    }

    public void MarkArchived()
    {
        Archived = DateTime.UtcNow;
        Status = "Archived";
    }

    public void MarkFailed(string reason)
    {
        Status = "Failed";
    }
}
