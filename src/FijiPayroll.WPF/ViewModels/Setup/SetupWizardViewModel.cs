using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.WPF.Services;
using FijiPayroll.WPF.ViewModels.Base;
using Microsoft.Win32;

namespace FijiPayroll.WPF.ViewModels.Setup;

public sealed partial class SetupWizardViewModel : ViewModelBase
{
    private readonly ISetupWizardService _setupService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private int _currentStep = 1;

    [ObservableProperty]
    private string _stepTitle = "Database Configuration";

    // ─── Step 1: Database Configuration ───────────────────────────────────────
    [ObservableProperty]
    private string _dbServer = @".\CS415";

    [ObservableProperty]
    private string _dbName = "FijiPayrollDb";

    [ObservableProperty]
    private string _dbConnectionStatus = "Not Connected";

    [ObservableProperty]
    private bool _dbIsConnected;

    [ObservableProperty]
    private string _dbStatusDetails = string.Empty;

    // ─── Step 2: Fiscal Calendar ─────────────────────────────────────────────
    [ObservableProperty]
    private DateTime _calendarStartDate = new DateTime(DateTime.Today.Year, 1, 1);

    [ObservableProperty]
    private DateTime _calendarEndDate = new DateTime(DateTime.Today.Year, 12, 31);

    [ObservableProperty]
    private string _calendarFrequency = "Monthly"; // Weekly, Fortnightly, Monthly, Bi-Monthly

    [ObservableProperty]
    private DateTime? _firstPayrollDate = new DateTime(DateTime.Today.Year, 1, 25);

    public ObservableCollection<string> Frequencies { get; } = new() { "Weekly", "Fortnightly", "Monthly", "Bi-Monthly" };

    // ─── Step 3: Company Information ─────────────────────────────────────────
    [ObservableProperty]
    private string _companyName = string.Empty;

    [ObservableProperty]
    private string _tradingName = string.Empty;

    [ObservableProperty]
    private string _companyTIN = string.Empty;

    [ObservableProperty]
    private string _companyFnpf = string.Empty;

    [ObservableProperty]
    private string _companyBRN = string.Empty;

    [ObservableProperty]
    private string _physicalAddress = string.Empty;

    [ObservableProperty]
    private string _postalAddress = string.Empty;

    [ObservableProperty]
    private string _contactNumber = string.Empty;

    [ObservableProperty]
    private string _companyEmail = string.Empty;

    [ObservableProperty]
    private string _companyWebsite = string.Empty;

    // ─── Step 4: Payroll Defaults ────────────────────────────────────────────
    [ObservableProperty]
    private string _defaultPayFrequency = "Monthly";

    [ObservableProperty]
    private string _defaultCurrency = "FJD";

    [ObservableProperty]
    private int _workingDaysPerWeek = 5;

    [ObservableProperty]
    private decimal _standardHoursPerWeek = 40.0m;

    [ObservableProperty]
    private string _negativeNetPayPolicy = "PartialDeduction"; // BlockDeduction, PartialDeduction, AllowNegativeNetPay

    public ObservableCollection<string> Policies { get; } = new() { "BlockDeduction", "PartialDeduction", "AllowNegativeNetPay" };

    // ─── Step 5: Earnings & Deductions ───────────────────────────────────────
    [ObservableProperty]
    private bool _useDefaultComponents = true;

    // ─── Step 6: Leave Setup ─────────────────────────────────────────────────
    [ObservableProperty] private decimal _leaveAnnualDays = 10;
    [ObservableProperty] private decimal _leaveAnnualMaxCarry = 5;
    [ObservableProperty] private decimal _leaveAnnualMaxBalance = 20;

    [ObservableProperty] private decimal _leaveSickDays = 10;
    [ObservableProperty] private decimal _leaveSickMaxCarry = 0;
    [ObservableProperty] private decimal _leaveSickMaxBalance = 10;

    [ObservableProperty] private decimal _leaveCompassionateDays = 3;
    [ObservableProperty] private decimal _leaveMaternityDays = 84;
    [ObservableProperty] private decimal _leaveStudyDays = 5;

    // ─── Step 7: Employee Import ─────────────────────────────────────────────
    [ObservableProperty]
    private string _importFilePath = string.Empty;

    [ObservableProperty]
    private string _importStatusText = "No file loaded. You can Skip this step or select an Excel/CSV file to load employees.";

    [ObservableProperty]
    private int _importedCount;

    public ObservableCollection<SetupWizardEmployeeImport> ImportedEmployees { get; } = new();

    // ─── Step 8: Security & Users ────────────────────────────────────────────
    [ObservableProperty]
    private string _adminUsername = "admin";

    [ObservableProperty]
    private string _adminPassword = string.Empty;

    [ObservableProperty]
    private string _adminConfirmPassword = string.Empty;

    [ObservableProperty]
    private string _adminFullName = "System Administrator";

    [ObservableProperty]
    private string _adminEmail = string.Empty;

