using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Features.Employees.Queries.GetEmployeeDetail;
using FijiPayroll.Application.Features.Leave.Commands.SubmitLeaveRequest;
using FijiPayroll.Application.Features.Leave.Queries.GetLeaveBalances;
using FijiPayroll.Application.Features.Leave.Queries.GetLeaveRequests;
using FijiPayroll.Application.Features.Leave.Queries.GetLeaveTypes;
using FijiPayroll.Application.Features.Loans.Queries.GetEmployeeLoans;
using FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollRunList;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.WPF.Services;
using FijiPayroll.WPF.ViewModels.Base;
using MediatR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using FijiPayroll.SDK.Interfaces;

namespace FijiPayroll.WPF.ViewModels.Auth;

/// <summary>
/// ViewModel for the Employee Self-Service (ESS) portal.
/// Scoped exclusively to the logged-in employee – all queries are constrained to their EmployeeId.
/// </summary>
public sealed partial class ESSHomeViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAuthSessionStore _sessionStore;
    private readonly FijiPayroll.WPF.Services.INotificationService _notificationService;
    private readonly IReportProvider _reportProvider;

    // ─── Tab Index ────────────────────────────────────────────────────────────
    [ObservableProperty]
    private int _activeTabIndex;

    // ─── Profile ──────────────────────────────────────────────────────────────
    [ObservableProperty]
    private EmployeeDetailDto? _employeeProfile;

    // ─── Payslips ─────────────────────────────────────────────────────────────
    [ObservableProperty]
    private PayrollRunSummaryDto? _selectedPayslipRun;

    // ─── Leave ────────────────────────────────────────────────────────────────
    [ObservableProperty]
    private LeaveTypeDto? _selectedLeaveType;

    [ObservableProperty]
    private DateTime _leaveStartDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _leaveEndDate = DateTime.Today;

    [ObservableProperty]
    private decimal _leaveTotalDays = 1.0m;

    [ObservableProperty]
    private string _leaveNotes = string.Empty;

    // ─── Collections ─────────────────────────────────────────────────────────
    public ObservableCollection<LeaveBalanceDto> LeaveBalances { get; } = new();
    public ObservableCollection<LeaveRequestDto> LeaveRequests { get; } = new();
    public ObservableCollection<LeaveTypeDto> LeaveTypes { get; } = new();
    public ObservableCollection<LoanDto> Loans { get; } = new();
    public ObservableCollection<PayrollRunSummaryDto> PayslipRuns { get; } = new();

    // ─── Commands ─────────────────────────────────────────────────────────────
    public ICommand LoadDataCommand { get; }
    public ICommand SubmitLeaveRequestCommand { get; }
    public ICommand DownloadPayslipCommand { get; }

    /// <summary>Gets the display title for the ESS portal.</summary>
    public string Title => "Employee Self-Service Portal";

    /// <summary>Gets the current employee's display name, or a default greeting.</summary>
    public string WelcomeMessage =>
        EmployeeProfile?.FullName is { } name ? $"Welcome, {name}" : "Employee Portal";

    public ESSHomeViewModel(
        IMediator mediator,
        ITenantProvider tenantProvider,
        IAuthSessionStore sessionStore,
        FijiPayroll.WPF.Services.INotificationService notificationService,
        IReportProvider reportProvider)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _reportProvider = reportProvider ?? throw new ArgumentNullException(nameof(reportProvider));

        LoadDataCommand = new AsyncRelayCommand(LoadAllDataAsync);
        SubmitLeaveRequestCommand = new AsyncRelayCommand(SubmitLeaveRequestAsync);
        DownloadPayslipCommand = new AsyncRelayCommand<PayrollRunSummaryDto?>(DownloadPayslipAsync);

        _ = LoadAllDataAsync();
    }

    // ─── Derived ESS Employee ID ──────────────────────────────────────────────
    private int? GetEmployeeId() => _sessionStore.Current?.EmployeeId;

    private int GetCompanyId() => _tenantProvider.GetCurrentCompanyId();

    // ─── Data Loading ─────────────────────────────────────────────────────────

    private async Task LoadAllDataAsync()
    {
        IsBusy = true;
        try
        {
            int? empId = GetEmployeeId();
            if (empId == null)
            {
                _notificationService.Warning("No employee linked to this account. Please contact your administrator.");
                return;
            }

            await Task.WhenAll(
                LoadProfileAsync(empId.Value),
                LoadLeaveDataAsync(empId.Value),
                LoadLoansAsync(empId.Value),
                LoadPayslipsAsync()
            );
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Failed to load ESS data");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadProfileAsync(int employeeId)
    {
        var result = await _mediator.Send(new GetEmployeeDetailQuery(employeeId));
        if (result.IsSuccess)
        {
            EmployeeProfile = result.Value;
            OnPropertyChanged(nameof(WelcomeMessage));
        }
    }

    private async Task LoadLeaveDataAsync(int employeeId)
    {
        // Leave types
        var typesResult = await _mediator.Send(new GetLeaveTypesQuery(GetCompanyId()));
        if (typesResult.IsSuccess)
        {
            LeaveTypes.Clear();
            foreach (var lt in typesResult.Value) LeaveTypes.Add(lt);
        }

        // Leave balances (current fiscal year)
        int fiscalYear = DateTime.Today.Year;
        var balResult = await _mediator.Send(new GetLeaveBalancesQuery(employeeId, fiscalYear));
        if (balResult.IsSuccess)
        {
            LeaveBalances.Clear();
            foreach (var lb in balResult.Value) LeaveBalances.Add(lb);
        }

        // Leave requests
        var reqResult = await _mediator.Send(new GetLeaveRequestsQuery(GetCompanyId(), employeeId));
        if (reqResult.IsSuccess)
        {
            LeaveRequests.Clear();
            foreach (var lr in reqResult.Value) LeaveRequests.Add(lr);
        }
    }

    private async Task LoadLoansAsync(int employeeId)
    {
        var result = await _mediator.Send(new GetEmployeeLoansQuery(GetCompanyId(), employeeId));
        if (result.IsSuccess)
        {
            Loans.Clear();
            foreach (var loan in result.Value) Loans.Add(loan);
        }
    }

    private async Task LoadPayslipsAsync()
    {
        var result = await _mediator.Send(new GetPayrollRunListQuery(
            CompanyId: GetCompanyId(),
            FrequencyFilter: null,
            StatusFilter: PayrollRunStatus.Posted,
            PageNumber: 1,
            PageSize: 24));

        if (result.IsSuccess)
        {
            PayslipRuns.Clear();
            foreach (var run in result.Value.Items) PayslipRuns.Add(run);
        }
    }

    // ─── Leave Submission ─────────────────────────────────────────────────────

    private async Task SubmitLeaveRequestAsync()
    {
        int? empId = GetEmployeeId();
        if (empId == null) { _notificationService.Warning("No employee linked to this account."); return; }
        if (SelectedLeaveType == null) { _notificationService.Warning("Please select a leave type."); return; }
        if (LeaveStartDate > LeaveEndDate) { _notificationService.Warning("Start date cannot be after end date."); return; }
        if (LeaveTotalDays <= 0) { _notificationService.Warning("Total days must be greater than zero."); return; }

        IsBusy = true;
        try
        {
            var cmd = new SubmitLeaveRequestCommand(
                CompanyId: GetCompanyId(),
                EmployeeId: empId.Value,
                LeaveTypeId: SelectedLeaveType.Id,
                StartDate: LeaveStartDate,
                EndDate: LeaveEndDate,
                TotalDays: LeaveTotalDays,
                Notes: string.IsNullOrWhiteSpace(LeaveNotes) ? null : LeaveNotes);

            var result = await _mediator.Send(cmd);
            if (result.IsSuccess)
            {
                _notificationService.Success("Leave request submitted successfully. Your manager will be notified.");
                LeaveNotes = string.Empty;
                LeaveTotalDays = 1.0m;
                await LoadLeaveDataAsync(empId.Value);
            }
            else
            {
                _notificationService.Error(result.Error, "Leave Submission Failed");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error submitting leave");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ─── Payslip Download ─────────────────────────────────────────────────────

    private async Task DownloadPayslipAsync(PayrollRunSummaryDto? run)
    {
        if (run == null) return;
        
        int? employeeId = GetEmployeeId();
        if (employeeId == null)
        {
            _notificationService.Warning("No employee context linked to this session.");
            return;
        }

        IsBusy = true;
        try
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Documents (*.pdf)|*.pdf",
                FileName = $"Payslip_{run.PeriodName.Replace(" ", "_")}_{employeeId}.pdf",
                Title = "Download Payslip"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var parameters = new Dictionary<string, string>
                {
                    { "@P_CompanyId", GetCompanyId().ToString() },
                    { "@P_PayrollRunId", run.Id.ToString() },
                    { "@P_EmployeeId", employeeId.Value.ToString() }
                };

                byte[] bytes = await _reportProvider.RenderReportAsync("Payslips", "PDF", parameters);
                await System.IO.File.WriteAllBytesAsync(saveFileDialog.FileName, bytes);
                _notificationService.Success($"Payslip downloaded successfully to {System.IO.Path.GetFileName(saveFileDialog.FileName)}", "Download Success");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error($"Download failed: {ex.Message}", "Download Error");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
