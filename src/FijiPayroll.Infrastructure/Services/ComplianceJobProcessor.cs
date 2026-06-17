using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.SDK.Contracts;
using FijiPayroll.SDK.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Infrastructure.Services;

/// <summary>
/// Background worker processing compliance jobs asynchronously from database queues.
/// Implements exponential backoffs and writes JSON audit manifests on file generation.
/// </summary>
public sealed class ComplianceJobProcessor : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEnumerable<IBankFileGenerator> _bankGenerators;
    private readonly IComplianceFileService _complianceFileService;
    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly ILogger<ComplianceJobProcessor> _logger;
    private readonly CancellationTokenSource _cts = new();
    private Task? _processingTask;
    private int _isStarted;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplianceJobProcessor"/> class.
    /// </summary>
    public ComplianceJobProcessor(
        IServiceScopeFactory scopeFactory,
        IEnumerable<IBankFileGenerator> bankGenerators,
        IComplianceFileService complianceFileService,
        IFileStorageProvider fileStorageProvider,
        ILogger<ComplianceJobProcessor> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _bankGenerators = bankGenerators ?? throw new ArgumentNullException(nameof(bankGenerators));
        _complianceFileService = complianceFileService ?? throw new ArgumentNullException(nameof(complianceFileService));
        _fileStorageProvider = fileStorageProvider ?? throw new ArgumentNullException(nameof(fileStorageProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Starts the background polling loop.
    /// </summary>
    public void Start()
    {
        if (Interlocked.CompareExchange(ref _isStarted, 1, 0) == 0)
        {
            _processingTask = Task.Run(ProcessJobsLoopAsync);
            _logger.LogInformation("Compliance background job processor started.");
        }
    }

    /// <summary>
    /// Stops the background polling loop.
    /// </summary>
    public void Stop()
    {
        if (Interlocked.CompareExchange(ref _isStarted, 0, 1) == 1)
        {
            _cts.Cancel();
            try
            {
                _processingTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Expected on stop
            }
            _logger.LogInformation("Compliance background job processor stopped.");
        }
    }

    private async Task ProcessJobsLoopAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // Fetch next pending or retrying compliance job
                var job = await unitOfWork.Compliance.GetNextJobToProcessAsync(_cts.Token);

                if (job != null)
                {
                    await ProcessJobAsync(job, unitOfWork, _cts.Token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in compliance job processor polling cycle.");
            }

            // Poll every 5 seconds
            await Task.Delay(TimeSpan.FromSeconds(5), _cts.Token);
        }
    }

    private async Task ProcessJobAsync(ComplianceJob job, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing compliance job ID {JobId} of type '{JobType}'", job.Id, job.JobType);
            job.StartJob();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Run core generation logic based on job type
            if (job.JobType.StartsWith("FRCS_Generate", StringComparison.OrdinalIgnoreCase))
            {
                await HandleFrcsGenerationAsync(job, unitOfWork, cancellationToken);
            }
            else if (job.JobType.StartsWith("FNPF_Generate", StringComparison.OrdinalIgnoreCase))
            {
                await HandleFnpfGenerationAsync(job, unitOfWork, cancellationToken);
            }
            else if (job.JobType.StartsWith("Bank_Generate", StringComparison.OrdinalIgnoreCase))
            {
                await HandleBankGenerationAsync(job, unitOfWork, cancellationToken);
            }
            else
            {
                throw new NotSupportedException($"Job type '{job.JobType}' is not supported.");
            }

            job.CompleteJob();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully completed compliance job ID {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing compliance job ID {JobId}", job.Id);
            bool canRetry = job.AttemptCount < 5;
            job.FailJob(ex.Message, canRetry);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (canRetry)
            {
                // Reschedule for retry with exponential backoff: 5, 10, 20, 40... seconds
                int backoffSeconds = 5 * (int)Math.Pow(2, job.AttemptCount - 1);
                _logger.LogWarning("Rescheduling Job ID {JobId} to retry in {Backoff} seconds.", job.Id, backoffSeconds);
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), _cts.Token);
                    if (!_cts.Token.IsCancellationRequested)
                    {
                        using var retryScope = _scopeFactory.CreateScope();
                        var retryUow = retryScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        var targetJob = await retryUow.Compliance.GetJobByIdAsync(job.Id);
                        if (targetJob != null && targetJob.Status == ComplianceJobStatus.Retrying)
                        {
                            targetJob.ResetForRetry();
                            await retryUow.SaveChangesAsync();
                        }
                    }
                });
            }
        }
    }

    private async Task HandleFrcsGenerationAsync(ComplianceJob job, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        // 1. Get active company period
        var period = await unitOfWork.Compliance.GetActivePeriodAsync(job.CompanyId, cancellationToken);
        if (period == null) throw new InvalidOperationException("No active compliance period found to generate FRCS returns.");

        // 2. Fetch finalized payroll ledgers
        var closedRunsResult = await unitOfWork.PayrollRuns.GetPagedAsync(job.CompanyId, null, PayrollRunStatus.Locked, 1, 1000, cancellationToken);
        var runs = closedRunsResult.Items;

        var ledgersList = new List<PayrollLedger>();
        foreach (var run in runs)
        {
            var l = await unitOfWork.Compliance.GetLedgerByRunIdAsync(run.Id, cancellationToken);
            ledgersList.AddRange(l);
        }

        if (!ledgersList.Any()) throw new InvalidOperationException("No finalized payroll ledger entries found to generate FRCS returns.");

        var paymentDetails = ledgersList.Select(x => new PaymentDetail(
            EmployeeId: x.EmployeeId,
            EmployeeName: x.EmployeeName,
            Tin: x.EmployeeTin,
            FnpfNumber: x.EmployeeFnpfNumber,
            Gross: x.Gross,
            Paye: x.PAYE,
            FnpfEmployee: x.FNPFEmployee,
            FnpfEmployer: x.FNPFEmployer,
            BankAccountNumber: string.Empty,
            Amount: 0
        )).ToList();

        // 3. Generate CSV
        string csvContent = _complianceFileService.GenerateFrcsCsv("123456789", paymentDetails, period.Month, period.Year);
        string fileHash = ComputeSHA256Hash(csvContent);
        string filename = $"FRCS_MER_{job.CompanyId}_{period.Year}_{period.Month:D2}.csv";
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports", "Compliance");
        Directory.CreateDirectory(outputDir);
        string filePath = Path.Combine(outputDir, filename);
        await File.WriteAllTextAsync(filePath, csvContent, Encoding.UTF8, cancellationToken);

        // 4. Save Submission
        var submission = FRCSSubmission.Create(
            job.CompanyId,
            period.Id,
            csvContent,
            filePath,
            fileHash,
            "Payroll Engine 1.0.0",
            "Formula Engine 2.1.0",
            "Compliance Engine 1.3.0",
            "Statutory Rules v2026.01"
        );
        await unitOfWork.Compliance.AddFRCSSubmissionAsync(submission, cancellationToken);

        // 5. Write manifest file
        await WriteManifestFileAsync(outputDir, filename, fileHash, paymentDetails.Count, paymentDetails.Sum(x => x.Gross), paymentDetails.Sum(x => x.Paye));
    }

    private async Task HandleFnpfGenerationAsync(ComplianceJob job, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var period = await unitOfWork.Compliance.GetActivePeriodAsync(job.CompanyId, cancellationToken);
        if (period == null) throw new InvalidOperationException("No active compliance period found to generate FNPF contribution.");

        var closedRunsResult = await unitOfWork.PayrollRuns.GetPagedAsync(job.CompanyId, null, PayrollRunStatus.Locked, 1, 1000, cancellationToken);
        var runs = closedRunsResult.Items;

        var ledgersList = new List<PayrollLedger>();
        foreach (var run in runs)
        {
            var l = await unitOfWork.Compliance.GetLedgerByRunIdAsync(run.Id, cancellationToken);
            ledgersList.AddRange(l);
        }

        if (!ledgersList.Any()) throw new InvalidOperationException("No finalized payroll ledger entries found to generate FNPF return.");

        var paymentDetails = ledgersList.Select(x => new PaymentDetail(
            EmployeeId: x.EmployeeId,
            EmployeeName: x.EmployeeName,
            Tin: x.EmployeeTin,
            FnpfNumber: x.EmployeeFnpfNumber,
            Gross: x.Gross,
            Paye: x.PAYE,
            FnpfEmployee: x.FNPFEmployee,
            FnpfEmployer: x.FNPFEmployer,
            BankAccountNumber: string.Empty,
            Amount: 0
        )).ToList();

        string csvContent = _complianceFileService.GenerateFnpfCsv("FNPF9999", "Fiji Enterprise Co", period.Month, period.Year, paymentDetails);
        string fileHash = ComputeSHA256Hash(csvContent);
        string filename = $"FNPF_Remit_{job.CompanyId}_{period.Year}_{period.Month:D2}.csv";
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports", "Compliance");
        Directory.CreateDirectory(outputDir);
        string filePath = Path.Combine(outputDir, filename);
        await File.WriteAllTextAsync(filePath, csvContent, Encoding.UTF8, cancellationToken);

        var submission = FNPFSubmission.Create(
            job.CompanyId,
            period.Id,
            csvContent,
            filePath,
            fileHash,
            "Payroll Engine 1.0.0",
            "Formula Engine 2.1.0",
            "Compliance Engine 1.3.0",
            "Statutory Rules v2026.01"
        );
        await unitOfWork.Compliance.AddFNPFSubmissionAsync(submission, cancellationToken);

        await WriteManifestFileAsync(outputDir, filename, fileHash, paymentDetails.Count, paymentDetails.Sum(x => x.Gross), paymentDetails.Sum(x => x.FnpfEmployee + x.FnpfEmployer));
    }

    private async Task HandleBankGenerationAsync(ComplianceJob job, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        // For bank file generation, the JobType parameter specifies the target run and bank code
        // Pattern: Bank_Generate_{RunId}_{BankCode}
        string[] parts = job.JobType.Split('_');
        if (parts.Length < 4 || !int.TryParse(parts[2], out int runId))
        {
            throw new ArgumentException($"Invalid bank generation job type payload format: '{job.JobType}'");
        }
        string bankCode = parts[3];

        var run = await unitOfWork.PayrollRuns.GetByIdAsync(runId, cancellationToken);
        if (run == null) throw new InvalidOperationException($"Payroll run {runId} not found.");

        // Fetch finalized ledger for payments details
        var ledgers = await unitOfWork.Compliance.GetLedgerByRunIdAsync(runId, cancellationToken);
        if (!ledgers.Any()) throw new InvalidOperationException($"No finalized ledger records found for run {runId} to disburse.");

        // Load layouts definition
        var layout = await unitOfWork.Compliance.GetFileLayoutAsync(bankCode, "DirectCredit", cancellationToken);
        if (layout == null) throw new InvalidOperationException($"No bank clearing layouts definition found for bank code '{bankCode}'.");

        // Resolve generator
        var generator = _bankGenerators.FirstOrDefault(x => x.BankCode.Equals(bankCode, StringComparison.OrdinalIgnoreCase));
        if (generator == null) throw new InvalidOperationException($"No bank file generator plugin registered for bank '{bankCode}'.");

        // We fetch the employee details to map their bank account numbers
        var employeeIds = ledgers.Select(x => x.EmployeeId).ToList();
        var employeeList = await unitOfWork.Employees.GetByIdsAsync(employeeIds, cancellationToken);
        var employees = employeeList.ToDictionary(
            x => x.Id,
            x => x.PaymentMethods.FirstOrDefault(pm => pm.IsPrimary && pm.MethodType == PaymentMethodType.BankTransfer)?.BankAccountNumber ?? string.Empty
        );

        var paymentDetails = ledgers.Select(x => new PaymentDetail(
            EmployeeId: x.EmployeeId,
            EmployeeName: x.EmployeeName,
            Tin: x.EmployeeTin,
            FnpfNumber: x.EmployeeFnpfNumber,
            Gross: x.Gross,
            Paye: x.PAYE,
            FnpfEmployee: x.FNPFEmployee,
            FnpfEmployer: x.FNPFEmployer,
            BankAccountNumber: employees.TryGetValue(x.EmployeeId, out string? acct) ? acct ?? string.Empty : string.Empty,
            Amount: x.NetPay
        )).ToList();

        string fileContent = generator.Generate(
            companyName: "Fiji Enterprise Co",
            companyAccount: "987654321",
            bsb: "062-900",
            paymentDate: run.PaymentDate,
            reference: $"PAYRUN_{runId}",
            payments: paymentDetails,
            headerTemplate: layout.HeaderTemplate,
            detailTemplate: layout.DetailTemplate,
            footerTemplate: layout.FooterTemplate,
            delimiter: layout.ColumnDelimiter
        );

        string fileHash = ComputeSHA256Hash(fileContent);
        string filename = $"BankClearing_{bankCode}_{runId}.{layout.FileExtension}";
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports", "Banking");
        Directory.CreateDirectory(outputDir);
        string filePath = Path.Combine(outputDir, filename);
        await File.WriteAllTextAsync(filePath, fileContent, Encoding.UTF8, cancellationToken);

        var bankFile = BankFile.Create(
            job.CompanyId,
            bankCode,
            runId,
            paymentDetails.Sum(x => x.Amount),
            paymentDetails.Count,
            fileContent,
            filePath,
            fileHash
        );
        await unitOfWork.Compliance.AddBankFileAsync(bankFile, cancellationToken);

        await WriteManifestFileAsync(outputDir, filename, fileHash, paymentDetails.Count, paymentDetails.Sum(x => x.Amount), 0);
    }

    private static async Task WriteManifestFileAsync(string dir, string filename, string fileHash, int recordCount, decimal monetaryTotal, decimal taxTotal)
    {
        string manifestPath = Path.Combine(dir, $"{Path.GetFileNameWithoutExtension(filename)}_manifest.json");
        var manifest = new
        {
            GeneratedFile = filename,
            SHA256 = fileHash,
            RecordCount = recordCount,
            MonetaryTotal = monetaryTotal,
            TaxTotal = taxTotal,
            Timestamp = DateTime.UtcNow
        };
        string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(manifestPath, json, Encoding.UTF8);
    }

    private static string ComputeSHA256Hash(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        byte[] hashBytes = SHA256.HashData(bytes);
        var sb = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
        _cts.Dispose();
    }
}