    // ─── Command properties ──────────────────────────────────────────────────
    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand BrowseFileCommand { get; }
    public ICommand ParseFileCommand { get; }
    public ICommand CompleteSetupCommand { get; }

    public event Action? SetupCompletedSuccessfully;

    public SetupWizardViewModel(ISetupWizardService setupService, INotificationService notificationService)
    {
        _setupService = setupService;
        _notificationService = notificationService;

        PreviousCommand = new RelayCommand(GoPrevious, CanGoPrevious);
        NextCommand = new RelayCommand(GoNext, CanGoNext);
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
        BrowseFileCommand = new RelayCommand(BrowseFile);
        ParseFileCommand = new AsyncRelayCommand(ParseFileAsync);
        CompleteSetupCommand = new AsyncRelayCommand(CompleteSetupAsync);
    }

    private void UpdateStepTitle()
    {
        StepTitle = CurrentStep switch
        {
            1 => "Database Configuration",
            2 => "Fiscal Calendar",
            3 => "Company Information",
            4 => "Payroll Defaults",
            5 => "Earnings & Deductions",
            6 => "Leave Setup",
            7 => "Employee Import",
            8 => "Security & Users",
            9 => "Review & Finish",
            _ => "Setup Wizard"
        };

        // Sync default frequency from Calendar Selection to Payroll Defaults
        if (CurrentStep == 4)
        {
            DefaultPayFrequency = CalendarFrequency;
        }

        (PreviousCommand as RelayCommand)?.NotifyCanExecuteChanged();
        (NextCommand as RelayCommand)?.NotifyCanExecuteChanged();
    }

    private bool CanGoPrevious() => CurrentStep > 1;

