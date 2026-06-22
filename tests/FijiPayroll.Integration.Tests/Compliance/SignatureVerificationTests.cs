using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Features.Compliance.Queries;
using FijiPayroll.Application.Services;
using FijiPayroll.Application.Services.EvidencePack;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Infrastructure.Services.ComplianceEvidence;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Interceptors;
using FijiPayroll.Persistence.Repositories;
using FijiPayroll.SDK.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FijiPayroll.Integration.Tests.Compliance;

public sealed class SignatureVerificationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SSRSReportSnapshotService _snapshotService;
    private readonly SimplePdfGenerator _pdfGenerator;
    private readonly FileArchiveManager _archiveManager;
    private readonly ComplianceMetadataAssembler _metadataAssembler;
    private readonly BuildVersionProvider _buildVersionProvider;
    private readonly EvidencePackSignatureService _signatureService;
    private readonly SignatureVerifierService _verifierService;
    private readonly EvidencePackGeneratorService _generatorService;

    public SignatureVerificationTests()
    {
        // 1. Setup in-memory DbContext
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenantProvider = Substitute.For<ITenantProvider>();
        _tenantProvider.GetCurrentCompanyId().Returns(1);

        var interceptor = new FijiPayroll.Persistence.Interceptors.AuditableEntityInterceptor(Substitute.For<ICurrentUserAccessor>());
        _context = new ApplicationDbContext(options, interceptor, _tenantProvider);

        // 2. Setup repos
        var compRepo = new PayrollComponentRepository(_context);
        var runRepo = new PayrollRunRepository(_context);
        var taxRepo = new TaxBracketRepository(_context);
        var empRepo = new EmployeeRepository(_context);
        var mockLookupRepo = Substitute.For<IMasterLookupRepository>();

        _unitOfWork = new UnitOfWork(_context, compRepo, runRepo, empRepo, taxRepo, mockLookupRepo);

        // 3. Setup Evidence Pack & Signature services
        var registry = new ReportSnapshotRegistry();
        var serviceProvider = Substitute.For<IServiceProvider>();

        _snapshotService = new SSRSReportSnapshotService(registry, serviceProvider);
        _pdfGenerator = new SimplePdfGenerator();
        _archiveManager = new FileArchiveManager();

        var validationLogger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ComplianceValidationService>>();
        var validationService = new ComplianceValidationService(new List<IComplianceValidator>(), validationLogger);
        _metadataAssembler = new ComplianceMetadataAssembler(validationService);

        _buildVersionProvider = new BuildVersionProvider();
        _signatureService = new EvidencePackSignatureService(_buildVersionProvider, _unitOfWork);
        _verifierService = new SignatureVerifierService(_tenantProvider);

        _generatorService = new EvidencePackGeneratorService(
            _unitOfWork,
            _snapshotService,
            _pdfGenerator,
            _archiveManager,
            _metadataAssembler,
            _buildVersionProvider,
            _signatureService
        );
    }

    [Fact]
    public async Task GenerateAndVerify_ShouldSucceed_WhenEvidencePackIsSigned()
    {
        // Arrange
        var run = CreateMockPayrollRun(1, 100);
        await _context.PayrollRuns.AddAsync(run);

        var ledger1 = CreateMockLedger(1, 100, 10, 2000m, 200m, 1640m);
        await _context.PayrollLedgers.AddAsync(ledger1);

        var runEmp1 = CreateMockRunEmployee(100, 10, "John Doe", "123456789", "12345", 2000m, 200m, 1640m);
        run.AddEmployee(runEmp1);

        await _context.SaveChangesAsync();

        // Act
        var pack = await _generatorService.GenerateEvidencePackAsync(1, 100, "test-user");
        var signedZip = await _generatorService.GenerateEvidenceZipArchiveAsync(pack);

        // Assert
        signedZip.Should().NotBeNull();
        
        // Verify via service
        Func<Task> verifyAct = async () => await _verifierService.VerifyEvidencePackSignatureAsync(signedZip);
        await verifyAct.Should().NotThrowAsync();

        // Verify via MediatR query handler
        var handler = new VerifyEvidencePackQueryHandler(_verifierService);
        var query = new VerifyEvidencePackQuery(signedZip);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Verify_ShouldThrowException_WhenZipBytesAreTampered()
    {
        // Arrange
        var run = CreateMockPayrollRun(1, 100);
        await _context.PayrollRuns.AddAsync(run);

        var ledger1 = CreateMockLedger(1, 100, 10, 2000m, 200m, 1640m);
        await _context.PayrollLedgers.AddAsync(ledger1);

        var runEmp1 = CreateMockRunEmployee(100, 10, "John Doe", "123456789", "12345", 2000m, 200m, 1640m);
        run.AddEmployee(runEmp1);

        await _context.SaveChangesAsync();

        var pack = await _generatorService.GenerateEvidencePackAsync(1, 100, "test-user");
        var signedZip = await _generatorService.GenerateEvidenceZipArchiveAsync(pack);

        // Act & Assert 1: Tamper with zip bytes (change a random byte)
        var tamperedZip = signedZip.ToArray();
        // Locate a byte that won't break basic ZIP header reading but fails content validation
        tamperedZip[100] = (byte)(tamperedZip[100] ^ 0xFF);

        Func<Task> verifyAct = async () => await _verifierService.VerifyEvidencePackSignatureAsync(tamperedZip);
        await verifyAct.Should().ThrowAsync<EvidencePackTamperedException>()
            .WithMessage("*mismatch*");
    }

    [Fact]
    public async Task Verify_ShouldThrowException_WhenManifestIsMissing()
    {
        // Arrange
        var run = CreateMockPayrollRun(1, 100);
        await _context.PayrollRuns.AddAsync(run);

        var ledger1 = CreateMockLedger(1, 100, 10, 2000m, 200m, 1640m);
        await _context.PayrollLedgers.AddAsync(ledger1);

        var runEmp1 = CreateMockRunEmployee(100, 10, "John Doe", "123456789", "12345", 2000m, 200m, 1640m);
        run.AddEmployee(runEmp1);

        await _context.SaveChangesAsync();

        var pack = await _generatorService.GenerateEvidencePackAsync(1, 100, "test-user");
        var unsignedZip = await _archiveManager.CreateEvidenceZipAsync(
            pack,
            _pdfGenerator.GeneratePdf(pack),
            new List<ReportSnapshotResult>()
        );

        // Act & Assert: verify the unsigned ZIP (which lacks signature.manifest.json)
        Func<Task> verifyAct = async () => await _verifierService.VerifyEvidencePackSignatureAsync(unsignedZip);
        await verifyAct.Should().ThrowAsync<EvidencePackTamperedException>()
            .WithMessage("*signature.manifest.json is missing.*");
    }

    [Fact]
    public async Task Verify_ShouldBlock_CrossTenantAccess()
    {
        // Arrange
        var run = CreateMockPayrollRun(1, 100);
        await _context.PayrollRuns.AddAsync(run);

        var ledger1 = CreateMockLedger(1, 100, 10, 2000m, 200m, 1640m);
        await _context.PayrollLedgers.AddAsync(ledger1);

        var runEmp1 = CreateMockRunEmployee(100, 10, "John Doe", "123456789", "12345", 2000m, 200m, 1640m);
        run.AddEmployee(runEmp1);

        await _context.SaveChangesAsync();

        var pack = await _generatorService.GenerateEvidencePackAsync(1, 100, "test-user");
        var signedZip = await _generatorService.GenerateEvidenceZipArchiveAsync(pack);

        // Act: switch active tenant context to company 2
        _tenantProvider.GetCurrentCompanyId().Returns(2);

        // Assert
        Func<Task> verifyAct = async () => await _verifierService.VerifyEvidencePackSignatureAsync(signedZip);
        await verifyAct.Should().ThrowAsync<EvidencePackTamperedException>()
            .WithMessage("*Unauthorized context access*");
    }

    [Fact]
    public async Task SignEvidenceZip_ShouldBeDeterministic()
    {
        // Arrange
        var run = CreateMockPayrollRun(1, 100);
        await _context.PayrollRuns.AddAsync(run);

        var ledger1 = CreateMockLedger(1, 100, 10, 2000m, 200m, 1640m);
        await _context.PayrollLedgers.AddAsync(ledger1);

        var runEmp1 = CreateMockRunEmployee(100, 10, "John Doe", "123456789", "12345", 2000m, 200m, 1640m);
        run.AddEmployee(runEmp1);

        await _context.SaveChangesAsync();

        var pack = await _generatorService.GenerateEvidencePackAsync(1, 100, "test-user");
        
        var unsignedZip1 = await _archiveManager.CreateEvidenceZipAsync(
            pack,
            _pdfGenerator.GeneratePdf(pack),
            new List<ReportSnapshotResult>()
        );

        var unsignedZip2 = await _archiveManager.CreateEvidenceZipAsync(
            pack,
            _pdfGenerator.GeneratePdf(pack),
            new List<ReportSnapshotResult>()
        );

        // Act
        var signedZip1 = await _signatureService.SignEvidenceZipAsync(unsignedZip1, 1, 100);
        var signedZip2 = await _signatureService.SignEvidenceZipAsync(unsignedZip2, 1, 100);

        // Extract manifest from zip 1 & 2 to assert signature values match
        string sig1 = ExtractSignature(signedZip1);
        string sig2 = ExtractSignature(signedZip2);

        // Assert: Signatures must be identical for identical input + identical key pair
        sig1.Should().Be(sig2);
    }

    private static string ExtractSignature(byte[] zipBytes)
    {
        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        var entry = archive.GetEntry("signature.manifest.json");
        using var stream = entry!.Open();
        using var reader = new StreamReader(stream);
        string json = reader.ReadToEnd();
        var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("Signature").GetString()!;
    }

    private static PayrollRun CreateMockPayrollRun(int companyId, int runId)
    {
        var run = PayrollRun.Create(
            companyId: companyId,
            runCode: $"RUN-{runId}",
            periodName: $"Period-{runId}",
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            paymentDate: new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc),
            frequency: FijiPayroll.Domain.Enumerations.PayrollFrequencyType.Fortnightly,
            description: "Mock payroll run"
        );

        typeof(PayrollRun).GetProperty("Id")?.SetValue(run, runId);
        return run;
    }

    private static PayrollRunEmployee CreateMockRunEmployee(
        int runId,
        int employeeId,
        string name,
        string tin,
        string fnpf,
        decimal gross,
        decimal paye,
        decimal net)
    {
        var emp = PayrollRunEmployee.Create(
            payrollRunId: runId,
            employeeId: employeeId,
            employeeName: name,
            tin: tin,
            fnpfNumber: fnpf,
            residencyStatus: "Resident",
            department: "Audit",
            baseSalary: gross,
            grossPay: gross,
            totalAllowances: 0,
            totalDeductions: gross - net,
            netPay: net,
            payeTax: paye,
            fnpfEmployeeContribution: gross * 0.08m,
            fnpfEmployerContribution: gross * 0.10m,
            taxVersionUsed: "v1",
            calculationRequestId: Guid.NewGuid()
        );

        return emp;
    }

    private static PayrollLedger CreateMockLedger(
        int companyId,
        int runId,
        int employeeId,
        decimal gross,
        decimal paye,
        decimal net)
    {
        var fnpfEE = gross * 0.08m;
        var fnpfER = gross * 0.10m;

        string empHash = FijiPayroll.Application.Services.EvidencePack.LedgerIntegrityVerifier.FormatLedgerRecord(
            PayrollLedgerEmployee.Create(companyId, employeeId, $"Employee-{employeeId}", "123456789", "12345", gross, paye, fnpfEE, fnpfER, net, "temp")
        );
        string calculatedHash = FijiPayroll.Application.Services.EvidencePack.DeterministicHashGenerator.ComputeSha256Hash(empHash);

        var ledger = PayrollLedger.Create(
            companyId: companyId,
            payrollRunId: runId,
            totalGross: gross,
            totalPaye: paye,
            totalFnpfEmployee: fnpfEE,
            totalFnpfEmployer: fnpfER,
            totalNetPay: net,
            createdBy: "system",
            hash: calculatedHash
        );

        var emp = PayrollLedgerEmployee.Create(
            companyId: companyId,
            employeeId: employeeId,
            employeeName: $"Employee-{employeeId}",
            employeeTin: "123456789",
            employeeFnpfNumber: "12345",
            gross: gross,
            paye: paye,
            fnpfEmployee: fnpfEE,
            fnpfEmployer: fnpfER,
            netPay: net,
            hash: calculatedHash
        );

        ledger.AddEmployee(emp);
        return ledger;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
