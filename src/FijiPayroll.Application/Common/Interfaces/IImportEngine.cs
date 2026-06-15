using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Detailed validation error entry for import pipelines.
/// </summary>
public sealed record ImportError(int Row, string Column, string ErrorMessage);

/// <summary>
/// Result of an import parsing and validation pass.
/// </summary>
public sealed record ImportValidationResult(bool IsValid, Guid JobId, int RecordsProcessed, int SuccessCount, int FailureCount, IReadOnlyList<ImportError> Errors);

/// <summary>
/// Handles spreadsheet import/export operations, column mappings, validation loops, and atomic transactions.
/// </summary>
public interface IImportEngine
{
    /// <summary>Generates an Excel import template file stream for lookups and employees data entry.</summary>
    Task GenerateTemplateAsync(Stream outputStream, string moduleName, CancellationToken cancellationToken = default);

    /// <summary>Parses an imported file and returns verification and validation rules reports.</summary>
    Task<ImportValidationResult> ValidateImportAsync(Stream inputStream, string moduleName, CancellationToken cancellationToken = default);

    /// <summary>Commits the temporary imported database records to the primary master schema.</summary>
    Task CommitImportAsync(Guid jobId, CancellationToken cancellationToken = default);
}
