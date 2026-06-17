using System;
using System.Collections.Generic;
using FijiPayroll.Domain.Entities.Payroll;

namespace FijiPayroll.Application.Services.EvidencePack;

/// <summary>
/// A fluent builder pattern to assemble and validate <see cref="EvidencePack"/> instances.
/// </summary>
public sealed class EvidencePackBuilder
{
    private Guid _correlationId = Guid.NewGuid();
    private DateTime _generatedUtc = DateTime.UtcNow;
    private string _generatedBy = "System";
    private string? _evidencePackSignature;
    private DateTime? _signatureTimestamp;
    private string? _systemBuildVersionHash;
    private string? _applicationVersion;
    private string? _gitCommitHash;
    private string? _assemblyVersionSnapshot;
    private ExecutiveSummary? _executiveSummary;
    private LedgerIntegrityManifest? _ledgerIntegrity;
    private SSRSReportSnapshotIndex? _reportSnapshotIndex;
    private List<EmployeeLevelEvidence> _employeeEvidence = [];
    private ComplianceValidationOutput? _validationOutput;
    private TraceabilityEvidence? _traceability;
    private ReconciliationSummary? _reconciliation;

    /// <summary>Sets the correlation ID.</summary>
    public EvidencePackBuilder WithCorrelationId(Guid id)
    {
        _correlationId = id;
        return this;
    }

    /// <summary>Sets the generation timestamp.</summary>
    public EvidencePackBuilder WithGeneratedUtc(DateTime timestamp)
    {
        _generatedUtc = timestamp;
        return this;
    }

    /// <summary>Sets the user who triggered the package generation.</summary>
    public EvidencePackBuilder WithGeneratedBy(string user)
    {
        _generatedBy = user ?? string.Empty;
        return this;
    }

    /// <summary>Sets the cryptographic signature metadata.</summary>
    public EvidencePackBuilder WithSignature(string? signature, DateTime? timestamp)
    {
        _evidencePackSignature = signature;
        _signatureTimestamp = timestamp;
        return this;
    }

    /// <summary>Sets the build version metadata.</summary>
    public EvidencePackBuilder WithBuildVersion(
        string? systemBuildVersionHash,
        string? applicationVersion,
        string? gitCommitHash,
        string? assemblyVersionSnapshot)
    {
        _systemBuildVersionHash = systemBuildVersionHash;
        _applicationVersion = applicationVersion;
        _gitCommitHash = gitCommitHash;
        _assemblyVersionSnapshot = assemblyVersionSnapshot;
        return this;
    }

    /// <summary>Sets the Executive Summary.</summary>
    public EvidencePackBuilder WithExecutiveSummary(ExecutiveSummary summary)
    {
        _executiveSummary = summary;
        return this;
    }

    /// <summary>Sets the Ledger Integrity Manifest.</summary>
    public EvidencePackBuilder WithLedgerIntegrity(LedgerIntegrityManifest manifest)
    {
        _ledgerIntegrity = manifest;
        return this;
    }

    /// <summary>Sets the SSRS Report Snapshot Index.</summary>
    public EvidencePackBuilder WithReportSnapshotIndex(SSRSReportSnapshotIndex index)
    {
        _reportSnapshotIndex = index;
        return this;
    }

    /// <summary>Sets the employee-level evidence collection.</summary>
    public EvidencePackBuilder WithEmployeeEvidence(IEnumerable<EmployeeLevelEvidence> evidence)
    {
        _employeeEvidence = evidence != null ? new List<EmployeeLevelEvidence>(evidence) : [];
        return this;
    }

    /// <summary>Sets the compliance validation output.</summary>
    public EvidencePackBuilder WithValidationOutput(ComplianceValidationOutput output)
    {
        _validationOutput = output;
        return this;
    }

    /// <summary>Sets the traceability evidence.</summary>
    public EvidencePackBuilder WithTraceability(TraceabilityEvidence traces)
    {
        _traceability = traces;
        return this;
    }

    /// <summary>Sets the reconciliation summary.</summary>
    public EvidencePackBuilder WithReconciliation(ReconciliationSummary summary)
    {
        _reconciliation = summary;
        return this;
    }

    /// <summary>
    /// Builds and validates the complete <see cref="EvidencePack"/> instance.
    /// </summary>
    public FijiPayroll.Domain.Entities.Payroll.EvidencePack Build()
    {
        if (_executiveSummary == null) throw new InvalidOperationException("Executive summary is required to build an EvidencePack.");
        if (_ledgerIntegrity == null) throw new InvalidOperationException("Ledger integrity manifest is required to build an EvidencePack.");
        if (_reportSnapshotIndex == null) throw new InvalidOperationException("SSRS Report snapshot index is required to build an EvidencePack.");
        if (_validationOutput == null) throw new InvalidOperationException("Validation output is required to build an EvidencePack.");
        if (_traceability == null) throw new InvalidOperationException("Traceability evidence is required to build an EvidencePack.");
        if (_reconciliation == null) throw new InvalidOperationException("Reconciliation summary is required to build an EvidencePack.");

        return new FijiPayroll.Domain.Entities.Payroll.EvidencePack
        {
            CorrelationId = _correlationId,
            GeneratedUtc = _generatedUtc,
            GeneratedBy = _generatedBy,
            EvidencePackSignature = _evidencePackSignature,
            SignatureTimestamp = _signatureTimestamp,
            SystemBuildVersionHash = _systemBuildVersionHash,
            ApplicationVersion = _applicationVersion,
            GitCommitHash = _gitCommitHash,
            AssemblyVersionSnapshot = _assemblyVersionSnapshot,
            ExecutiveSummary = _executiveSummary,
            LedgerIntegrity = _ledgerIntegrity,
            ReportSnapshotIndex = _reportSnapshotIndex,
            EmployeeEvidence = _employeeEvidence.AsReadOnly(),
            ValidationOutput = _validationOutput,
            Traceability = _traceability,
            Reconciliation = _reconciliation
        };
    }
}