    private void GoPrevious()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
            UpdateStepTitle();
        }
    }

    private bool CanGoNext()
    {
        if (CurrentStep == 9) return false;
        
        // Block step 1 if not connected
        if (CurrentStep == 1) return DbIsConnected;

        return true;
    }

    private void GoNext()
    {
        if (ValidateStep())
        {
            CurrentStep++;
            UpdateStepTitle();
        }
    }

    private bool ValidateStep()
    {
        switch (CurrentStep)
        {
            case 1:
                if (!DbIsConnected)
                {
                    _notificationService.Warning("Please test and establish database connection first.", "Connection Required");
                    return false;
                }
                break;

            case 2:
                if (CalendarStartDate >= CalendarEndDate)
                {
                    _notificationService.Warning("Start date must be earlier than the end date.", "Invalid Dates");
                    return false;
                }
                break;

            case 3:
                if (string.IsNullOrWhiteSpace(CompanyName))
                {
                    _notificationService.Warning("Company name is required.", "Validation Error");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(CompanyTIN) || CompanyTIN.Length != 9 || !CompanyTIN.All(char.IsDigit))
                {
                    _notificationService.Warning("Company TIN must be exactly 9 digits.", "Validation Error");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(CompanyFnpf))
                {
                    _notificationService.Warning("FNPF number is required.", "Validation Error");
                    return false;
                }
                break;

            case 4:
                if (WorkingDaysPerWeek < 1 || WorkingDaysPerWeek > 7)
                {
                    _notificationService.Warning("Working days per week must be between 1 and 7.", "Validation Error");
                    return false;
                }
                if (StandardHoursPerWeek < 1 || StandardHoursPerWeek > 168)
                {
                    _notificationService.Warning("Standard hours per week must be valid.", "Validation Error");
                    return false;
                }
                break;

            case 8:
                if (string.IsNullOrWhiteSpace(AdminUsername))
                {
                    _notificationService.Warning("Administrator username is required.", "Validation Error");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(AdminPassword) || AdminPassword.Length < 8)
                {
                    _notificationService.Warning("Password must be at least 8 characters long.", "Weak Password");
                    return false;
                }
                if (!Regex.IsMatch(AdminPassword, @"[0-9]") || !Regex.IsMatch(AdminPassword, @"[^a-zA-Z0-9]"))
                {
                    _notificationService.Warning("Password must contain at least one digit and one special character.", "Weak Password");
                    return false;
                }
                if (AdminPassword != AdminConfirmPassword)
                {
                    _notificationService.Warning("Passwords do not match.", "Validation Error");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(AdminFullName))
                {
                    _notificationService.Warning("Full name is required.", "Validation Error");
                    return false;
                }
                break;
        }

        return true;
    }

    private async Task TestConnectionAsync()
    {
        IsBusy = true;
        DbConnectionStatus = "Testing connection...";
        DbStatusDetails = string.Empty;
        DbIsConnected = false;
        (NextCommand as RelayCommand)?.NotifyCanExecuteChanged();

        string connStr = $"Server={DbServer};Database={DbName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        try
        {
            bool ok = await _setupService.TestConnectionAsync(connStr);
            if (ok)
            {
                DbIsConnected = true;
                DbConnectionStatus = "Connected Successfully!";
                DbStatusDetails = "SQL Server connection validated. Proceeding to save and migrate database...";

                // Save config and run migrations
                await _setupService.SaveConnectionStringAsync(connStr);
                await _setupService.RunMigrationsAndSeedAsync();

                _notificationService.Success("Database connected and migrated successfully.", "Setup Successful");
            }
            else
            {
                DbConnectionStatus = "Connection Failed";
                DbStatusDetails = "Could not open connection to SQL Server instance. Please verify server name, authentication, and status.";
                _notificationService.Error("Failed to connect to the database.", "Connection Error");
            }
        }
        catch (Exception ex)
        {
            DbConnectionStatus = "Connection Error";
            DbStatusDetails = $"Error during connection test: {ex.Message}";
            _notificationService.Error($"Connection failed: {ex.Message}", "Database Error");
        }
        finally
        {
            IsBusy = false;
            (NextCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private void BrowseFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title = "Select Employee Import File"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            ImportFilePath = openFileDialog.FileName;
            ImportStatusText = $"Selected file: {Path.GetFileName(ImportFilePath)}. Click Parse/Validate to load.";
        }
    }

    private async Task ParseFileAsync()
    {
        if (string.IsNullOrWhiteSpace(ImportFilePath) || !File.Exists(ImportFilePath))
        {
            _notificationService.Warning("Please select a valid file first.", "File Selection Required");
            return;
        }

        IsBusy = true;
        ImportStatusText = "Parsing and validating spreadsheet data...";
        ImportedEmployees.Clear();

        try
        {
            var parsed = await _setupService.ParseEmployeesAsync(ImportFilePath);
            ImportedCount = parsed.Count;

            foreach (var emp in parsed)
            {
                ImportedEmployees.Add(emp);
            }

            ImportStatusText = $"Successfully parsed {ImportedCount} employees. Ready to proceed.";
            _notificationService.Success($"Parsed {ImportedCount} employee records successfully.", "Import Parsed");
        }
        catch (Exception ex)
        {
            ImportStatusText = $"Parse Error: {ex.Message}";
            _notificationService.Error($"Failed to parse file: {ex.Message}", "Import Error");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CompleteSetupAsync()
    {
        IsBusy = true;
        try
        {
            var data = new SetupWizardData
            {
                Company = new SetupWizardCompanyData
                {
                    CompanyName = CompanyName,
                    TradingName = TradingName,
                    TIN = CompanyTIN,
                    FnpfNumber = CompanyFnpf,
                    BusinessRegistrationNumber = CompanyBRN,
                    PhysicalAddress = PhysicalAddress,
                    PostalAddress = PostalAddress,
                    ContactNumber = ContactNumber,
                    EmailAddress = CompanyEmail,
                    Website = CompanyWebsite
                },
                Calendar = new SetupWizardCalendarData
                {
                    StartDate = CalendarStartDate,
                    EndDate = CalendarEndDate,
                    PayrollFrequency = CalendarFrequency,
                    FirstPayrollDate = FirstPayrollDate
                },
                Defaults = new SetupWizardDefaultsData
                {
                    DefaultPayFrequency = DefaultPayFrequency,
                    Currency = DefaultCurrency,
                    WorkingDaysPerWeek = WorkingDaysPerWeek,
                    StandardHoursPerWeek = StandardHoursPerWeek,
                    NegativeNetPayPolicy = NegativeNetPayPolicy
                },
                UseDefaultComponents = UseDefaultComponents,
                LeavePolicies = new List<SetupWizardLeavePolicy>
                {
                    new() { LeaveTypeName = "Annual Leave", Category = "AnnualLeave", EntitlementDays = LeaveAnnualDays, MaxCarryOverDays = LeaveAnnualMaxCarry, MaximumBalance = LeaveAnnualMaxBalance },
                    new() { LeaveTypeName = "Sick Leave", Category = "SickLeave", EntitlementDays = LeaveSickDays, MaxCarryOverDays = LeaveSickMaxCarry, MaximumBalance = LeaveSickMaxBalance },
                    new() { LeaveTypeName = "Compassionate Leave", Category = "BereavementLeave", EntitlementDays = LeaveCompassionateDays },
                    new() { LeaveTypeName = "Maternity Leave", Category = "MaternityLeave", EntitlementDays = LeaveMaternityDays },
                    new() { LeaveTypeName = "Study Leave", Category = "Other", EntitlementDays = LeaveStudyDays }
                },
                Employees = ImportedEmployees.ToList(),
                Administrator = new SetupWizardUserData
                {
                    Username = AdminUsername,
                    Password = AdminPassword,
                    FullName = AdminFullName,
                    Email = AdminEmail
                }
            };

            await _setupService.CompleteSetupAsync(data);
            
            _notificationService.Success("Setup Wizard Completed Successfully!", "Setup Completed");
            SetupCompletedSuccessfully?.Invoke();
        }
        catch (Exception ex)
        {
            _notificationService.Error($"Onboarding setup completion failed: {ex.Message}", "Configuration Error");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
