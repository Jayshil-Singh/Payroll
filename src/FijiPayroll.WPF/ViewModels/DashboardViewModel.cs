using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Features.Dashboard.Queries;
using FijiPayroll.WPF.ViewModels.Base;
using MediatR;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel for the executive dashboard panel containing live KPI widgets,
/// payroll financial summaries, system alerts, and historic chart data.
/// </summary>
public sealed class DashboardViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    // ── KPI Card Properties ───────────────────────────────────────────────────

    private int _activeEmployeesCount;
    private int _terminatedThisMonthCount;
    private int _openPeriodsCount;
    private string _currentPeriodCode = "N/A";
    private string _currentRunStatus = "No Active Run";
    private int _postedRunsCount;

    // ── Financial Summary ─────────────────────────────────────────────────────

    private decimal _latestGrossPay;
    private decimal _latestPAYETax;
    private decimal _latestFNPFEmployee;
    private decimal _latestFNPFEmployer;
    private decimal _latestNetPay;

    // ── State Flags ───────────────────────────────────────────────────────────

    private bool _hasSystemAlerts;
    private string _loadStatusText = "Loading dashboard...";
    private bool _isDataLoaded;

    public DashboardViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));

        SystemAlerts = new ObservableCollection<string>();
        RecentRuns = new ObservableCollection<HistoricRunDto>();
        ChartBars = new ObservableCollection<ChartBarItem>();

        RefreshCommand = new AsyncRelayCommand(LoadDashboardDataAsync);

        // Auto-load on construction via post-dispatch
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Loaded,
            async () => await LoadDashboardDataAsync());
    }

    /// <summary>Gets the panel title.</summary>
    public string Title => "Executive Dashboard";

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>Refreshes all dashboard data from the database.</summary>
    public IAsyncRelayCommand RefreshCommand { get; }

    // ── KPI Properties ────────────────────────────────────────────────────────

    /// <summary>Count of currently active employees in the company.</summary>
    public int ActiveEmployeesCount
    {
        get => _activeEmployeesCount;
        private set => SetProperty(ref _activeEmployeesCount, value);
    }

    /// <summary>Count of employees terminated during the current calendar month.</summary>
    public int TerminatedThisMonthCount
    {
        get => _terminatedThisMonthCount;
        private set => SetProperty(ref _terminatedThisMonthCount, value);
    }

    /// <summary>Count of open payroll periods.</summary>
    public int OpenPeriodsCount
    {
        get => _openPeriodsCount;
        private set => SetProperty(ref _openPeriodsCount, value);
    }

    /// <summary>Period code of the current open payroll period.</summary>
    public string CurrentPeriodCode
    {
        get => _currentPeriodCode;
        private set => SetProperty(ref _currentPeriodCode, value);
    }

    /// <summary>Status of the most recent payroll run.</summary>
    public string CurrentRunStatus
    {
        get => _currentRunStatus;
        private set => SetProperty(ref _currentRunStatus, value);
    }

    /// <summary>Total count of posted/finalized payroll runs.</summary>
    public int PostedRunsCount
    {
        get => _postedRunsCount;
        private set => SetProperty(ref _postedRunsCount, value);
    }

    // ── Financial Properties ──────────────────────────────────────────────────

    /// <summary>Gross pay total from the latest posted payroll.</summary>
    public decimal LatestGrossPay
    {
        get => _latestGrossPay;
        private set
        {
            SetProperty(ref _latestGrossPay, value);
            OnPropertyChanged(nameof(LatestGrossPayFormatted));
        }
    }

    /// <summary>PAYE tax total from the latest posted payroll.</summary>
    public decimal LatestPAYETax
    {
        get => _latestPAYETax;
        private set
        {
            SetProperty(ref _latestPAYETax, value);
            OnPropertyChanged(nameof(LatestPAYETaxFormatted));
        }
    }

    /// <summary>FNPF employee (8%) contribution total.</summary>
    public decimal LatestFNPFEmployee
    {
        get => _latestFNPFEmployee;
        private set
        {
            SetProperty(ref _latestFNPFEmployee, value);
            OnPropertyChanged(nameof(LatestFNPFFormatted));
        }
    }

    /// <summary>FNPF employer (10%) contribution total.</summary>
    public decimal LatestFNPFEmployer
    {
        get => _latestFNPFEmployer;
        private set => SetProperty(ref _latestFNPFEmployer, value);
    }

    /// <summary>Net pay total from the latest posted payroll.</summary>
    public decimal LatestNetPay
    {
        get => _latestNetPay;
        private set
        {
            SetProperty(ref _latestNetPay, value);
            OnPropertyChanged(nameof(LatestNetPayFormatted));
        }
    }

    // ── Formatted Display Properties ──────────────────────────────────────────

    public string LatestGrossPayFormatted => FormatCurrency(_latestGrossPay);
    public string LatestPAYETaxFormatted  => FormatCurrency(_latestPAYETax);
    public string LatestNetPayFormatted   => FormatCurrency(_latestNetPay);
    public string LatestFNPFFormatted     => FormatCurrency(_latestFNPFEmployee + _latestFNPFEmployer);

    // ── Status / Alerts ───────────────────────────────────────────────────────

    /// <summary>Observable list of system alert strings to display in the alerts pane.</summary>
    public ObservableCollection<string> SystemAlerts { get; }

    /// <summary>Indicates whether there are any active system alerts.</summary>
    public bool HasSystemAlerts
    {
        get => _hasSystemAlerts;
        private set => SetProperty(ref _hasSystemAlerts, value);
    }

    /// <summary>Loading status text shown while data is fetching.</summary>
    public string LoadStatusText
    {
        get => _loadStatusText;
        private set => SetProperty(ref _loadStatusText, value);
    }

    /// <summary>Whether the dashboard has successfully loaded data.</summary>
    public bool IsDataLoaded
    {
        get => _isDataLoaded;
        private set => SetProperty(ref _isDataLoaded, value);
    }

    // ── Chart Data ────────────────────────────────────────────────────────────

    /// <summary>Recent payroll runs for the chart.</summary>
    public ObservableCollection<HistoricRunDto> RecentRuns { get; }

    /// <summary>Scaled chart bar items derived from recent runs.</summary>
    public ObservableCollection<ChartBarItem> ChartBars { get; }

    // ── Data Loader ───────────────────────────────────────────────────────────

    private async Task LoadDashboardDataAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;

        IsBusy = true;
        IsDataLoaded = false;
        LoadStatusText = "Fetching live dashboard metrics...";

        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var result = await _mediator.Send(new GetDashboardSummaryQuery(companyId), cancellationToken);

            if (!result.IsSuccess)
            {
                LoadStatusText = $"Error: {result.Error}";
                return;
            }

            var dto = result.Value!;

            // KPIs
            ActiveEmployeesCount     = dto.ActiveEmployeesCount;
            TerminatedThisMonthCount = dto.TerminatedThisMonthCount;
            OpenPeriodsCount         = dto.OpenPeriodsCount;
            CurrentPeriodCode        = dto.CurrentPeriodCode;
            CurrentRunStatus         = dto.CurrentRunStatus;
            PostedRunsCount          = dto.PostedRunsCount;

            // Financials
            LatestGrossPay    = dto.LatestGrossPay;
            LatestPAYETax     = dto.LatestPAYETax;
            LatestFNPFEmployee = dto.LatestFNPFEmployee;
            LatestFNPFEmployer = dto.LatestFNPFEmployer;
            LatestNetPay      = dto.LatestNetPay;

            // Alerts
            SystemAlerts.Clear();
            foreach (var alert in dto.SystemAlerts)
                SystemAlerts.Add(alert);
            HasSystemAlerts = SystemAlerts.Count > 0;

            // Chart bars — scale heights relative to max gross pay
            RecentRuns.Clear();
            ChartBars.Clear();
            decimal maxGross = 1;
            foreach (var run in dto.RecentRuns)
            {
                RecentRuns.Add(run);
                if (run.GrossPay > maxGross) maxGross = run.GrossPay;
            }
            foreach (var run in dto.RecentRuns)
            {
                ChartBars.Add(new ChartBarItem
                {
                    Label       = run.PeriodCode,
                    GrossPay    = run.GrossPay,
                    NetPay      = run.NetPay,
                    PAYETax     = run.PAYETax,
                    BarHeight   = (double)(run.GrossPay / maxGross) * 130,
                    IsHighlight = run == dto.RecentRuns[^1]
                });
            }

            IsDataLoaded  = true;
            LoadStatusText = $"Last refreshed: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            LoadStatusText = $"Failed to load: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string FormatCurrency(decimal value)
    {
        if (value >= 1_000_000) return $"${value / 1_000_000:F2}M";
        if (value >= 1_000) return $"${value / 1_000:F1}K";
        return $"${value:F2}";
    }
}

/// <summary>
/// Represents one bar in the disbursements chart, with scaled height and label.
/// </summary>
public sealed class ChartBarItem
{
    public string Label { get; set; } = string.Empty;
    public decimal GrossPay { get; set; }
    public decimal NetPay { get; set; }
    public decimal PAYETax { get; set; }
    public double BarHeight { get; set; }
    public bool IsHighlight { get; set; }
    public string BarColor => IsHighlight ? "#0D99FF" : "#2E3656";
}
