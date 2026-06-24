using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Seeders;
using FijiPayroll.Persistence.Converters;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Entities.Leave;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Entities.Audit;

namespace FijiPayroll.WPF.Services;

public sealed class SetupWizardService : ISetupWizardService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IApplicationStateStore _stateStore;
    private readonly ILogger<SetupWizardService> _logger;

    public SetupWizardService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IApplicationStateStore stateStore,
        ILogger<SetupWizardService> _logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _stateStore = stateStore;
        this._logger = _logger;
    }

    public async Task<bool> IsSetupCompletedAsync()
    {
        try
        {
            string? connStr = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connStr))
            {
                _logger.LogWarning("No connection string found in appsettings.json.");
                return false;
            }

            // Quick check if the database is online and tables exist
            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                
                // Let's check if the Companies table exists and if any company has IsSetupComplete = true
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM [company].[Companies] WHERE [IsSetupComplete] = 1", conn))
                {
                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Setup is incomplete or database is inaccessible.");
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed.");
            return false;
        }
    }

    public async Task SaveConnectionStringAsync(string connectionString)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string appSettingsPath = Path.Combine(baseDir, "appsettings.json");

        if (!File.Exists(appSettingsPath))
        {
            // If the file doesn't exist, we can construct a minimal template
            var minimal = new Dictionary<string, object>
            {
                ["ConnectionStrings"] = new Dictionary<string, string>
                {
                    ["DefaultConnection"] = connectionString
                },
                ["FileStorage"] = new Dictionary<string, string>
                {
                    ["RootDirectory"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fiji Payroll", "Exports")
                }
            };
            string newJson = JsonSerializer.Serialize(minimal, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(appSettingsPath, newJson);
            return;
        }

        try
        {
            string json = await File.ReadAllTextAsync(appSettingsPath);
            var options = new JsonSerializerOptions { WriteIndented = true };
            
            var document = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (document != null)
            {
                Dictionary<string, string> connStrings;
                if (document.TryGetValue("ConnectionStrings", out var val) && val is JsonElement elem)
                {
                    connStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(elem.GetRawText()) ?? new();
                }
                else
                {
                    connStrings = new();
                }
                connStrings["DefaultConnection"] = connectionString;
                document["ConnectionStrings"] = connStrings;
                
                string newJson = JsonSerializer.Serialize(document, options);
                await File.WriteAllTextAsync(appSettingsPath, newJson);
            }

            // Force reload of Configuration
            if (_configuration is IConfigurationRoot root)
            {
                root.Reload();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save connection string to appsettings.json.");
            throw;
        }
    }

    public async Task RunMigrationsAndSeedAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            _logger.LogInformation("Applying migrations...");
            await context.Database.MigrateAsync();
            _logger.LogInformation("Migrations applied successfully.");

            // Seed standard reference data required by the system
            _logger.LogInformation("Seeding system reference data...");
            
            var ruleModuleSeeder = scope.ServiceProvider.GetRequiredService<RuleModuleSeeder>();
            await ruleModuleSeeder.SeedAsync();

            var taxSeeder = scope.ServiceProvider.GetRequiredService<TaxBracketSeeder>();
            await taxSeeder.SeedAsync();

            var complianceSeeder = scope.ServiceProvider.GetRequiredService<ComplianceSeeder>();
            await complianceSeeder.SeedAsync();

            _logger.LogInformation("System reference data seeded successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database migrations or seeding failed.");
            throw;
        }
    }

    public async Task CompleteSetupAsync(SetupWizardData data)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var calendarGenerator = scope.ServiceProvider.GetRequiredService<IFiscalCalendarGenerator>();
        var payScheduleGenerator = scope.ServiceProvider.GetRequiredService<IPayScheduleGenerator>();

        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // 1. Create Company
            var company = Company.Create(
                legalName: data.Company.CompanyName,
                securityIsolatorKey: $"KEY_COMP_{Guid.NewGuid()}"
            );
            company.ConfigureCompanyDetails(
                tradingName: data.Company.TradingName,
                tin: data.Company.TIN,
                fnpfNumber: data.Company.FnpfNumber,
                addr1: data.Company.PhysicalAddress,
                addr2: data.Company.PostalAddress,
                city: data.Company.PhysicalAddress,
                phone: data.Company.ContactNumber,
                email: data.Company.EmailAddress,
                website: data.Company.Website
            );
            
            // Map voluntary deduction policy enum
            if (Enum.TryParse<NegativeNetPayPolicy>(data.Defaults.NegativeNetPayPolicy, true, out var policy))
            {
                company.SetNegativeNetPayPolicy(policy);
            }
            
            await context.Companies.AddAsync(company);
            await context.SaveChangesAsync();

            // Set tenant context for this transaction
            _stateStore.CurrentCompanyId = company.Id;
            TenantEncryptionValueConverter.CurrentKey = $"KEY_COMP_{company.Id}";

            // 2. Create SystemSettings
            var systemSettings = SystemSettings.Create(company.Id);
            systemSettings.DefaultPayFrequency = data.Defaults.DefaultPayFrequency;
            systemSettings.NegativePayPolicy = data.Defaults.NegativeNetPayPolicy;
            systemSettings.DefaultPayrollCalendar = $"Standard {data.Calendar.StartDate.Year}";
            
            // Map paths
            string commonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fiji Payroll");
            systemSettings.BackupDirectory = Path.Combine(commonPath, "Backups");
            systemSettings.ExportDirectory = Path.Combine(commonPath, "Exports");
            systemSettings.ImportDirectory = Path.Combine(commonPath, "Imports");
            systemSettings.DefaultSubmissionPaths = Path.Combine(commonPath, "Submissions");

            await context.SystemSettings.AddAsync(systemSettings);

            // 3. Create Fiscal Calendar
            var calType = Enum.TryParse<CalendarType>(data.Calendar.PayrollFrequency, true, out var ct) ? ct : CalendarType.Monthly;
            var calendar = await calendarGenerator.GenerateCalendarAsync(
                companyId: company.Id,
                fiscalYear: data.Calendar.StartDate.Year,
                startDate: data.Calendar.StartDate,
                calendarType: calType,
                generatedBy: data.Administrator.Username
            );
            await context.FiscalCalendars.AddAsync(calendar);
            await context.SaveChangesAsync(); // Generates IDs for calendar and periods

            // 4. Create Payroll Frequency Definition
            var freqType = Enum.TryParse<PayrollFrequencyType>(data.Calendar.PayrollFrequency, true, out var ft) ? ft : PayrollFrequencyType.Monthly;
            var freqCode = Enum.TryParse<FrequencyCode>(data.Calendar.PayrollFrequency, true, out var fc) ? fc : FrequencyCode.Monthly;
            int periodsPerYear = freqType switch
            {
                PayrollFrequencyType.Weekly => 52,
                PayrollFrequencyType.Fortnightly => 26,
                PayrollFrequencyType.BiMonthly => 24,
                _ => 12
            };

            var freqDef = PayrollFrequencyDefinition.Create(
                companyId: company.Id,
                name: $"{data.Calendar.PayrollFrequency} Pay Cycle",
                type: freqType,
                code: freqCode,
                payDay: freqType == PayrollFrequencyType.Monthly ? "25" : "Friday",
                periodsPerYear: periodsPerYear,
                description: $"Primary {data.Calendar.PayrollFrequency} payroll frequency"
            );
            await context.PayrollFrequencyDefinitions.AddAsync(freqDef);
            await context.SaveChangesAsync();

            // Generate pay schedules
            var schedules = await payScheduleGenerator.GenerateSchedulesAsync(company.Id, freqDef, calendar);
            foreach (var s in schedules)
            {
                freqDef.AssociateSchedule(s);
                await context.PayPeriodSchedules.AddAsync(s);
            }

            // 5. Seed Payroll Components
            if (data.UseDefaultComponents)
            {
                var components = new List<PayrollComponent>
                {
                    PayrollComponent.Create(company.Id, "BASIC", "Basic Salary", ComponentType.Earning, CalculationMethod.Manual, null, null, true, true, 10, "Base contract earnings", false),
                    PayrollComponent.Create(company.Id, "PAYE", "PAYE Tax Deduction", ComponentType.Statutory, CalculationMethod.Manual, null, null, false, false, 90, "Fiji Revenue and Customs Service PAYE Tax", true),
                    PayrollComponent.Create(company.Id, "FNPF_EE", "FNPF Employee Contribution", ComponentType.Statutory, CalculationMethod.Percentage, 8.00m, null, false, false, 91, "FNPF Employee Contribution (8%)", true),
                    PayrollComponent.Create(company.Id, "FNPF_ER", "FNPF Employer Contribution", ComponentType.Statutory, CalculationMethod.Percentage, 10.00m, null, false, false, 92, "FNPF Employer Contribution (10%)", true),
                    PayrollComponent.Create(company.Id, "OVERTIME", "Overtime Pay (1.5x)", ComponentType.Earning, CalculationMethod.Formula, null, "{HourlyRate} * {OvertimeHours} * 1.5", true, true, 20, "Standard Overtime pay multiplier", false),
                    PayrollComponent.Create(company.Id, "BONUS", "Bonus Pay", ComponentType.Earning, CalculationMethod.Manual, null, null, true, true, 30, "Performance or discretionary bonus", false),
                    PayrollComponent.Create(company.Id, "ALLOWANCE", "Allowance", ComponentType.Allowance, CalculationMethod.Fixed, 100.00m, null, false, true, 40, "General allowance support", false),
                    PayrollComponent.Create(company.Id, "LOAN_DED", "Loan Deduction", ComponentType.Deduction, CalculationMethod.Manual, null, null, false, false, 93, "Staff Loan Deduction Recovery", false),
                    PayrollComponent.Create(company.Id, "ADV_REC", "Advance Recovery", ComponentType.Deduction, CalculationMethod.Manual, null, null, false, false, 94, "Salary Advance recovery", false)
                };

                foreach (var comp in components)
                {
                    comp.CreatedBy = data.Administrator.Username;
                    comp.CreatedAt = DateTime.UtcNow;
                    await context.PayrollComponents.AddAsync(comp);
                }
            }

            // Seed Custom Components
            if (data.CustomComponents != null)
            {
                int order = 100;
                foreach (var cc in data.CustomComponents)
                {
                    var compType = Enum.TryParse<ComponentType>(cc.Type, true, out var t) ? t : ComponentType.Allowance;
                    var calcMethod = Enum.TryParse<CalculationMethod>(cc.CalculationMethod, true, out var m) ? m : CalculationMethod.Fixed;
                    
                    var comp = PayrollComponent.Create(
                        companyId: company.Id,
                        componentCode: cc.Code,
                        componentName: cc.Name,
                        componentType: compType,
                        calculationMethod: calcMethod,
                        calculationValue: (calcMethod == CalculationMethod.Fixed || calcMethod == CalculationMethod.Percentage) ? (cc.DefaultValue > 0 ? cc.DefaultValue : 1.0m) : null,
                        formula: null,
                        isTaxable: cc.IsTaxable,
                        isFnpfApplicable: cc.SubjectToFnpf,
                        displayOrder: order++,
                        description: "Custom component added during onboarding setup wizard",
                        isSystemComponent: false
                    );
                    
                    comp.CreatedBy = data.Administrator.Username;
                    comp.CreatedAt = DateTime.UtcNow;
                    await context.PayrollComponents.AddAsync(comp);
                }
            }

            // 6. Create Leave Policies
            foreach (var policyData in data.LeavePolicies)
            {
                var leaveCat = Enum.TryParse<LeaveCategory>(policyData.Category, true, out var lc) ? lc : LeaveCategory.Other;
                var leaveType = LeaveType.Create(
                    companyId: company.Id,
                    typeName: policyData.LeaveTypeName,
                    category: leaveCat,
                    entitlementDays: policyData.EntitlementDays,
                    isPaid: true,
                    applyLeaveLoading: policyData.LeaveTypeName.Contains("Annual"),
                    maxCarryOverDays: policyData.MaxCarryOverDays > 0 ? policyData.MaxCarryOverDays : null
                );
                await context.LeaveTypes.AddAsync(leaveType);
            }

            // 7. Seed Banks and Branches (standard seeder logic)
            var jsonSeedLoader = scope.ServiceProvider.GetRequiredService<IJsonSeedLoader>();
            await jsonSeedLoader.SeedBanksAsync(company.Id);

            // 8. Import Employees
            foreach (var empData in data.Employees)
            {
                var employee = FijiPayroll.Domain.Entities.Company.Employee.Create(
                    companyId: company.Id,
                    fullName: $"{empData.FirstName} {empData.LastName}",
                    tin: empData.TIN,
                    fnpfNumber: empData.Fnpf,
                    residencyStatus: "Resident",
                    department: empData.Department,
                    baseSalary: empData.PayRate,
                    frequency: freqType,
                    isFnpfExempt: false,
                    isTaxExempt: false,
                    isActive: true,
                    employmentType: EmploymentType.Permanent,
                    branch: "Main",
                    position: empData.Position
                );
                await context.Employees.AddAsync(employee);
            }

            // 9. Create Administrator User Account (BCrypt Hashed)
            string passwordHash = passwordHasher.Hash(data.Administrator.Password);
            var adminUser = UserAccount.Create(
                companyId: company.Id,
                username: data.Administrator.Username,
                passwordHash: passwordHash,
                displayName: data.Administrator.FullName,
                isSystemAdmin: true,
                mustChangePassword: false
            );
            adminUser.CreatedBy = "setup-wizard";
            adminUser.CreatedAt = DateTime.UtcNow;
            
            await context.UserAccounts.AddAsync(adminUser);
            await context.SaveChangesAsync();

            // Assign Admin Role and Permissions
            var adminRole = UserRole.Create(adminUser.Id, "PayrollAdministrator");
            await context.UserRoles.AddAsync(adminRole);
            await context.SaveChangesAsync();

            var permissionsList = new[]
            {
                "CompanyView", "CompanyEdit", "EmployeesView", "EmployeesCreate", "EmployeesEdit",
                "EmployeesDelete", "EmployeesTerminate", "EmployeesExport", "EmployeesImport",
                "PayrollView", "PayrollCreate", "PayrollCalculate", "PayrollApprove", "PayrollReverse",
                "PayrollExport", "PayrollRunsView", "PayrollRunsCreate", "PayrollRunsEdit",
                "PayrollRunsApprove", "PayrollRunsPost", "PayrollComponentsView", "PayrollComponentsCreate",
                "PayrollComponentsEdit", "PayrollComponentsDelete", "PayrollComponentsImport",
                "PayrollComponentsExport", "LeaveView", "LeaveCreate", "LeaveApprove", "LoansView",
                "LoansCreate", "LoansManage", "FrcsView", "FrcsGenerate", "FnpfGenerate",
                "BankFilesGenerate", "ReportsView", "ReportsExport", "SettingsUsers", "SettingsRoles",
                "SettingsConfig", "SettingsBackup", "AuditView"
            };

            foreach (var permCode in permissionsList)
            {
                var permission = UserPermission.Create(adminRole.Id, permCode);
                adminRole.AddPermission(permission);
            }

            adminUser.AddRole(adminRole);
            context.UserAccounts.Update(adminUser);

            // 10. Mark Wizard Setup State as Completed
            var wizardState = CompanySetupState.Create(company.Id);
            wizardState.CompleteSetup();
            await context.CompanySetupStates.AddAsync(wizardState);

            // Mark company as completed
            company.MarkSetupCompleted();
            context.Companies.Update(company);

            // 11. Create Audit Record
            var audit = CompanySetupAudit.Create(
                companyId: company.Id,
                step: "Completed",
                action: "CompleteSetupWizard",
                result: "Guided first-time setup wizard completed. Company tenant environment locked.",
                status: SetupAuditStatus.Success,
                ipAddress: "127.0.0.1",
                machineName: Environment.MachineName,
                appVersion: "1.0.0",
                correlationId: Guid.NewGuid(),
                executionId: Guid.NewGuid()
            );
            await context.CompanySetupAudits.AddAsync(audit);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Generate onboarding completion log
            WriteOnboardingCompletionLog(data, company.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed. Rolling back first-time setup wizard details.");
            await transaction.RollbackAsync();
            throw;
        }
    }

    private void WriteOnboardingCompletionLog(SetupWizardData data, int companyId)
    {
        try
        {
            string logsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fiji Payroll", "Logs");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }
            string logPath = Path.Combine(logsDir, "onboarding.log");
            string logText = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] ONBOARDING COMPLETED{Environment.NewLine}" +
                             $"Company ID: {companyId}{Environment.NewLine}" +
                             $"Company Name: {data.Company.CompanyName}{Environment.NewLine}" +
                             $"Admin User: {data.Administrator.Username}{Environment.NewLine}" +
                             $"Imported Employees: {data.Employees.Count}{Environment.NewLine}" +
                             $"Primary Frequency: {data.Calendar.PayrollFrequency}{Environment.NewLine}" +
                             $"========================================================{Environment.NewLine}";
            File.AppendAllText(logPath, logText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write onboarding log.");
        }
    }

    public async Task<List<SetupWizardEmployeeImport>> ParseEmployeesAsync(string filePath)
    {
        var list = new List<SetupWizardEmployeeImport>();
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Import file not found.", filePath);

        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext == ".xlsx")
        {
            await Task.Run(() =>
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) return;

                var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                if (rowCount < 2) return;

                // Simple header map defaults
                int empNoCol = 1, firstCol = 2, lastCol = 3, tinCol = 4, fnpfCol = 5, deptCol = 6, posCol = 7, rateCol = 8;
                var firstRow = worksheet.Row(1);
                int lastColUsed = worksheet.LastColumnUsed()?.ColumnNumber() ?? 10;
                for (int c = 1; c <= lastColUsed; c++)
                {
                    string header = firstRow.Cell(c).GetString()?.Trim().ToLowerInvariant() ?? "";
                    if (header.Contains("number") || header.Contains("emp no") || header.Contains("code")) empNoCol = c;
                    else if (header.Contains("first")) firstCol = c;
                    else if (header.Contains("last")) lastCol = c;
                    else if (header.Contains("tin")) tinCol = c;
                    else if (header.Contains("fnpf")) fnpfCol = c;
                    else if (header.Contains("dept") || header.Contains("department")) deptCol = c;
                    else if (header.Contains("pos") || header.Contains("position") || header.Contains("role")) posCol = c;
                    else if (header.Contains("rate") || header.Contains("salary") || header.Contains("pay")) rateCol = c;
                }

                for (int r = 2; r <= rowCount; r++)
                {
                    var row = worksheet.Row(r);
                    if (row.IsEmpty()) continue;

                    string empNo = row.Cell(empNoCol).GetString()?.Trim() ?? "";
                    string first = row.Cell(firstCol).GetString()?.Trim() ?? "";
                    string last = row.Cell(lastCol).GetString()?.Trim() ?? "";
                    string tin = row.Cell(tinCol).GetString()?.Trim() ?? "";
                    string fnpf = row.Cell(fnpfCol).GetString()?.Trim() ?? "";
                    string dept = row.Cell(deptCol).GetString()?.Trim() ?? "";
                    string pos = row.Cell(posCol).GetString()?.Trim() ?? "";
                    string rateStr = row.Cell(rateCol).GetString()?.Trim() ?? "0";

                    decimal.TryParse(rateStr, out decimal rate);

                    if (!string.IsNullOrEmpty(first) || !string.IsNullOrEmpty(last))
                    {
                        list.Add(new SetupWizardEmployeeImport
                        {
                            EmployeeNumber = empNo,
                            FirstName = first,
                            LastName = last,
                            TIN = tin,
                            Fnpf = fnpf,
                            Department = dept,
                            Position = pos,
                            PayRate = rate
                        });
                    }
                }
            });
        }
        else // Fallback to CSV
        {
            using var reader = new StreamReader(filePath);
            string? headerLine = await reader.ReadLineAsync();
            if (headerLine != null)
            {
                int empNoCol = 0, firstCol = 1, lastCol = 2, tinCol = 3, fnpfCol = 4, deptCol = 5, posCol = 6, rateCol = 7;
                string[] headers = headerLine.Split(',');
                for (int i = 0; i < headers.Length; i++)
                {
                    string header = headers[i].Trim().ToLowerInvariant();
                    if (header.Contains("number") || header.Contains("emp no") || header.Contains("code")) empNoCol = i;
                    else if (header.Contains("first")) firstCol = i;
                    else if (header.Contains("last")) lastCol = i;
                    else if (header.Contains("tin")) tinCol = i;
                    else if (header.Contains("fnpf")) fnpfCol = i;
                    else if (header.Contains("dept") || header.Contains("department")) deptCol = i;
                    else if (header.Contains("pos") || header.Contains("position") || header.Contains("role")) posCol = i;
                    else if (header.Contains("rate") || header.Contains("salary") || header.Contains("pay")) rateCol = i;
                }

                while (!reader.EndOfStream)
                {
                    string? line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Split(',');
                    string GetPart(int idx) => idx < parts.Length ? parts[idx].Trim() : "";

                    string empNo = GetPart(empNoCol);
                    string first = GetPart(firstCol);
                    string last = GetPart(lastCol);
                    string tin = GetPart(tinCol);
                    string fnpf = GetPart(fnpfCol);
                    string dept = GetPart(deptCol);
                    string pos = GetPart(posCol);
                    string rateStr = GetPart(rateCol);

                    decimal.TryParse(rateStr, out decimal rate);

                    if (!string.IsNullOrEmpty(first) || !string.IsNullOrEmpty(last))
                    {
                        list.Add(new SetupWizardEmployeeImport
                        {
                            EmployeeNumber = empNo,
                            FirstName = first,
                            LastName = last,
                            TIN = tin,
                            Fnpf = fnpf,
                            Department = dept,
                            Position = pos,
                            PayRate = rate
                        });
                    }
                }
            }
        }

        return list;
    }

    public async Task GenerateEmployeeImportTemplateAsync(string filePath)
    {
        await Task.Run(() =>
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Employees Template");
            
            // Set Headers
            worksheet.Cell(1, 1).Value = "Employee Number";
            worksheet.Cell(1, 2).Value = "First Name";
            worksheet.Cell(1, 3).Value = "Last Name";
            worksheet.Cell(1, 4).Value = "TIN";
            worksheet.Cell(1, 5).Value = "FNPF";
            worksheet.Cell(1, 6).Value = "Department";
            worksheet.Cell(1, 7).Value = "Position";
            worksheet.Cell(1, 8).Value = "Pay Rate";

            // Format headers
            var headerRow = worksheet.Row(1);
            headerRow.Height = 24;
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
            headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1B2032");
            headerRow.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            headerRow.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;

            // Add sample data row as placeholder
            worksheet.Cell(2, 1).Value = "EMP001";
            worksheet.Cell(2, 2).Value = "John";
            worksheet.Cell(2, 3).Value = "Doe";
            worksheet.Cell(2, 4).Value = "123456789";
            worksheet.Cell(2, 5).Value = "987654";
            worksheet.Cell(2, 6).Value = "Finance";
            worksheet.Cell(2, 7).Value = "Accountant";
            worksheet.Cell(2, 8).Value = 2500.00;

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        });
    }
}
