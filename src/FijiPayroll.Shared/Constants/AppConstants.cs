namespace FijiPayroll.Shared.Constants;

/// <summary>
/// Global application-level constants used across all layers.
/// </summary>
public static class AppConstants
{
    /// <summary>The application display name.</summary>
    public const string ApplicationName = "Fiji Enterprise Payroll System";

    /// <summary>The application version.</summary>
    public const string ApplicationVersion = "1.0.0";

    /// <summary>The default currency used in Fiji.</summary>
    public const string DefaultCurrency = "FJD";

    /// <summary>Number of working days assumed per year for daily rate calculations.</summary>
    public const decimal WorkingDaysPerYear = 260m;

    /// <summary>Default standard working hours per week.</summary>
    public const decimal DefaultHoursPerWeek = 40m;

    /// <summary>Default standard working days per week.</summary>
    public const decimal DefaultDaysPerWeek = 5m;

    /// <summary>Default overtime rate multiplier (time and a half).</summary>
    public const decimal DefaultOvertimeMultiplier = 1.5m;

    /// <summary>Double-time multiplier for public holidays.</summary>
    public const decimal DoubleTimeMultiplier = 2.0m;

    /// <summary>Decimal precision stored in the database for monetary values.</summary>
    public const int MoneyStoragePrecision = 4;

    /// <summary>Decimal precision displayed for monetary values.</summary>
    public const int MoneyDisplayPrecision = 2;

    /// <summary>Decimal precision for leave accrual stored values.</summary>
    public const int LeaveAccrualPrecision = 4;

    /// <summary>Maximum account lockout minutes after failed login attempts.</summary>
    public const int AccountLockoutMinutes = 30;

    /// <summary>Maximum consecutive failed login attempts before lockout.</summary>
    public const int MaxFailedLoginAttempts = 5;

    /// <summary>Log file path pattern.</summary>
    public const string LogFilePath = @"%ProgramData%\FijiPayroll\Logs\fpayroll-.log";

    /// <summary>License file extension.</summary>
    public const string LicenseFileExtension = ".fplic";

    /// <summary>Page size for paginated queries.</summary>
    public const int DefaultPageSize = 25;

    /// <summary>Maximum records for an export operation.</summary>
    public const int MaxExportRecords = 100_000;
}
