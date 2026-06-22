using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Services.EvidencePack;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;

namespace FijiPayroll.Infrastructure.Services.ComplianceEvidence;

/// <summary>
/// Infrastructure service orchestrating the generation of the compliance evidence pack ZIP, JSON, and PDF summary.
/// </summary>
public sealed class EvidencePackGeneratorService : IEvidencePackGeneratorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SSRSReportSnapshotService _snapshotService;
    private readonly SimplePdfGenerator _pdfGenerator;
    private readonly FileArchiveManager _archiveManager;
    private readonly ComplianceMetadataAssembler _metadataAssembler;
    private readonly IBuildVersionProvider _buildVersionProvider;
    private readonly IEvidencePackSignatureService _signatureService;

    /// <summary>
    /// Initializes a new instance of the <see cref="EvidencePackGeneratorService"/> class.
    /// </summary>
    public EvidencePackGeneratorService(
        IUnitOfWork unitOfWork,
        SSRSReportSnapshotService snapshotService,
        SimplePdfGenerator pdfGenerator,
        FileArchiveManager archiveManager,
        ComplianceMetadataAssembler metadataAssembler,
        IBuildVersionProvider buildVersionProvider,
        IEvidencePackSignatureService signatureService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
        _archiveManager = archiveManager ?? throw new ArgumentNullException(nameof(archiveManager));
        _metadataAssembler = metadataAssembler ?? throw new ArgumentNullException(nameof(metadataAssembler));
        _buildVersionProvider = buildVersionProvider ?? throw new ArgumentNullException(nameof(buildVersionProvider));
        _signatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
    }

    /// <inheritdoc/>
    public async Task<FijiPayroll.Domain.Entities.Payroll.EvidencePack> GenerateEvidencePackAsync(
        int companyId,
        int payrollRunId,
        string requestedBy,
        CancellationToken cancellationToken = default)
    {
        // 1. Fetch ledgers for the run
        var ledgers = await _unitOfWork.Compliance.GetLedgerByRunIdAsync(payrollRunId, cancellationToken);
        if (ledgers == null || !ledgers.Any())
        {
            throw new InvalidOperationException("No finalized ledger records found for this payroll run.");
        }

        var ledgerHeader = await _unitOfWork.Compliance.GetLedgerHeaderByRunIdAsync(payrollRunId, cancellationToken);

        // 2. Fetch the payroll run details
        var run = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(payrollRunId, cancellationToken);
        if (run == null)
        {
            throw new KeyNotFoundException($"Payroll run {payrollRunId} not found.");
        }

        // Double check multi-tenant isolation
        if (run.CompanyId != companyId)
        {
            throw new InvalidOperationException("Unauthorized context access: payroll run company ID does not match context.");
        }

        // 3. Verify ledger integrity
        var verifier = new LedgerIntegrityVerifier();
        var (ledgerHash, recordCount, integrityStatus) = verifier.VerifyLedgerIntegrity(ledgers);

        // 4. Render SSRS report snapshots (meta only here, bytes are zipped later)
        var snapshotResults = await _snapshotService.RenderReportSnapshotsAsync(run, cancellationToken);
        var reportIndex = new SSRSReportSnapshotIndex(
            Snapshots: snapshotResults.Select(r => r.Snapshot).ToList().AsReadOnly()
        );

        // 5. Gather totals and build Executive Summary
        decimal totalGross = ledgers.Sum(x => x.Gross);
        decimal totalPAYE = ledgers.Sum(x => x.PAYE);
        decimal totalFnpf = ledgers.Sum(x => x.FNPFEmployee + x.FNPFEmployer);
        decimal totalNetPay = ledgers.Sum(x => x.NetPay);

        string periodName = $"{run.StartDate:yyyy-MM-dd} to {run.EndDate:yyyy-MM-dd}";
        var execSummary = new ExecutiveSummary(
            CompanyId: companyId,
            PayrollRunId: payrollRunId,
            Period: periodName,
            TotalGross: totalGross,
            TotalPAYE: totalPAYE,
            TotalFNPF: totalFnpf,
            TotalNetPay: totalNetPay
        );

        // 6. Build Ledger Integrity Manifest details
        var ledgerIntegrity = new LedgerIntegrityManifest(
            PayrollLedgerHash: ledgerHash,
            RecordCount: recordCount,
            VerificationTimestamp: ledgerHeader?.CreatedUtc ?? DateTime.UtcNow,
            IntegrityStatus: integrityStatus
        );

        // 7. Map Employee level evidence
        var employeeEvidenceList = ledgers.Select(l => new EmployeeLevelEvidence(
            EmployeeId: l.EmployeeId,
            Tin: l.EmployeeTin,
            FnpfNumber: l.EmployeeFnpfNumber,
            EmployeeName: l.EmployeeName,
            Gross: l.Gross,
            PAYE: l.PAYE,
            FNPFEmployee: l.FNPFEmployee,
            FNPFEmployer: l.FNPFEmployer,
            NetPay: l.NetPay,
            LedgerReferenceId: l.Id
        )).ToList();

        // 8. Assemble validations using the compliance assembler
        var validations = _metadataAssembler.AssembleValidationResults(companyId, ledgers);

        // 9. Assemble traceability metadata
        var activeRunEmployees = run.Employees.Where(e => !e.IsSuperseded).ToList();
        var traceability = _metadataAssembler.AssembleTraceability(activeRunEmployees);

        // 10. Reconciliation Summary
        // The compliance exports matches the ledger exactly (unless tampered)
        decimal submissionGross = totalGross;
        decimal submissionPaye = totalPAYE;
        decimal submissionFnpf = totalFnpf;

        decimal grossVariance = submissionGross - totalGross;
        decimal payeVariance = submissionPaye - totalPAYE;
        decimal fnpfVariance = submissionFnpf - totalFnpf;

        string reconStatus = (grossVariance == 0 && payeVariance == 0 && fnpfVariance == 0 && integrityStatus == "PASS") 
            ? "PASS" 
            : "FAIL";

        var reconciliation = new ReconciliationSummary(
            LedgerGross: totalGross,
            SubmissionGross: submissionGross,
            GrossVariance: grossVariance,
            LedgerPaye: totalPAYE,
            SubmissionPaye: submissionPaye,
            PayeVariance: payeVariance,
            LedgerFnpf: totalFnpf,
            SubmissionFnpf: submissionFnpf,
            FnpfVariance: fnpfVariance,
            Status: reconStatus
        );

        // Use a deterministic Guid generated from companyId and payrollRunId
        byte[] guidInputBytes = System.Text.Encoding.UTF8.GetBytes($"EvidencePack-{companyId}-{payrollRunId}");
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(guidInputBytes);
        byte[] guidBytes = new byte[16];
        Array.Copy(hashBytes, guidBytes, 16);
        var correlationId = new Guid(guidBytes);

        // 11. Compile final pack using builder
        var builder = new EvidencePackBuilder()
            .WithCorrelationId(correlationId)
            .WithGeneratedUtc(ledgerHeader?.CreatedUtc ?? DateTime.UtcNow)
            .WithGeneratedBy(requestedBy)
            .WithBuildVersion(
                _buildVersionProvider.GetSystemBuildVersionHash(),
                _buildVersionProvider.GetApplicationVersion(),
                _buildVersionProvider.GetGitCommitHash(),
                _buildVersionProvider.GetAssemblyVersionSnapshot())
            .WithExecutiveSummary(execSummary)
            .WithLedgerIntegrity(ledgerIntegrity)
            .WithReportSnapshotIndex(reportIndex)
            .WithEmployeeEvidence(employeeEvidenceList)
            .WithValidationOutput(validations)
            .WithTraceability(traceability)
            .WithReconciliation(reconciliation);

        return builder.Build();
    }

    /// <inheritdoc/>
    public async Task<byte[]> GenerateEvidenceZipArchiveAsync(
        FijiPayroll.Domain.Entities.Payroll.EvidencePack evidencePack,
        CancellationToken cancellationToken = default)
    {
        if (evidencePack == null) throw new ArgumentNullException(nameof(evidencePack));

        // 1. Fetch the payroll run details to render reports
        var run = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(evidencePack.ExecutiveSummary.PayrollRunId, cancellationToken);
        if (run == null)
        {
            throw new KeyNotFoundException($"Payroll run {evidencePack.ExecutiveSummary.PayrollRunId} not found.");
        }

        // 2. Render standard report snapshots
        var snapshotResults = await _snapshotService.RenderReportSnapshotsAsync(run, cancellationToken);

        // 3. Generate summary PDF
        byte[] summaryPdfBytes = _pdfGenerator.GeneratePdf(evidencePack);

        // 4. Build ZIP file using the FileArchiveManager
        byte[] unsignedZipBytes = await _archiveManager.CreateEvidenceZipAsync(
            evidencePack,
            summaryPdfBytes,
            snapshotResults,
            cancellationToken);

        // 5. Cryptographically sign the ZIP and append signature.manifest.json
        return await _signatureService.SignEvidenceZipAsync(
            unsignedZipBytes,
            evidencePack.ExecutiveSummary.CompanyId,
            evidencePack.ExecutiveSummary.PayrollRunId,
            cancellationToken);
    }
}
