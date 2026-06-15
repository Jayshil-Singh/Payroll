namespace FijiPayroll.Shared.Constants;

/// <summary>
/// Fiji-specific tax constants sourced from FRCS regulations.
/// These values must be reviewed and updated at the start of each fiscal year.
/// </summary>
public static class FijiTaxConstants
{
    // ─── FNPF Contribution Rates ────────────────────────────────────────────────

    /// <summary>FNPF employee contribution rate as a percentage (8%).</summary>
    public const decimal FnpfEmployeeRatePercent = 8.0m;

    /// <summary>FNPF employer contribution rate as a percentage (10%).</summary>
    public const decimal FnpfEmployerRatePercent = 10.0m;

    // ─── PAYE Tax Brackets for Residents (FY 2025–2026) ────────────────────────
    // Source: FRCS Employer Guide – updated each fiscal year via migration

    /// <summary>Upper bound of the 0% PAYE tax bracket for residents (FJD).</summary>
    public const decimal ResidentTaxFreeThreshold = 30_000.00m;

    /// <summary>Lower bound of the 18% PAYE tax bracket for residents (FJD).</summary>
    public const decimal ResidentBracket18From = 30_000.00m;

    /// <summary>Upper bound of the 18% PAYE tax bracket for residents (FJD).</summary>
    public const decimal ResidentBracket18To = 50_000.00m;

    /// <summary>Marginal rate for the 18% bracket (expressed as a fraction).</summary>
    public const decimal ResidentRate18 = 0.18m;

    /// <summary>Lower bound of the 20% PAYE tax bracket for residents (FJD).</summary>
    public const decimal ResidentBracket20From = 50_000.00m;

    /// <summary>Marginal rate for the 20% bracket (expressed as a fraction).</summary>
    public const decimal ResidentRate20 = 0.20m;

    /// <summary>Flat PAYE rate applied to all non-resident income (20%).</summary>
    public const decimal NonResidentFlatRate = 0.20m;

    // ─── Payroll Frequency Period Counts ────────────────────────────────────────

    /// <summary>Number of weekly pay periods in a year.</summary>
    public const int WeeklyPeriodsPerYear = 52;

    /// <summary>Number of fortnightly pay periods in a year.</summary>
    public const int FortnightlyPeriodsPerYear = 26;

    /// <summary>Number of bi-monthly (semi-monthly) pay periods in a year.</summary>
    public const int BiMonthlyPeriodsPerYear = 24;

    /// <summary>Number of monthly pay periods in a year.</summary>
    public const int MonthlyPeriodsPerYear = 12;

    // ─── Leave Entitlement Defaults ─────────────────────────────────────────────

    /// <summary>Minimum statutory annual leave entitlement in days per year (Employment Relations Act).</summary>
    public const decimal MinimumAnnualLeaveDays = 10.0m;

    /// <summary>Minimum statutory sick leave entitlement in days per year.</summary>
    public const decimal MinimumSickLeaveDays = 10.0m;

    /// <summary>Daily leave accrual rate for annual leave (10 days / 260 working days).</summary>
    public const decimal AnnualLeaveAccrualRatePerDay = 0.038461538m;

    /// <summary>Annual leave loading percentage when leave is taken (if configured).</summary>
    public const decimal AnnualLeaveLoadingPercent = 25.0m;

    // ─── System Component Codes (cannot be deleted or inactivated) ──────────────

    /// <summary>System component code for PAYE tax deduction.</summary>
    public const string PayeComponentCode = "PAYE";

    /// <summary>System component code for FNPF employee contribution.</summary>
    public const string FnpfEmployeeComponentCode = "FNPF-EMP";

    /// <summary>System component code for FNPF employer contribution.</summary>
    public const string FnpfEmployerComponentCode = "FNPF-EMPLR";

    /// <summary>System component code for basic salary/wage.</summary>
    public const string BasicSalaryComponentCode = "BASIC";
}
