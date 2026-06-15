namespace FijiPayroll.Shared.Constants;

/// <summary>
/// Fine-grained permission codes used throughout the RBAC system.
/// Format: [Module].[Action]
/// </summary>
public static class PermissionConstants
{
    // ─── Company ────────────────────────────────────────────────────────────────
    public const string CompanyView   = "Company.View";
    public const string CompanyEdit   = "Company.Edit";

    // ─── Employees ──────────────────────────────────────────────────────────────
    public const string EmployeesView      = "Employees.View";
    public const string EmployeesCreate    = "Employees.Create";
    public const string EmployeesEdit      = "Employees.Edit";
    public const string EmployeesDelete    = "Employees.Delete";
    public const string EmployeesTerminate = "Employees.Terminate";
    public const string EmployeesExport    = "Employees.Export";
    public const string EmployeesImport    = "Employees.Import";

    // ─── Payroll ────────────────────────────────────────────────────────────────
    public const string PayrollView      = "Payroll.View";
    public const string PayrollCreate    = "Payroll.Create";
    public const string PayrollCalculate = "Payroll.Calculate";
    public const string PayrollApprove   = "Payroll.Approve";
    public const string PayrollReverse   = "Payroll.Reverse";
    public const string PayrollExport    = "Payroll.Export";

    // ─── Payroll Components ─────────────────────────────────────────────────────
    public const string PayrollComponentsView   = "PayrollComponents.View";
    public const string PayrollComponentsCreate = "PayrollComponents.Create";
    public const string PayrollComponentsEdit   = "PayrollComponents.Edit";
    public const string PayrollComponentsDelete = "PayrollComponents.Delete";
    public const string PayrollComponentsImport = "PayrollComponents.Import";
    public const string PayrollComponentsExport = "PayrollComponents.Export";

    // ─── Leave ──────────────────────────────────────────────────────────────────
    public const string LeaveView    = "Leave.View";
    public const string LeaveCreate  = "Leave.Create";
    public const string LeaveApprove = "Leave.Approve";

    // ─── Compliance ─────────────────────────────────────────────────────────────
    public const string FrcsView     = "FRCS.View";
    public const string FrcsGenerate = "FRCS.Generate";
    public const string FnpfGenerate = "FNPF.Generate";
    public const string BankFilesGenerate = "BankFiles.Generate";

    // ─── Reports ────────────────────────────────────────────────────────────────
    public const string ReportsView   = "Reports.View";
    public const string ReportsExport = "Reports.Export";

    // ─── Settings / Configuration ────────────────────────────────────────────────
    public const string SettingsUsers  = "Settings.Users";
    public const string SettingsRoles  = "Settings.Roles";
    public const string SettingsConfig = "Settings.Config";
    public const string SettingsBackup = "Settings.Backup";

    // ─── Audit ──────────────────────────────────────────────────────────────────
    public const string AuditView = "Audit.View";
}
