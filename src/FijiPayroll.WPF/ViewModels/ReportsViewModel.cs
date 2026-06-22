using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Application.Services;
using FijiPayroll.SDK.Interfaces;
using FijiPayroll.WPF.Services;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.WPF.ViewModels.Base;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using IWpfNotificationService = FijiPayroll.WPF.Services.INotificationService;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel managing report generating panels, payslip distributions, and statutory reports.
/// </summary>
public sealed class ReportsViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReportProvider _reportProvider;
    private readonly ITenantProvider _tenantProvider;
    private readonly IWpfNotificationService _notifications;


    private PayrollRun? _selectedRun;
    private PayrollRun? _selectedCompareRun;
    private string _selectedFormat = "PDF";

    public ReportsViewModel(
        IUnitOfWork unitOfWork,
        IReportProvider reportProvider,
        ITenantProvider tenantProvider,
        IWpfNotificationService notifications)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _reportProvider = reportProvider ?? throw new ArgumentNullException(nameof(reportProvider));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));

        PayrollRuns = new ObservableCollection<PayrollRun>();
        Formats = new ObservableCollection<string> { "PDF", "Excel" };

        LoadRunsCommand = new AsyncRelayCommand(LoadRunsAsync);
        ExportSummaryCommand = new AsyncRelayCommand(ExportSummaryAsync, () => SelectedRun != null);
        ExportRegisterCommand = new AsyncRelayCommand(ExportRegisterAsync, () => SelectedRun != null);
        ExportPayslipsCommand = new AsyncRelayCommand(ExportPayslipsAsync, () => SelectedRun != null);
        ExportVarianceCommand = new AsyncRelayCommand(ExportVarianceAsync, () => SelectedRun != null && SelectedCompareRun != null);

        // Run initial load
        _ = LoadRunsAsync();
    }

    public string Title => "Reports & Analytics";

    public ObservableCollection<PayrollRun> PayrollRuns { get; }
    public ObservableCollection<string> Formats { get; }

    public PayrollRun? SelectedRun
    {
        get => _selectedRun;
        set
        {
            if (SetProperty(ref _selectedRun, value))
            {
                (ExportSummaryCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                (ExportRegisterCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                (ExportPayslipsCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                (ExportVarianceCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();

                // Automatically try to select a comparative preceding run when SelectedRun changes
                if (_selectedRun != null)
                {
                    SelectedCompareRun = PayrollRuns
                        .Where(r => r.Id != _selectedRun.Id && r.EndDate < _selectedRun.StartDate)
                        .OrderByDescending(r => r.EndDate)
                        .FirstOrDefault();
                }
            }
        }
    }

    public PayrollRun? SelectedCompareRun
    {
        get => _selectedCompareRun;
        set
        {
            if (SetProperty(ref _selectedCompareRun, value))
            {
                (ExportVarianceCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public string SelectedFormat
    {
        get => _selectedFormat;
        set => SetProperty(ref _selectedFormat, value);
    }

    // Commands
    public ICommand LoadRunsCommand { get; }
    public ICommand ExportSummaryCommand { get; }
    public ICommand ExportRegisterCommand { get; }
    public ICommand ExportPayslipsCommand { get; }
    public ICommand ExportVarianceCommand { get; }

    private async Task LoadRunsAsync()
    {
        IsBusy = true;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            // Get all payroll runs for the company. Using GetPagedAsync with a large limit.
            var result = await _unitOfWork.PayrollRuns.GetPagedAsync(companyId, null, null, 1, 1000);
            
            PayrollRuns.Clear();
            // We can display Calculated, Approved, Posted, etc.
            var relevantRuns = result.Items
                .Where(r => r.Status >= PayrollRunStatus.Calculated)
                .OrderByDescending(r => r.EndDate);

            foreach (var run in relevantRuns)
            {
                PayrollRuns.Add(run);
            }

            if (SelectedRun == null && PayrollRuns.Count > 0)
            {
                SelectedRun = PayrollRuns[0];
            }
        }
        catch (Exception ex)
        {
            _notifications.Error($"Failed to load payroll runs: {ex.Message}", "Error");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportSummaryAsync()
    {
        if (SelectedRun == null) return;
        await SaveReportAsync("PayrollSummary", new Dictionary<string, string>
        {
            { "CompanyId", _tenantProvider.GetCurrentCompanyId().ToString() },
            { "PayrollRunId", SelectedRun.Id.ToString() }
        });
    }

    private async Task ExportRegisterAsync()
    {
        if (SelectedRun == null) return;
        await SaveReportAsync("PayrollRegister", new Dictionary<string, string>
        {
            { "CompanyId", _tenantProvider.GetCurrentCompanyId().ToString() },
            { "PayrollRunId", SelectedRun.Id.ToString() }
        });
    }

    private async Task ExportPayslipsAsync()
    {
        if (SelectedRun == null) return;
        await SaveReportAsync("Payslips", new Dictionary<string, string>
        {
            { "CompanyId", _tenantProvider.GetCurrentCompanyId().ToString() },
            { "PayrollRunId", SelectedRun.Id.ToString() }
        });
    }

    private async Task ExportVarianceAsync()
    {
        if (SelectedRun == null || SelectedCompareRun == null) return;
        await SaveReportAsync("PayrollVariance", new Dictionary<string, string>
        {
            { "CompanyId", _tenantProvider.GetCurrentCompanyId().ToString() },
            { "PayrollRunId", SelectedRun.Id.ToString() },
            { "CompareRunId", SelectedCompareRun.Id.ToString() }
        });
    }

    private async Task SaveReportAsync(string reportName, Dictionary<string, string> parameters)
    {
        IsBusy = true;
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = SelectedFormat.Equals("Excel", StringComparison.OrdinalIgnoreCase)
                    ? "Excel Workbooks (*.xlsx)|*.xlsx"
                    : "PDF Documents (*.pdf)|*.pdf",
                FileName = $"{reportName}_{SelectedRun?.RunCode ?? "Export"}.{(SelectedFormat.Equals("Excel", StringComparison.OrdinalIgnoreCase) ? "xlsx" : "pdf")}",
                Title = $"Export {reportName}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                byte[] bytes = await _reportProvider.RenderReportAsync(reportName, SelectedFormat, parameters);
                await File.WriteAllBytesAsync(saveFileDialog.FileName, bytes);
                _notifications.Success($"Report exported successfully to {Path.GetFileName(saveFileDialog.FileName)}", "Export Success");
            }
        }
        catch (Exception ex)
        {
            _notifications.Error($"Export failed: {ex.Message}", "Export Error");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
