using ClosedXML.Excel;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Infrastructure.Services;

/// <summary>
/// Implementation of IImportEngine providing Excel template generation, import parsing, validation,
/// and atomic commit operations using ClosedXML.
/// </summary>
public sealed class ImportEngine : IImportEngine
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IReferenceDataCache _cache;

    /// <summary>Initializes the import engine.</summary>
    public ImportEngine(
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUserService,
        IReferenceDataCache cache)
    {
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _currentUserService = currentUserService;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task GenerateTemplateAsync(Stream outputStream, string moduleName, CancellationToken cancellationToken = default)
    {
        if (outputStream == null) throw new ArgumentNullException(nameof(outputStream));

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Template");

        if (string.Equals(moduleName, "Employees", StringComparison.OrdinalIgnoreCase))
        {
            string[] headers = {
                "Full Name", "TIN", "FNPF Number", "Residency Status", "Department",
                "Base Salary", "Frequency", "Is FNPF Exempt", "Is Tax Exempt", "Is Active",
                "Employment Type", "Branch", "Position", "Email"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
            }

            // Instructions Row
            worksheet.Cell(2, 1).Value = "Example: John Doe";
            worksheet.Cell(2, 2).Value = "998877665";
            worksheet.Cell(2, 3).Value = "12345-F";
            worksheet.Cell(2, 4).Value = "Resident";
            worksheet.Cell(2, 5).Value = "Finance";
            worksheet.Cell(2, 6).Value = 2500.00;
            worksheet.Cell(2, 7).Value = "Fortnightly";
            worksheet.Cell(2, 8).Value = "False";
            worksheet.Cell(2, 9).Value = "False";
            worksheet.Cell(2, 10).Value = "True";
            worksheet.Cell(2, 11).Value = "Permanent";
            worksheet.Cell(2, 12).Value = "Suva";
            worksheet.Cell(2, 13).Value = "Accountant";
            worksheet.Cell(2, 14).Value = "john.doe@company.com";
            worksheet.Row(2).Style.Font.Italic = true;
        }
        else if (string.Equals(moduleName, "Lookups", StringComparison.OrdinalIgnoreCase))
        {
            string[] headers = {
                "Category", "Code", "Name", "Effective From", "Effective To", "Parent Code", "Display Order", "Is Active"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
            }

            // Instructions Row
            worksheet.Cell(2, 1).Value = "Branch";
            worksheet.Cell(2, 2).Value = "SUV";
            worksheet.Cell(2, 3).Value = "Suva Branch";
            worksheet.Cell(2, 4).Value = DateTime.UtcNow.ToString("yyyy-MM-dd");
            worksheet.Cell(2, 5).Value = "";
            worksheet.Cell(2, 6).Value = "";
            worksheet.Cell(2, 7).Value = 1;
            worksheet.Cell(2, 8).Value = "True";
            worksheet.Row(2).Style.Font.Italic = true;
        }
        else
        {
            throw new ArgumentException($"Unsupported module: {moduleName}", nameof(moduleName));
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(outputStream);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<ImportValidationResult> ValidateImportAsync(Stream inputStream, string moduleName, CancellationToken cancellationToken = default)
    {
        if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));

        var companyId = _tenantProvider.GetCurrentCompanyId();
        var jobId = Guid.NewGuid();
        var errors = new List<ImportError>();
        var processedCount = 0;
        var successCount = 0;
        var failureCount = 0;

        using var workbook = new XLWorkbook(inputStream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet is null)
        {
            return new ImportValidationResult(false, jobId, 0, 0, 0, new[] { new ImportError(0, "Workbook", "No worksheets found in the Excel file.") });
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var validEmployees = new List<ParsedEmployee>();
        var validLookups = new List<ParsedLookup>();

        if (string.Equals(moduleName, "Employees", StringComparison.OrdinalIgnoreCase))
        {
            var expectedHeaders = new[] {
                "Full Name", "TIN", "FNPF Number", "Residency Status", "Department",
                "Base Salary", "Frequency", "Is FNPF Exempt", "Is Tax Exempt", "Is Active",
                "Employment Type", "Branch", "Position", "Email"
            };

            for (int col = 1; col <= expectedHeaders.Length; col++)
            {
                var headerVal = worksheet.Cell(1, col).GetString()?.Trim();
                if (!string.Equals(headerVal, expectedHeaders[col - 1], StringComparison.OrdinalIgnoreCase))
                {
                    return new ImportValidationResult(false, jobId, 0, 0, 0, new[] {
                        new ImportError(1, worksheet.Cell(1, col).Address.ToString() ?? "", $"Invalid header column. Expected '{expectedHeaders[col - 1]}' but found '{headerVal}'.")
                    });
                }
            }

            for (int r = 3; r <= lastRow; r++)
            {
                var row = worksheet.Row(r);
                if (row.IsEmpty()) continue;

                processedCount++;
                var rowErrors = new List<string>();

                var fullName = row.Cell(1).GetString()?.Trim() ?? string.Empty;
                var tin = row.Cell(2).GetString()?.Trim() ?? string.Empty;
                var fnpfNumber = row.Cell(3).GetString()?.Trim() ?? string.Empty;
                var residencyStatus = row.Cell(4).GetString()?.Trim() ?? string.Empty;
                var department = row.Cell(5).GetString()?.Trim() ?? string.Empty;
                var baseSalaryStr = row.Cell(6).GetString()?.Trim() ?? string.Empty;
                var frequency = row.Cell(7).GetString()?.Trim() ?? string.Empty;
                var isFnpfExemptStr = row.Cell(8).GetString()?.Trim() ?? string.Empty;
                var isTaxExemptStr = row.Cell(9).GetString()?.Trim() ?? string.Empty;
                var isActiveStr = row.Cell(10).GetString()?.Trim() ?? string.Empty;
                var employmentType = row.Cell(11).GetString()?.Trim() ?? string.Empty;
                var branch = row.Cell(12).GetString()?.Trim() ?? string.Empty;
                var position = row.Cell(13).GetString()?.Trim() ?? string.Empty;
                var email = row.Cell(14).GetString()?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(fullName)) rowErrors.Add("Full Name is required.");
                if (string.IsNullOrWhiteSpace(tin))
                {
                    rowErrors.Add("TIN is required.");
                }
                else if (!Regex.IsMatch(tin, @"^\d{9}$"))
                {
                    rowErrors.Add("TIN must be exactly 9 digits.");
                }

                bool isFnpfExempt = false;
                if (!string.IsNullOrWhiteSpace(isFnpfExemptStr))
                {
                    _ = bool.TryParse(isFnpfExemptStr, out isFnpfExempt);
                }

                if (!isFnpfExempt)
                {
                    if (string.IsNullOrWhiteSpace(fnpfNumber))
                    {
                        rowErrors.Add("FNPF Number is required when employee is not exempt from FNPF.");
                    }
                    else if (!Regex.IsMatch(fnpfNumber, @"^\d{5}-[A-Za-z]$"))
                    {
                        rowErrors.Add("FNPF Number must be in the format '12345-F'.");
                    }
                }

                if (string.IsNullOrWhiteSpace(residencyStatus))
                {
                    rowErrors.Add("Residency Status is required.");
                }
                else if (!string.Equals(residencyStatus, "Resident", StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(residencyStatus, "NonResident", StringComparison.OrdinalIgnoreCase))
                {
                    rowErrors.Add("Residency Status must be 'Resident' or 'NonResident'.");
                }

                if (string.IsNullOrWhiteSpace(department)) rowErrors.Add("Department is required.");

                decimal baseSalary = 0;
                if (string.IsNullOrWhiteSpace(baseSalaryStr) || !decimal.TryParse(baseSalaryStr, out baseSalary) || baseSalary <= 0)
                {
                    rowErrors.Add("Base Salary must be a positive decimal number.");
                }

                if (string.IsNullOrWhiteSpace(frequency))
                {
                    rowErrors.Add("Frequency is required.");
                }
                else if (!string.Equals(frequency, "Weekly", StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(frequency, "Fortnightly", StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(frequency, "Monthly", StringComparison.OrdinalIgnoreCase))
                {
                    rowErrors.Add("Frequency must be 'Weekly', 'Fortnightly', or 'Monthly'.");
                }

                if (string.IsNullOrWhiteSpace(employmentType))
                {
                    rowErrors.Add("Employment Type is required.");
                }
                else if (!string.Equals(employmentType, "Permanent", StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(employmentType, "Contract", StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(employmentType, "Casual", StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(employmentType, "PartTime", StringComparison.OrdinalIgnoreCase))
                {
                    rowErrors.Add("Employment Type must be 'Permanent', 'Contract', 'Casual', or 'PartTime'.");
                }

                if (!string.IsNullOrWhiteSpace(email) && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    rowErrors.Add("Invalid Email format.");
                }

                bool isTaxExempt = false;
                if (!string.IsNullOrWhiteSpace(isTaxExemptStr))
                {
                    _ = bool.TryParse(isTaxExemptStr, out isTaxExempt);
                }

                bool isActive = true;
                if (!string.IsNullOrWhiteSpace(isActiveStr))
                {
                    _ = bool.TryParse(isActiveStr, out isActive);
                }

                if (rowErrors.Any())
                {
                    failureCount++;
                    foreach (var err in rowErrors)
                    {
                        errors.Add(new ImportError(r, "", err));
                    }
                }
                else
                {
                    successCount++;
                    var normalizedResidency = string.Equals(residencyStatus, "Resident", StringComparison.OrdinalIgnoreCase) ? "Resident" : "NonResident";
                    var normalizedFrequency = string.Equals(frequency, "Weekly", StringComparison.OrdinalIgnoreCase) ? "Weekly" :
                                               string.Equals(frequency, "Fortnightly", StringComparison.OrdinalIgnoreCase) ? "Fortnightly" : "Monthly";
                    var normalizedEmpType = string.Equals(employmentType, "Permanent", StringComparison.OrdinalIgnoreCase) ? "Permanent" :
                                            string.Equals(employmentType, "Contract", StringComparison.OrdinalIgnoreCase) ? "Contract" :
                                            string.Equals(employmentType, "Casual", StringComparison.OrdinalIgnoreCase) ? "Casual" : "PartTime";

                    validEmployees.Add(new ParsedEmployee
                    {
                        FullName = fullName,
                        Tin = tin,
                        FnpfNumber = fnpfNumber.ToUpperInvariant(),
                        ResidencyStatus = normalizedResidency,
                        Department = department,
                        BaseSalary = baseSalary,
                        Frequency = normalizedFrequency,
                        IsFnpfExempt = isFnpfExempt,
                        IsTaxExempt = isTaxExempt,
                        IsActive = isActive,
                        EmploymentType = normalizedEmpType,
                        Branch = branch,
                        Position = position,
                        Email = email
                    });
                }
            }
        }
        else if (string.Equals(moduleName, "Lookups", StringComparison.OrdinalIgnoreCase))
        {
            var expectedHeaders = new[] {
                "Category", "Code", "Name", "Effective From", "Effective To", "Parent Code", "Display Order", "Is Active"
            };

            for (int col = 1; col <= expectedHeaders.Length; col++)
            {
                var headerVal = worksheet.Cell(1, col).GetString()?.Trim();
                if (!string.Equals(headerVal, expectedHeaders[col - 1], StringComparison.OrdinalIgnoreCase))
                {
                    return new ImportValidationResult(false, jobId, 0, 0, 0, new[] {
                        new ImportError(1, worksheet.Cell(1, col).Address.ToString() ?? "", $"Invalid header column. Expected '{expectedHeaders[col - 1]}' but found '{headerVal}'.")
                    });
                }
            }

            for (int r = 3; r <= lastRow; r++)
            {
                var row = worksheet.Row(r);
                if (row.IsEmpty()) continue;

                processedCount++;
                var rowErrors = new List<string>();

                var category = row.Cell(1).GetString()?.Trim() ?? string.Empty;
                var code = row.Cell(2).GetString()?.Trim() ?? string.Empty;
                var name = row.Cell(3).GetString()?.Trim() ?? string.Empty;
                var effectiveFromStr = row.Cell(4).GetString()?.Trim() ?? string.Empty;
                var effectiveToStr = row.Cell(5).GetString()?.Trim() ?? string.Empty;
                var parentCode = row.Cell(6).GetString()?.Trim() ?? string.Empty;
                var displayOrderStr = row.Cell(7).GetString()?.Trim() ?? string.Empty;
                var isActiveStr = row.Cell(8).GetString()?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(category)) rowErrors.Add("Category is required.");
                if (string.IsNullOrWhiteSpace(code)) rowErrors.Add("Code is required.");
                if (string.IsNullOrWhiteSpace(name)) rowErrors.Add("Name is required.");

                DateTime effectiveFrom = DateTime.MinValue;
                if (string.IsNullOrWhiteSpace(effectiveFromStr) || !DateTime.TryParse(effectiveFromStr, out effectiveFrom))
                {
                    rowErrors.Add("Effective From must be a valid DateTime.");
                }

                DateTime effectiveTo = DateTime.MaxValue;
                if (!string.IsNullOrWhiteSpace(effectiveToStr))
                {
                    if (DateTime.TryParse(effectiveToStr, out var toVal))
                    {
                        effectiveTo = toVal;
                    }
                    else
                    {
                        rowErrors.Add("Effective To must be a valid DateTime if provided.");
                    }
                }

                int displayOrder = 0;
                if (!string.IsNullOrWhiteSpace(displayOrderStr) && (!int.TryParse(displayOrderStr, out displayOrder) || displayOrder < 0))
                {
                    rowErrors.Add("Display Order must be a non-negative integer.");
                }

                bool isActive = true;
                if (!string.IsNullOrWhiteSpace(isActiveStr))
                {
                    _ = bool.TryParse(isActiveStr, out isActive);
                }

                if (rowErrors.Any())
                {
                    failureCount++;
                    foreach (var err in rowErrors)
                    {
                        errors.Add(new ImportError(r, "", err));
                    }
                }
                else
                {
                    successCount++;
                    validLookups.Add(new ParsedLookup
                    {
                        Category = category,
                        Code = code.ToUpperInvariant(),
                        Name = name,
                        EffectiveFrom = effectiveFrom,
                        EffectiveTo = effectiveTo,
                        ParentCode = string.IsNullOrWhiteSpace(parentCode) ? null : parentCode.ToUpperInvariant(),
                        DisplayOrder = displayOrder,
                        IsActive = isActive
                    });
                }
            }
        }
        else
        {
            throw new ArgumentException($"Unsupported module: {moduleName}", nameof(moduleName));
        }

        bool isValid = failureCount == 0 && processedCount > 0;
        var payload = "";
        if (isValid)
        {
            payload = string.Equals(moduleName, "Employees", StringComparison.OrdinalIgnoreCase)
                ? JsonSerializer.Serialize(validEmployees)
                : JsonSerializer.Serialize(validLookups);
        }

        var status = isValid ? "Pending" : "Failed";
        var errorMsg = errors.Any() ? string.Join("; ", errors.Select(e => $"Row {e.Row}: {e.ErrorMessage}")) : null;

        var importJob = ImportJob.Create(
            jobId: jobId,
            companyId: companyId,
            moduleName: moduleName,
            fileName: "UploadedFile",
            processedCount: processedCount,
            successCount: successCount,
            failureCount: failureCount,
            payload: payload,
            status: status,
            errorMessage: errorMsg
        );

        importJob.CreatedBy = _currentUserService.Username;
        importJob.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.ImportJobs.AddAsync(importJob, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImportValidationResult(isValid, jobId, processedCount, successCount, failureCount, errors);
    }

    /// <inheritdoc />
    public async Task CommitImportAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _unitOfWork.ImportJobs.GetByJobIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            throw new InvalidOperationException($"Import job with ID {jobId} was not found.");
        }

        if (job.Status != "Pending")
        {
            throw new InvalidOperationException($"Import job is in '{job.Status}' status and cannot be committed.");
        }

        if (string.Equals(job.ModuleName, "Employees", StringComparison.OrdinalIgnoreCase))
        {
            var employeesToImport = JsonSerializer.Deserialize<List<ParsedEmployee>>(job.Payload);
            if (employeesToImport is not null)
            {
                foreach (var emp in employeesToImport)
                {
                    var residency = emp.ResidencyStatus;
                    var freq = Enum.Parse<PayrollFrequencyType>(emp.Frequency, true);
                    var empType = Enum.Parse<EmploymentType>(emp.EmploymentType, true);

                    var employee = Employee.Create(
                        companyId: job.CompanyId,
                        fullName: emp.FullName,
                        tin: emp.Tin,
                        fnpfNumber: emp.FnpfNumber,
                        residencyStatus: residency,
                        department: emp.Department,
                        baseSalary: emp.BaseSalary,
                        frequency: freq,
                        isFnpfExempt: emp.IsFnpfExempt,
                        isTaxExempt: emp.IsTaxExempt,
                        isActive: emp.IsActive,
                        employmentType: empType,
                        branch: emp.Branch,
                        position: emp.Position,
                        email: emp.Email
                    );

                    employee.AddPaymentMethod(EmployeePaymentMethod.Create(
                        methodType: PaymentMethodType.Cash,
                        percentage: 100m,
                        isPrimary: true
                    ));

                    employee.CreatedBy = job.CreatedBy;
                    employee.CreatedAt = DateTime.UtcNow;

                    await _unitOfWork.Employees.AddAsync(employee, cancellationToken);
                }
            }
        }
        else if (string.Equals(job.ModuleName, "Lookups", StringComparison.OrdinalIgnoreCase))
        {
            var lookupsToImport = JsonSerializer.Deserialize<List<ParsedLookup>>(job.Payload);
            if (lookupsToImport is not null)
            {
                foreach (var lk in lookupsToImport)
                {
                    int? parentId = null;
                    if (!string.IsNullOrWhiteSpace(lk.ParentCode))
                    {
                        var existingLookups = await _unitOfWork.MasterLookups.GetByCategoryAsync(job.CompanyId, lk.Category, cancellationToken);
                        var parentLookup = existingLookups.FirstOrDefault(x => string.Equals(x.Code, lk.ParentCode, StringComparison.OrdinalIgnoreCase));
                        if (parentLookup is not null)
                        {
                            parentId = parentLookup.Id;
                        }
                    }

                    var lookup = MasterLookup.Create(
                        companyId: job.CompanyId,
                        category: lk.Category,
                        code: lk.Code,
                        name: lk.Name,
                        effectiveFrom: lk.EffectiveFrom,
                        effectiveTo: lk.EffectiveTo,
                        parentId: parentId,
                        displayOrder: lk.DisplayOrder,
                        isActive: lk.IsActive
                    );

                    lookup.CreatedBy = job.CreatedBy;
                    lookup.CreatedAt = DateTime.UtcNow;

                    await _unitOfWork.MasterLookups.AddAsync(lookup, cancellationToken);
                }

                var categories = lookupsToImport.Select(x => x.Category).Distinct();
                foreach (var cat in categories)
                {
                    _cache.InvalidateCategory(cat);
                }
            }
        }

        job.Complete();
        job.ModifiedBy = _currentUserService.Username;
        job.ModifiedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private sealed class ParsedEmployee
    {
        public string FullName { get; set; } = string.Empty;
        public string Tin { get; set; } = string.Empty;
        public string FnpfNumber { get; set; } = string.Empty;
        public string ResidencyStatus { get; set; } = "Resident";
        public string Department { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public string Frequency { get; set; } = "Fortnightly";
        public bool IsFnpfExempt { get; set; }
        public bool IsTaxExempt { get; set; }
        public bool IsActive { get; set; } = true;
        public string EmploymentType { get; set; } = "Permanent";
        public string Branch { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private sealed class ParsedLookup
    {
        public string Category { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; } = DateTime.MaxValue;
        public string? ParentCode { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
