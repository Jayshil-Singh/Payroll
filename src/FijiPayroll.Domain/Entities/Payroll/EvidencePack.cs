using System;
using System.Collections.Generic;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Root domain model representing an immutable, audit-grade compliance evidence pack for a finalized payroll run.
/// </summary>
public sealed class EvidencePack
{
    /// <summary>Gets the unique identifier of the evidence pack verification session.</summary>
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    /// <summary>Gets the timestamp when this evidence pack was generated.</summary>
    public DateTime GeneratedUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Gets the user who initiated the evidence pack generation.</summary>
    public string GeneratedBy { get; init; } = string.Empty;

    /// <summary>Gets the cryptographic signature of the evidence pack.</summary>
    public string? EvidencePackSignature { get; init; }

    /// <summary>Gets the timestamp when the signature was generated.</summary>
    public DateTime? SignatureTimestamp { get; init; }

    /// <summary>Gets the system build version hash at generation time.</summary>
    public string? SystemBuildVersionHash { get; init; }

    /// <summary>Gets the application version snapshot.</summary>
    public string? ApplicationVersion { get; init; }

    /// <summary>Gets the Git commit hash of the build.</summary>
    public string? GitCommitHash { get; init; }

    /// <summary>Gets the assembly version snapshot details.</summary>
    public string? AssemblyVersionSnapshot { get; init; }

    /// <summary>Gets the Executive Summary details.</summary>
    public ExecutiveSummary ExecutiveSummary { get; init; } = null!;

    /// <summary>Gets the Ledger Integrity Manifest.</summary>
    public LedgerIntegrityManifest LedgerIntegrity { get; init; } = null!;

    /// <summary>Gets the SSRS Report Snapshot Index details.</summary>
    public SSRSReportSnapshotIndex ReportSnapshotIndex { get; init; } = null!;

    /// <summary>Gets the employee-level evidence details.</summary>
    public IReadOnlyList<EmployeeLevelEvidence> EmployeeEvidence { get; init; } = [];

    /// <summary>Gets the validation outputs representing compliance audit issues.</summary>
    public ComplianceValidationOutput ValidationOutput { get; init; } = null!;

    /// <summary>Gets the Traceability Evidence containing step-level calculation traces.</summary>
    public TraceabilityEvidence Traceability { get; init; } = null!;

    /// <summary>Gets the Reconciliation Summary logs.</summary>
    public ReconciliationSummary Reconciliation { get; init; } = null!;
}

/// <summary>
/// High-level executive metadata summary of the payroll run.
/// </summary>
public sealed record ExecutiveSummary(
    int CompanyId,
    int PayrollRunId,
    string Period,
    decimal TotalGross,
    decimal TotalPAYE,
    decimal TotalFNPF,
    decimal TotalNetPay
);

/// <summary>
/// Manifest asserting the data integrity hash of the raw PayrollLedger records.
/// </summary>
public sealed record LedgerIntegrityManifest(
    string PayrollLedgerHash,
    int RecordCount,
    DateTime VerificationTimestamp,
    string IntegrityStatus // PASS / FAIL
);

/// <summary>
/// Snapshot details of a single rendered report template.
/// </summary>
public sealed record SSRSReportSnapshot(
    string ReportName,
    IReadOnlyDictionary<string, string> ParameterSet,
    DateTime RenderTimestamp,
    string ReportHash
);

/// <summary>
/// Index collection containing all SSRS Report snapshots.
/// </summary>
public sealed record SSRSReportSnapshotIndex(
    IReadOnlyList<SSRSReportSnapshot> Snapshots
);

/// <summary>
/// Individual ledger breakdown details captured for an employee.
/// </summary>
public sealed record EmployeeLevelEvidence(
    int EmployeeId,
    string Tin,
    string FnpfNumber,
    string EmployeeName,
    decimal Gross,
    decimal PAYE,
    decimal FNPFEmployee,
    decimal FNPFEmployer,
    decimal NetPay,
    int LedgerReferenceId
);

/// <summary>
/// Represents a validation issue returned by FRCS/FNPF audits.
/// </summary>
public sealed record EvidenceValidationIssue(
    string Severity, // Info, Warning, Error
    string Message,
    string AffectedEmployee,
    string RuleCode,
    string RecommendedAction
);

/// <summary>
/// Composite validation errors generated against FRCS and FNPF rules.
/// </summary>
public sealed record ComplianceValidationOutput(
    IReadOnlyList<EvidenceValidationIssue> FrcsValidationResults,
    IReadOnlyList<EvidenceValidationIssue> FnpfValidationResults
);

/// <summary>
/// Detailed metadata containing progressive rule step values.
/// </summary>
public sealed record EmployeeTraceEvidence(
    int EmployeeId,
    string EmployeeName,
    string TraceTextSummary,
    IReadOnlyList<string> OrderedStepReferenceIds,
    IReadOnlyDictionary<string, decimal> ComponentValues
);

/// <summary>
/// Collection of all execution traces for audit analysis.
/// </summary>
public sealed record TraceabilityEvidence(
    IReadOnlyList<EmployeeTraceEvidence> EmployeeTraces
);

/// <summary>
/// Ledger totals vs compliance export variance checks.
/// </summary>
public sealed record ReconciliationSummary(
    decimal LedgerGross,
    decimal SubmissionGross,
    decimal GrossVariance,
    decimal LedgerPaye,
    decimal SubmissionPaye,
    decimal PayeVariance,
    decimal LedgerFnpf,
    decimal SubmissionFnpf,
    decimal FnpfVariance,
    string Status // PASS / FAIL
);
