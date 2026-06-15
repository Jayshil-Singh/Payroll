using ClosedXML.Excel;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Interceptors;
using FijiPayroll.Persistence.Repositories;
using FijiPayroll.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FijiPayroll.Integration.Tests;

public sealed class ImportExportIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IReferenceDataCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImportEngine _importEngine;

    public ImportExportIntegrationTests()
    {
        _currentUserAccessor = Substitute.For<ICurrentUserAccessor>();
        _currentUserAccessor.Username.Returns("import-test-user");

        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.Username.Returns("import-test-user");
        _currentUserService.HasPermission(Arg.Any<string>()).Returns(true);
        _currentUserService.HasCompanyAccess(Arg.Any<int>()).Returns(true);

        _tenantProvider = Substitute.For<ITenantProvider>();
        _tenantProvider.GetCurrentCompanyId().Returns(1);

        _cache = Substitute.For<IReferenceDataCache>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var auditableInterceptor = new AuditableEntityInterceptor(_currentUserAccessor);
        
        // Context with null AuditLogInterceptor to avoid dependencies in basic save
        _context = new ApplicationDbContext(options, auditableInterceptor, _tenantProvider);

        var employeeRepo = new EmployeeRepository(_context);
        var payrollCompRepo = new PayrollComponentRepository(_context);
        var mockRunRepo = Substitute.For<IPayrollRunRepository>();
        var mockTaxRepo = Substitute.For<ITaxBracketRepository>();
        var lookupRepo = new MasterLookupRepository(_context);
        var importJobRepo = new ImportJobRepository(_context);

        _unitOfWork = new UnitOfWork(_context, payrollCompRepo, mockRunRepo, employeeRepo, mockTaxRepo, lookupRepo, importJobRepo);

        _importEngine = new ImportEngine(_unitOfWork, _tenantProvider, _currentUserService, _cache);
    }

    [Fact]
    public async Task GenerateTemplate_ForEmployees_ShouldGenerateNonEmptyExcelStream()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        await _importEngine.GenerateTemplateAsync(stream, "Employees", CancellationToken.None);

        // Assert
        stream.Length.Should().BeGreaterThan(0);
        
        // Read back workbook to verify headers
        stream.Position = 0;
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        worksheet.Should().NotBeNull();
        worksheet!.Cell(1, 1).GetString().Should().Be("Full Name");
        worksheet.Cell(1, 2).GetString().Should().Be("TIN");
        worksheet.Cell(1, 3).GetString().Should().Be("FNPF Number");
    }

    [Fact]
    public async Task ValidateImport_ValidEmployeeExcel_ShouldSavePendingJobAndReturnValidResult()
    {
        // Arrange
        using var stream = CreateValidEmployeeExcelStream();

        // Act
        var result = await _importEngine.ValidateImportAsync(stream, "Employees", CancellationToken.None);

        // Assert
        result.IsValid.Should().BeTrue();
        result.RecordsProcessed.Should().Be(1);
        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(0);
        result.Errors.Should().BeEmpty();

        // Check DB state for ImportJob
        var job = await _context.ImportJobs.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.JobId == result.JobId);
        job.Should().NotBeNull();
        job!.Status.Should().Be("Pending");
        job.ModuleName.Should().Be("Employees");
        job.Payload.Should().Contain("Imported Employee");
        job.Payload.Should().Contain("123456789");
    }

    [Fact]
    public async Task ValidateImport_InvalidEmployeeExcel_ShouldSaveFailedJobAndReturnErrors()
    {
        // Arrange
        using var stream = CreateInvalidEmployeeExcelStream();

        // Act
        var result = await _importEngine.ValidateImportAsync(stream, "Employees", CancellationToken.None);

        // Assert
        result.IsValid.Should().BeFalse();
        result.RecordsProcessed.Should().Be(1);
        result.FailureCount.Should().Be(1);
        result.Errors.Should().NotBeEmpty();

        // Check DB state for ImportJob
        var job = await _context.ImportJobs.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.JobId == result.JobId);
        job.Should().NotBeNull();
        job!.Status.Should().Be("Failed");
        job.ErrorMessage.Should().Contain("Full Name is required");
        job.ErrorMessage.Should().Contain("TIN must be exactly 9 digits");
    }

    [Fact]
    public async Task CommitImport_ValidPendingJob_ShouldImportRecordsAndMarkCompleted()
    {
        // Arrange
        using var stream = CreateValidEmployeeExcelStream();
        var validationResult = await _importEngine.ValidateImportAsync(stream, "Employees", CancellationToken.None);
        validationResult.IsValid.Should().BeTrue();

        // Act
        await _importEngine.CommitImportAsync(validationResult.JobId, CancellationToken.None);

        // Assert
        var job = await _context.ImportJobs.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.JobId == validationResult.JobId);
        job.Should().NotBeNull();
        job!.Status.Should().Be("Completed");

        var employee = await _context.Employees.FirstOrDefaultAsync(x => x.FullName == "Imported Employee");
        employee.Should().NotBeNull();
        employee!.Tin.Should().Be("123456789");
        employee.FnpfNumber.Should().Be("54321-B");
        employee.Department.Should().Be("Engineering");
        employee.Branch.Should().Be("Suva");
        employee.Position.Should().Be("Developer");
        employee.Email.Should().Be("imported@company.com");
    }

    [Fact]
    public async Task TenantIsolation_ShouldFilterImportJobs()
    {
        // Arrange
        using var stream = CreateValidEmployeeExcelStream();
        
        // Tenant 1 imports a file
        var result = await _importEngine.ValidateImportAsync(stream, "Employees", CancellationToken.None);

        // Switch active tenant to 2
        _tenantProvider.GetCurrentCompanyId().Returns(2);

        // Act
        // Attempting to query as Tenant 2
        var jobAsTenant2 = await _context.ImportJobs.FirstOrDefaultAsync(x => x.JobId == result.JobId);

        // Assert
        jobAsTenant2.Should().BeNull(); // RLS query filter isolates the record
    }

    private Stream CreateValidEmployeeExcelStream()
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Template");

        string[] headers = {
            "Full Name", "TIN", "FNPF Number", "Residency Status", "Department",
            "Base Salary", "Frequency", "Is FNPF Exempt", "Is Tax Exempt", "Is Active",
            "Employment Type", "Branch", "Position", "Email"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        // Example/instruction row (row 2)
        worksheet.Row(2).Cell(1).Value = "Instructions";

        // Data row (row 3)
        worksheet.Cell(3, 1).Value = "Imported Employee";
        worksheet.Cell(3, 2).Value = "123456789";
        worksheet.Cell(3, 3).Value = "54321-B";
        worksheet.Cell(3, 4).Value = "Resident";
        worksheet.Cell(3, 5).Value = "Engineering";
        worksheet.Cell(3, 6).Value = 3000.00;
        worksheet.Cell(3, 7).Value = "Fortnightly";
        worksheet.Cell(3, 8).Value = "False";
        worksheet.Cell(3, 9).Value = "False";
        worksheet.Cell(3, 10).Value = "True";
        worksheet.Cell(3, 11).Value = "Permanent";
        worksheet.Cell(3, 12).Value = "Suva";
        worksheet.Cell(3, 13).Value = "Developer";
        worksheet.Cell(3, 14).Value = "imported@company.com";

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private Stream CreateInvalidEmployeeExcelStream()
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Template");

        string[] headers = {
            "Full Name", "TIN", "FNPF Number", "Residency Status", "Department",
            "Base Salary", "Frequency", "Is FNPF Exempt", "Is Tax Exempt", "Is Active",
            "Employment Type", "Branch", "Position", "Email"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        worksheet.Row(2).Cell(1).Value = "Instructions";

        // Invalid Data row (row 3)
        worksheet.Cell(3, 1).Value = ""; // Missing name
        worksheet.Cell(3, 2).Value = "123"; // Invalid TIN
        worksheet.Cell(3, 3).Value = "abc"; // Invalid FNPF
        worksheet.Cell(3, 4).Value = "Tourist"; // Invalid residency
        worksheet.Cell(3, 5).Value = "QA";
        worksheet.Cell(3, 6).Value = -100.00; // Invalid salary
        worksheet.Cell(3, 7).Value = "Daily"; // Invalid frequency
        worksheet.Cell(3, 8).Value = "False";
        worksheet.Cell(3, 9).Value = "False";
        worksheet.Cell(3, 10).Value = "True";
        worksheet.Cell(3, 11).Value = "Permanent";

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
