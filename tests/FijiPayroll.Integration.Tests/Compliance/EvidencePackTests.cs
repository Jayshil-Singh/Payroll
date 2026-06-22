using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Features.Compliance.Commands;
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

public sealed class EvidencePackTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SSRSReportSnapshotService _snapshotService;
    private readonly SimplePdfGenerator _pdfGenerator;
    private readonly FileArchiveManager _archiveManager;
    private readonly ComplianceMetadataAssembler _metadataAssembler;
    private readonly IEvidencePackGeneratorService _generatorService;

    public EvidencePackTests()
    {
        // 1. Setup in-memory DbContext
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenantProvider = Substitute.For<ITenantProvider>();
        _tenantProvider.GetCurrentCompanyId().Returns(1);

        var interceptor = new FijiPayroll.Persistence.Interceptors.AuditableEntityInterceptor(Substitute.For<ICurrentUserAccessor>());
        _context = new ApplicationDbContext(options, interceptor, _tenantProvider);

        // 2. Setup repositories and Unit of Work
        var compRepo = new PayrollComponentRepository(_context);
        var runRepo = new PayrollRunRepository(_context);
        var taxRepo = new TaxBracketRepository(_context);
        var empRepo = new EmployeeRepository(_context);
        var mockLookupRepo = Substitute.For<IMasterLookupRepository>();

        _unitOfWork = new UnitOfWork(_context, compRepo, runRepo, empRepo, taxRepo, mockLookupRepo);

        // 3. Setup Evidence Pack services
        var registry = new ReportSnapshotRegistry();
        var serviceProvider = Substitute.For<IServiceProvider>();
        
        _snapshotService = new SSRSReportSnapshotService(registry, serviceProvider);
        _pdfGenerator = new SimplePdfGenerator();
        _archiveManager = new FileArchiveManager();

        var validationLogger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ComplianceValidationService>>();
        var validators = new List<IComplianceValidator>();
        var validationService = new ComplianceValidationService(validators, validationLogger);
        
        _metadataAssembler = new ComplianceMetadataAssembler(validationService);

        var buildVersionProvider = new BuildVersionProvider();
        var signatureService = new EvidencePackSignatureService(buildVersionProvider, _unitOfWork);

        _generatorService = new EvidencePackGeneratorService(
            _unitOfWork,
            _snapshotService,
            _pdfGenerator,
            _archiveManager,
            _metadataAssembler,
            buildVersionProvider,
            signatureService
        );
    }

    [Fact]
    public async Task GenerateEvidencePack_ShouldBeFullyDeterministic_AndProduceIdenticalZipHashes()
    {
        // Arrange: Seed a finalized run and ledgers
        var run = CreateMockPayrollRun(companyId: 1, runId: 100);
        await _context.PayrollRuns.AddAsync(run);

        var ledger1 = CreateMockLedger(companyId: 1, runId: 100, employeeId: 10, gross: 2000m, paye: 200m, net: 1640m);
        var ledger2 = CreateMockLedger(companyId: 1, runId: 100, employeeId: 11, gross: 3000m, paye: 350m, net: 2410m);
        await _context.PayrollLedgers.AddRangeAsync(ledger1, ledger2);

        // Add trace record and link line items
        var runEmp1 = CreateMockRunEmployee(100, 10, "John Doe", "123456789", "12345", 2000m, 200m, 1640m);
        var trace1 = PayrollRunEmployeeTrace.Create(runEmp1.Id, "[Trace] Calculated Earning 'Basic' (BASIC): $2,000.00\n[Trace] FNPF Employee contribution (8%): $160.00\n[Trace] PAYE tax calculated: $200.00");
        runEmp1.SetTrace(trace1);
        run.AddEmployee(runEmp1);

        await _context.SaveChangesAsync();

        // Act: Generate twice
        var pack1 = await _generatorService.GenerateEvidencePackAsync(1, 100, "test-admin");
        var zipBytes1 = await _generatorService.GenerateEvidenceZipArchiveAsync(pack1);
        string hash1 = DeterministicHashGenerator.ComputeSha256Hash(zipBytes1);

        // Reset/recreate generators or just run again to assert absolute determinism
        var pack2 = await _generatorService.GenerateEvidencePackAsync(1, 100, "test-admin");
        var zipBytes2 = await _generatorService.GenerateEvidenceZipArchiveAsync(pack2);
        string hash2 = DeterministicHashGenerator.ComputeSha256Hash(zipBytes2);

        // Assert: Hashes must be identical
        hash1.Should().Be(hash2);
        zipBytes1.Should().Equal(zipBytes2);
    }

    [Fact]
    public void LedgerIntegrityVerifier_ShouldDetectTampering_WhenRecordIsModified()
    {
        // Arrange
        var verifier = new LedgerIntegrityVerifier();

        var ledger = CreateMockLedger(companyId: 1, runId: 100, employeeId: 10, gross: 2000m, paye: 200m, net: 1640m);
        var ledgers = ledger.Employees;

        // Act 1: Verify valid record
        var result1 = verifier.VerifyLedgerIntegrity(ledgers);
        result1.IntegrityStatus.Should().Be("PASS");

        // Act 2: Tamper with record (mutate Gross but keep old Hash)
        var originalEmp = ledger.Employees.First();
        var tamperedEmp = PayrollLedgerEmployee.Create(
            companyId: originalEmp.CompanyId,
            employeeId: originalEmp.EmployeeId,
            employeeName: originalEmp.EmployeeName,
            employeeTin: originalEmp.EmployeeTin,
            employeeFnpfNumber: originalEmp.EmployeeFnpfNumber,
            gross: 2500m, // Tampered!
            paye: originalEmp.PAYE,
            fnpfEmployee: originalEmp.FNPFEmployee,
            fnpfEmployer: originalEmp.FNPFEmployer,
            netPay: originalEmp.NetPay,
            hash: originalEmp.Hash // Original Hash
        );

        var result2 = verifier.VerifyLedgerIntegrity(new[] { tamperedEmp });

        // Assert
        result2.IntegrityStatus.Should().Be("FAIL");
    }

    [Fact]
    public async Task GenerateEvidencePackCommand_ShouldBlock_CrossCompanyAccess()
    {
        // Arrange
        // Current tenant context is company 1
        _tenantProvider.GetCurrentCompanyId().Returns(1);

        // Seed a run belonging to company 2
        var run = CreateMockPayrollRun(companyId: 2, runId: 200);
        await _context.PayrollRuns.AddAsync(run);
        await _context.SaveChangesAsync();

        var handler = new GenerateEvidencePackCommandHandler(_generatorService, _unitOfWork, _tenantProvider);
        var command = new GenerateEvidencePackCommand(200, "test-user");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Unauthorized context access");
    }

    [Fact]
    public async Task TraceConsistency_ShouldCorrectlyParseOrderedSteps_FromTraceText()
    {
        // Arrange
        var run = CreateMockPayrollRun(companyId: 1, runId: 300);
        await _context.PayrollRuns.AddAsync(run);

        var ledger = CreateMockLedger(companyId: 1, runId: 300, employeeId: 30, gross: 1000m, paye: 100m, net: 820m);
        await _context.PayrollLedgers.AddAsync(ledger);

        var runEmp = CreateMockRunEmployee(300, 30, "Alice Jones", "987654321", "54321", 1000m, 100m, 820m);
        
        // Setup trace text detailing ordered step execution
        string traceText = "[Trace] Starting calculations...\n" +
                           "[Trace] Calculated Earning 'Base Salary' (BASIC): $1,000.00\n" +
                           "[Trace] Calculated Allowance 'Overtime' (OT): $50.00\n" +
                           "[Trace] FNPF Employee contribution (8%): $80.00\n" +
                           "[Trace] PAYE tax calculated: $100.00";
        
        var trace = PayrollRunEmployeeTrace.Create(runEmp.Id, traceText);
        runEmp.SetTrace(trace);
        run.AddEmployee(runEmp);

        await _context.SaveChangesAsync();

        // Act
        var pack = await _generatorService.GenerateEvidencePackAsync(1, 300, "admin");

        // Assert
        pack.Traceability.Should().NotBeNull();
        pack.Traceability.EmployeeTraces.Should().ContainSingle();
        
        var empTrace = pack.Traceability.EmployeeTraces[0];
        empTrace.EmployeeId.Should().Be(30);
        empTrace.EmployeeName.Should().Be("Alice Jones");
        empTrace.OrderedStepReferenceIds.Should().ContainInOrder("BASIC", "OT");
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

        // Use reflection to set private Id
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
