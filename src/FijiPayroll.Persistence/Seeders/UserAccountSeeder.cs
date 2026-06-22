using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Seeders;

/// <summary>
/// Idempotent database seeder for admin users and permissions mapping.
/// </summary>
public sealed class UserAccountSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJsonSeedLoader _jsonSeedLoader;

    public UserAccountSeeder(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJsonSeedLoader jsonSeedLoader)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jsonSeedLoader = jsonSeedLoader;
    }

    /// <summary>
    /// Seeds default company if none exists, then seeds default admin user with PayrollAdministrator role and all permissions.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // First, ensure there is at least one company
        if (!await _context.Companies.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            var defaultCompany = Company.Create(
                legalName: "Fiji Test Company Ltd",
                securityIsolatorKey: "DEFAULT_SEC_ISOLATOR_KEY_123"
            );
            defaultCompany.TradingName = "Fiji Test Company";
            defaultCompany.TIN = "123456789";
            defaultCompany.FnpfEmployerNumber = "FNPF12345";
            defaultCompany.CreatedBy = "system-seeder";
            defaultCompany.CreatedAt = DateTime.UtcNow;

            await _context.Companies.AddAsync(defaultCompany, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Get all companies (ignoring RLS query filters so we can seed all of them)
        var companies = await _context.Companies.IgnoreQueryFilters().ToListAsync(cancellationToken);

        var permissionsList = new[]
        {
            PermissionConstants.CompanyView,
            PermissionConstants.CompanyEdit,
            PermissionConstants.EmployeesView,
            PermissionConstants.EmployeesCreate,
            PermissionConstants.EmployeesEdit,
            PermissionConstants.EmployeesDelete,
            PermissionConstants.EmployeesTerminate,
            PermissionConstants.EmployeesExport,
            PermissionConstants.EmployeesImport,
            PermissionConstants.PayrollView,
            PermissionConstants.PayrollCreate,
            PermissionConstants.PayrollCalculate,
            PermissionConstants.PayrollApprove,
            PermissionConstants.PayrollReverse,
            PermissionConstants.PayrollExport,
            PermissionConstants.PayrollRunsView,
            PermissionConstants.PayrollRunsCreate,
            PermissionConstants.PayrollRunsEdit,
            PermissionConstants.PayrollRunsApprove,
            PermissionConstants.PayrollRunsPost,
            PermissionConstants.PayrollComponentsView,
            PermissionConstants.PayrollComponentsCreate,
            PermissionConstants.PayrollComponentsEdit,
            PermissionConstants.PayrollComponentsDelete,
            PermissionConstants.PayrollComponentsImport,
            PermissionConstants.PayrollComponentsExport,
            PermissionConstants.LeaveView,
            PermissionConstants.LeaveCreate,
            PermissionConstants.LeaveApprove,
            PermissionConstants.LoansView,
            PermissionConstants.LoansCreate,
            PermissionConstants.LoansManage,
            PermissionConstants.FrcsView,
            PermissionConstants.FrcsGenerate,
            PermissionConstants.FnpfGenerate,
            PermissionConstants.BankFilesGenerate,
            PermissionConstants.ReportsView,
            PermissionConstants.ReportsExport,
            PermissionConstants.SettingsUsers,
            PermissionConstants.SettingsRoles,
            PermissionConstants.SettingsConfig,
            PermissionConstants.SettingsBackup,
            PermissionConstants.AuditView
        };

        foreach (var company in companies)
        {
            // Seed reference bank data
            await _jsonSeedLoader.SeedBanksAsync(company.Id, cancellationToken);

            // Ensure fallback bank account exists
            var bankAccountExists = await _context.CompanyBankAccounts
                .IgnoreQueryFilters()
                .AnyAsync(b => b.CompanyId == company.Id, cancellationToken);

            if (!bankAccountExists)
            {
                var branch = await _context.BankBranches
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(b => b.CompanyId == company.Id, cancellationToken);

                if (branch != null)
                {
                    var fallbackAccount = CompanyBankAccount.Create(
                        companyId: company.Id,
                        accountName: "Operating Account",
                        bankMasterId: branch.BankMasterId,
                        bankBranchId: branch.Id,
                        accountType: BankAccountType.Operating,
                        encryptedAccountNumber: "PLAINTEXT:v1:none:MTIzNDU2Nzg5", // "123456789" in base64
                        accountNumberHash: "123456789".GetHashCode().ToString(),
                        last4Digits: "6789",
                        isActive: true
                    );
                    await _context.CompanyBankAccounts.AddAsync(fallbackAccount, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            var adminUserExists = await _context.UserAccounts
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Username == "admin" && u.CompanyId == company.Id, cancellationToken);

            if (!adminUserExists)
            {
                var passwordHash = _passwordHasher.Hash("Admin@1234");
                var adminUser = UserAccount.Create(
                    companyId: company.Id,
                    username: "admin",
                    passwordHash: passwordHash,
                    displayName: "System Administrator",
                    isSystemAdmin: true,
                    mustChangePassword: true
                );

                adminUser.CreatedBy = "system-seeder";
                adminUser.CreatedAt = DateTime.UtcNow;

                await _context.UserAccounts.AddAsync(adminUser, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken); // Save to get the UserAccount ID

                // Create the role
                var adminRole = UserRole.Create(adminUser.Id, "PayrollAdministrator");

                await _context.UserRoles.AddAsync(adminRole, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken); // Save to get Role ID

                // Assign all permissions to this role
                foreach (var permCode in permissionsList)
                {
                    var permission = UserPermission.Create(adminRole.Id, permCode);
                    adminRole.AddPermission(permission);
                }

                adminUser.AddRole(adminRole);
                _context.UserAccounts.Update(adminUser);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
