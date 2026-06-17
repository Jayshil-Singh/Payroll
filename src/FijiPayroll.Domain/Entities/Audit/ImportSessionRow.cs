using FijiPayroll.Domain.Entities.Common;

namespace FijiPayroll.Domain.Entities.Audit;

/// <summary>
/// Domain entity representing a staged row within an ImportSession.
/// </summary>
public sealed class ImportSessionRow : BaseEntity
{
    private ImportSessionRow() { }

    public ImportSessionRow(
        int importSessionId,
        int rowNumber,
        string payload,
        string validationStatus,
        string? errors = null,
        string? warnings = null)
    {
        ImportSessionId = importSessionId;
        RowNumber = rowNumber;
        Payload = payload;
        ValidationStatus = validationStatus;
        Errors = errors;
        Warnings = warnings;
    }

    public int ImportSessionId { get; private set; }
    public int RowNumber { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public string ValidationStatus { get; private set; } = string.Empty;
    public string? Errors { get; private set; }
    public string? Warnings { get; private set; }

    // Navigation
    public ImportSession? ImportSession { get; private set; }

    public void UpdateValidation(string status, string? errors, string? warnings)
    {
        ValidationStatus = status;
        Errors = errors;
        Warnings = warnings;
    }
}
