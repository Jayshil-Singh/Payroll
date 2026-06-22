using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Shared.Constants;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Establishes a minimal authenticated session for the desktop shell at startup.
/// </summary>
public static class AuthSessionBootstrap
{
    private static readonly string[] DefaultRoles = ["PayrollAdministrator"];

    private static readonly string[] DefaultPermissions =
    [
        PermissionConstants.CompanyView,
        PermissionConstants.CompanyEdit,
        PermissionConstants.EmployeesView,
        PermissionConstants.EmployeesCreate,
        PermissionConstants.EmployeesEdit,
        PermissionConstants.EmployeesDelete,
        PermissionConstants.EmployeesImport,
        PermissionConstants.EmployeesExport,
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
        PermissionConstants.FrcsView,
        PermissionConstants.FrcsGenerate,
        PermissionConstants.FnpfGenerate,
        PermissionConstants.BankFilesGenerate,
    ];

    public static void Initialize(IAuthSessionStore sessionStore, IApplicationStateStore stateStore)
    {
        int companyId = stateStore.CurrentCompanyId;
        if (companyId <= 0)
        {
            throw new InvalidOperationException(
                "Cannot bootstrap authentication without a valid company context. Set CurrentCompanyId before establishing session.");
        }

        sessionStore.Establish(new AuthenticatedSession
        {
            UserId = 1,
            Username = "payroll.admin@local",
            CompanyIds = new List<int> { companyId },
            Roles = DefaultRoles,
            Permissions = DefaultPermissions
        });
    }
}
