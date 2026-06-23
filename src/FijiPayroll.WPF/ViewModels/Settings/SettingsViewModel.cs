using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Features.Settings.Commands;
using FijiPayroll.Application.Features.Settings.Queries;
using FijiPayroll.WPF.Services;
using FijiPayroll.WPF.ViewModels.Base;
using MediatR;

namespace FijiPayroll.WPF.ViewModels.Settings;

/// <summary>
/// ViewModel for the system settings configuration screen.
/// Allows admins to configure payroll defaults, directory paths, and SMTP email settings.
/// </summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;
    private readonly INotificationService _notificationService;

    // ── Payroll Defaults ──────────────────────────────────────────────────────
    private string _defaultPayFrequency    = "Weekly";
    private string _defaultPayrollCalendar = "Standard 2026";
    private string _negativePayPolicy      = "PartialDeduction";

    // ── Directories ───────────────────────────────────────────────────────────
    private string _defaultSubmissionPaths = @"C:\FijiPayroll\Submissions";
    private string _backupDirectory        = @"C:\FijiPayroll\Backups";
    private string _exportDirectory        = @"C:\FijiPayroll\Exports";
    private string _importDirectory        = @"C:\FijiPayroll\Imports";

    // ── SMTP ──────────────────────────────────────────────────────────────────
    private string _smtpHost       = string.Empty;
    private int    _smtpPort       = 587;
    private string _smtpUsername   = string.Empty;
    private string _smtpPassword   = string.Empty;
    private bool   _smtpSslEnabled = true;

    // ── State ─────────────────────────────────────────────────────────────────
    private bool   _isDirty;
    private string _statusMessage = "Load settings to begin.";
    private bool   _hasError;

    public SettingsViewModel(
        IMediator mediator,
        ITenantProvider tenantProvider,
        INotificationService notificationService)
    {
        _mediator            = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _tenantProvider      = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => IsDirty && !IsBusy);

        // Auto-load
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Loaded,
            async () => await LoadAsync());
    }

    public string Title => "System Settings";

    // ── Commands ──────────────────────────────────────────────────────────────
    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }

    // ── State Properties ──────────────────────────────────────────────────────

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            SetProperty(ref _isDirty, value);
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    // ── Payroll Defaults ──────────────────────────────────────────────────────

    public string DefaultPayFrequency
    {
        get => _defaultPayFrequency;
        set { if (SetProperty(ref _defaultPayFrequency, value)) IsDirty = true; }
    }

    public string DefaultPayrollCalendar
    {
        get => _defaultPayrollCalendar;
        set { if (SetProperty(ref _defaultPayrollCalendar, value)) IsDirty = true; }
    }

    public string NegativePayPolicy
    {
        get => _negativePayPolicy;
        set { if (SetProperty(ref _negativePayPolicy, value)) IsDirty = true; }
    }

    // ── Directories ───────────────────────────────────────────────────────────

    public string DefaultSubmissionPaths
    {
        get => _defaultSubmissionPaths;
        set { if (SetProperty(ref _defaultSubmissionPaths, value)) IsDirty = true; }
    }

    public string BackupDirectory
    {
        get => _backupDirectory;
        set { if (SetProperty(ref _backupDirectory, value)) IsDirty = true; }
    }

    public string ExportDirectory
    {
        get => _exportDirectory;
        set { if (SetProperty(ref _exportDirectory, value)) IsDirty = true; }
    }

    public string ImportDirectory
    {
        get => _importDirectory;
        set { if (SetProperty(ref _importDirectory, value)) IsDirty = true; }
    }

    // ── SMTP ──────────────────────────────────────────────────────────────────

    public string SmtpHost
    {
        get => _smtpHost;
        set { if (SetProperty(ref _smtpHost, value)) IsDirty = true; }
    }

    public int SmtpPort
    {
        get => _smtpPort;
        set { if (SetProperty(ref _smtpPort, value)) IsDirty = true; }
    }

    public string SmtpUsername
    {
        get => _smtpUsername;
        set { if (SetProperty(ref _smtpUsername, value)) IsDirty = true; }
    }

    public string SmtpPassword
    {
        get => _smtpPassword;
        set { if (SetProperty(ref _smtpPassword, value)) IsDirty = true; }
    }

    public bool SmtpSslEnabled
    {
        get => _smtpSslEnabled;
        set { if (SetProperty(ref _smtpSslEnabled, value)) IsDirty = true; }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;

        IsBusy = true;
        HasError = false;
        StatusMessage = "Loading settings...";

        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var result = await _mediator.Send(new GetSystemSettingsQuery(companyId), cancellationToken);

            if (!result.IsSuccess)
            {
                HasError = true;
                StatusMessage = $"Error: {result.Error}";
                return;
            }

            var dto = result.Value!;

            // Populate without triggering IsDirty
            _defaultPayFrequency    = dto.DefaultPayFrequency;
            _defaultPayrollCalendar = dto.DefaultPayrollCalendar;
            _negativePayPolicy      = dto.NegativePayPolicy;
            _defaultSubmissionPaths = dto.DefaultSubmissionPaths;
            _backupDirectory        = dto.BackupDirectory;
            _exportDirectory        = dto.ExportDirectory;
            _importDirectory        = dto.ImportDirectory;
            _smtpHost               = dto.SmtpHost;
            _smtpPort               = dto.SmtpPort;
            _smtpUsername           = dto.SmtpUsername;
            _smtpPassword           = dto.SmtpPassword;
            _smtpSslEnabled         = dto.SmtpSslEnabled;

            // Notify all properties changed
            OnPropertyChanged(string.Empty);

            IsDirty = false;
            StatusMessage = $"Settings loaded successfully — {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Load failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;

        // Port validation
        if (SmtpPort is < 1 or > 65535)
        {
            _notificationService.Error("SMTP Port must be between 1 and 65535.", "Validation Error");
            return;
        }

        IsBusy = true;
        HasError = false;
        StatusMessage = "Saving settings...";

        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();

            var command = new UpdateSystemSettingsCommand
            {
                CompanyId              = companyId,
                DefaultPayFrequency    = DefaultPayFrequency,
                DefaultPayrollCalendar = DefaultPayrollCalendar,
                NegativePayPolicy      = NegativePayPolicy,
                DefaultSubmissionPaths = DefaultSubmissionPaths,
                BackupDirectory        = BackupDirectory,
                ExportDirectory        = ExportDirectory,
                ImportDirectory        = ImportDirectory,
                SmtpHost               = SmtpHost,
                SmtpPort               = SmtpPort,
                SmtpUsername           = SmtpUsername,
                SmtpPassword           = SmtpPassword,
                SmtpSslEnabled         = SmtpSslEnabled,
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                HasError = true;
                StatusMessage = $"Save failed: {result.Error}";
                _notificationService.Error(result.Error ?? "Unknown error", "Settings Error");
                return;
            }

            IsDirty = false;
            StatusMessage = $"Settings saved — {DateTime.Now:HH:mm:ss}";
            _notificationService.Success("System settings have been saved successfully.", "Settings Saved");
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Save failed: {ex.Message}";
            _notificationService.Error(ex.Message, "Settings Error");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
