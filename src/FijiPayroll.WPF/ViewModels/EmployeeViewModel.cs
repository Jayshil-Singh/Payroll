using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.Employees.Commands.TerminateEmployee;
using FijiPayroll.Application.Features.Employees.Queries.GetEmployeeDetail;
using FijiPayroll.Application.Features.Employees.Queries.GetEmployeesList;
using FijiPayroll.Application.Features.Leave.Commands.ApproveLeaveRequest;
using FijiPayroll.Application.Features.Leave.Commands.RejectLeaveRequest;
using FijiPayroll.Application.Features.Leave.Commands.SubmitLeaveRequest;
using FijiPayroll.Application.Features.Leave.Queries.GetLeaveBalances;
using FijiPayroll.Application.Features.Leave.Queries.GetLeaveRequests;
using FijiPayroll.Application.Features.Leave.Queries.GetLeaveTypes;
using FijiPayroll.Application.Features.Loans.Commands.CreateLoan;
using FijiPayroll.Application.Features.Loans.Commands.SuspendLoan;
using FijiPayroll.Application.Features.Loans.Commands.ResumeLoan;
using FijiPayroll.Application.Features.Loans.Commands.WriteOffLoan;
using FijiPayroll.Application.Features.Loans.Queries.GetEmployeeLoans;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.WPF.Services;
using FijiPayroll.WPF.ViewModels.Base;
using MediatR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel governing employee listing, profile details, payment methods, and leave request operations.
/// </summary>
public sealed partial class EmployeeViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;

    // Filter and search properties
    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private string? _selectedDepartment;

    [ObservableProperty]
    private EmployeeSummaryDto? _selectedEmployee;

    [ObservableProperty]
    private EmployeeDetailDto? _selectedEmployeeDetail;

    [ObservableProperty]
    private bool _isLoadingDetails;

    // Leave-related selections
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

    [ObservableProperty]
    private string _rejectionReason = string.Empty;

    // Loan form inputs
    [ObservableProperty]
    private string _loanDescription = string.Empty;

    [ObservableProperty]
    private decimal _loanPrincipalAmount = 1000m;

    [ObservableProperty]
    private decimal _loanInterestRate = 0.05m;

    [ObservableProperty]
    private decimal _loanDeductionAmountPerPeriod = 100m;

    [ObservableProperty]
    private DateTime _loanStartDate = DateTime.Today;

    // Collections
    public ObservableCollection<EmployeeSummaryDto> Employees { get; } = new();
    public ObservableCollection<string> Departments { get; } = new() { "All Departments", "Engineering", "Human Resources", "Finance", "Operations", "Sales" };
    public ObservableCollection<LeaveBalanceDto> LeaveBalances { get; } = new();
    public ObservableCollection<LeaveRequestDto> LeaveRequests { get; } = new();
    public ObservableCollection<LeaveTypeDto> LeaveTypes { get; } = new();
    public ObservableCollection<LoanDto> Loans { get; } = new();

    // Commands
    public ICommand LoadEmployeesCommand { get; }
    public ICommand TerminateEmployeeCommand { get; }
    public ICommand SubmitLeaveRequestCommand { get; }
    public ICommand ApproveLeaveRequestCommand { get; }
    public ICommand RejectLeaveRequestCommand { get; }
    public ICommand CreateLoanCommand { get; }
    public ICommand SuspendLoanCommand { get; }
    public ICommand ResumeLoanCommand { get; }
    public ICommand WriteOffLoanCommand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeViewModel"/> class.
    /// </summary>
    public EmployeeViewModel(
        IMediator mediator,
        ITenantProvider tenantProvider,
        INotificationService notificationService,
        IDialogService dialogService)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        LoadEmployeesCommand = new AsyncRelayCommand(LoadEmployeesAsync);
        TerminateEmployeeCommand = new AsyncRelayCommand(TerminateEmployeeAsync, () => SelectedEmployeeDetail != null && SelectedEmployeeDetail.IsActive);
        SubmitLeaveRequestCommand = new AsyncRelayCommand(SubmitLeaveRequestAsync);
        ApproveLeaveRequestCommand = new AsyncRelayCommand<LeaveRequestDto>(ApproveLeaveRequestAsync);
        RejectLeaveRequestCommand = new AsyncRelayCommand<LeaveRequestDto>(RejectLeaveRequestAsync);
        CreateLoanCommand = new AsyncRelayCommand(CreateLoanAsync);
        SuspendLoanCommand = new AsyncRelayCommand<LoanDto>(SuspendLoanAsync);
        ResumeLoanCommand = new AsyncRelayCommand<LoanDto>(ResumeLoanAsync);
        WriteOffLoanCommand = new AsyncRelayCommand<LoanDto>(WriteOffLoanAsync);

        SelectedDepartment = "All Departments";
        _ = LoadEmployeesAsync();
        _ = LoadLeaveTypesAsync();
    }

    /// <summary>
    /// Title display value.
    /// </summary>
    public string Title => "Employee Registry & Leave Hub";

    partial void OnSearchTermChanged(string value) => _ = LoadEmployeesAsync();
    partial void OnSelectedDepartmentChanged(string? value) => _ = LoadEmployeesAsync();

    partial void OnSelectedEmployeeChanged(EmployeeSummaryDto? value)
    {
        if (value != null)
        {
            _ = LoadEmployeeDetailsAsync(value.Id);
        }
        else
        {
            SelectedEmployeeDetail = null;
            LeaveBalances.Clear();
            LeaveRequests.Clear();
            Loans.Clear();
        }
        (TerminateEmployeeCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
    }

    private async Task LoadEmployeesAsync()
    {
        IsBusy = true;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            string? deptFilter = SelectedDepartment == "All Departments" ? null : SelectedDepartment;

            var result = await _mediator.Send(new GetEmployeesListQuery(
                CompanyId: companyId,
                SearchTerm: string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
                DepartmentFilter: deptFilter,
                PageNumber: 1,
                PageSize: 100
            ));

            if (result.IsSuccess)
            {
                Employees.Clear();
                foreach (var emp in result.Value.Items)
                {
                    Employees.Add(emp);
                }
            }
            else
            {
                _notificationService.Error(result.Error, "Failed to load employees");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error loading employees");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadLeaveTypesAsync()
    {
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var result = await _mediator.Send(new GetLeaveTypesQuery(companyId));
            if (result.IsSuccess)
            {
                LeaveTypes.Clear();
                foreach (var lt in result.Value)
                {
                    LeaveTypes.Add(lt);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load leave types: {ex.Message}");
        }
    }

    private async Task LoadEmployeeDetailsAsync(int employeeId)
    {
        IsLoadingDetails = true;
        try
        {
            var detailResult = await _mediator.Send(new GetEmployeeDetailQuery(employeeId));
            if (detailResult.IsSuccess)
            {
                SelectedEmployeeDetail = detailResult.Value;
            }
            else
            {
                _notificationService.Error(detailResult.Error, "Failed to load profile details");
                SelectedEmployeeDetail = null;
            }

            int fiscalYear = DateTime.Today.Year;
            var balancesResult = await _mediator.Send(new GetLeaveBalancesQuery(employeeId, fiscalYear));
            if (balancesResult.IsSuccess)
            {
                LeaveBalances.Clear();
                foreach (var bal in balancesResult.Value)
                {
                    LeaveBalances.Add(bal);
                }
            }

            int companyId = _tenantProvider.GetCurrentCompanyId();
            var requestsResult = await _mediator.Send(new GetLeaveRequestsQuery(companyId, employeeId));
            if (requestsResult.IsSuccess)
            {
                LeaveRequests.Clear();
                foreach (var req in requestsResult.Value)
                {
                    LeaveRequests.Add(req);
                }
            }

            var loansResult = await _mediator.Send(new GetEmployeeLoansQuery(companyId, employeeId));
            if (loansResult.IsSuccess)
            {
                Loans.Clear();
                foreach (var loan in loansResult.Value)
                {
                    Loans.Add(loan);
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error loading details");
        }
        finally
        {
            IsLoadingDetails = false;
            (TerminateEmployeeCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private async Task TerminateEmployeeAsync()
    {
        if (SelectedEmployeeDetail == null) return;

        bool confirm = await _dialogService.ShowConfirmationAsync(
            "Terminate Employee",
            $"Are you sure you want to terminate {SelectedEmployeeDetail.FullName}? This will deactivate their payment configurations and mark their profile as inactive.");

        if (!confirm) return;

        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new TerminateEmployeeCommand(SelectedEmployeeDetail.Id));
            if (result.IsSuccess)
            {
                _notificationService.Success($"{SelectedEmployeeDetail.FullName} has been terminated successfully.");
                await LoadEmployeesAsync();
                await LoadEmployeeDetailsAsync(SelectedEmployeeDetail.Id);
            }
            else
            {
                _notificationService.Error(result.Error, "Termination Failed");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error terminating employee");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SubmitLeaveRequestAsync()
    {
        if (SelectedEmployeeDetail == null)
        {
            _notificationService.Warning("Please select an employee first.");
            return;
        }

        if (SelectedLeaveType == null)
        {
            _notificationService.Warning("Please select a leave type.");
            return;
        }

        if (LeaveStartDate.Date > LeaveEndDate.Date)
        {
            _notificationService.Warning("Start date cannot be after end date.");
            return;
        }

        if (LeaveTotalDays <= 0)
        {
            _notificationService.Warning("Total working days must be greater than zero.");
            return;
        }

        IsBusy = true;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var command = new SubmitLeaveRequestCommand(
                CompanyId: companyId,
                EmployeeId: SelectedEmployeeDetail.Id,
                LeaveTypeId: SelectedLeaveType.Id,
                StartDate: LeaveStartDate,
                EndDate: LeaveEndDate,
                TotalDays: LeaveTotalDays,
                Notes: string.IsNullOrWhiteSpace(LeaveNotes) ? null : LeaveNotes
            );

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                _notificationService.Success("Leave request submitted successfully.");
                // Reset form fields
                LeaveNotes = string.Empty;
                LeaveTotalDays = 1.0m;
                // Reload lists
                await LoadEmployeeDetailsAsync(SelectedEmployeeDetail.Id);
            }
            else
            {
                _notificationService.Error(result.Error, "Submission Failed");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error submitting leave request");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ApproveLeaveRequestAsync(LeaveRequestDto? req)
    {
        if (req == null) return;

        bool confirm = await _dialogService.ShowConfirmationAsync(
            "Approve Leave Request",
            $"Are you sure you want to approve this request of {req.TotalDays} days for {req.EmployeeName}?");

        if (!confirm) return;

        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new ApproveLeaveRequestCommand(req.Id));
            if (result.IsSuccess)
            {
                _notificationService.Success("Leave request approved successfully.");
                if (SelectedEmployeeDetail != null)
                {
                    await LoadEmployeeDetailsAsync(SelectedEmployeeDetail.Id);
                }
            }
            else
            {
                _notificationService.Error(result.Error, "Approval Failed");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error approving leave request");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RejectLeaveRequestAsync(LeaveRequestDto? req)
    {
        if (req == null) return;

        if (string.IsNullOrWhiteSpace(RejectionReason))
        {
            _notificationService.Warning("A rejection reason is required.");
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new RejectLeaveRequestCommand(req.Id, RejectionReason));
            if (result.IsSuccess)
            {
                _notificationService.Success("Leave request rejected successfully.");
                RejectionReason = string.Empty;
                if (SelectedEmployeeDetail != null)
                {
                    await LoadEmployeeDetailsAsync(SelectedEmployeeDetail.Id);
                }
            }
            else
            {
                _notificationService.Error(result.Error, "Rejection Failed");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error rejecting leave request");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateLoanAsync()
    {
        if (SelectedEmployeeDetail == null)
        {
            _notificationService.Warning("Please select an employee first.");
            return;
        }

        if (string.IsNullOrWhiteSpace(LoanDescription))
        {
            _notificationService.Warning("Please enter a loan description.");
            return;
        }

        if (LoanPrincipalAmount <= 0)
        {
            _notificationService.Warning("Principal amount must be greater than zero.");
            return;
        }

        if (LoanInterestRate < 0)
        {
            _notificationService.Warning("Interest rate cannot be negative.");
            return;
        }

        if (LoanDeductionAmountPerPeriod <= 0)
        {
            _notificationService.Warning("Deduction amount per period must be greater than zero.");
            return;
        }

        IsBusy = true;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var command = new CreateLoanCommand(
                CompanyId: companyId,
                EmployeeId: SelectedEmployeeDetail.Id,
                Description: LoanDescription,
                PrincipalAmount: LoanPrincipalAmount,
                InterestRate: LoanInterestRate,
                DeductionAmountPerPeriod: LoanDeductionAmountPerPeriod,
                StartDate: LoanStartDate
            );

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                _notificationService.Success("Loan registered successfully.");
                // Reset form fields
                LoanDescription = string.Empty;
                LoanPrincipalAmount = 1000m;
                LoanInterestRate = 0.05m;
                LoanDeductionAmountPerPeriod = 100m;
                LoanStartDate = DateTime.Today;
                // Reload lists
                await LoadEmployeeDetailsAsync(SelectedEmployeeDetail.Id);
            }
            else
            {
                _notificationService.Error(result.Error, "Loan Registration Failed");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error registering loan");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SuspendLoanAsync(LoanDto? loan)
    {
        if (loan == null || SelectedEmployeeDetail == null) return;

        bool confirm = await _dialogService.ShowConfirmationAsync(
            "Suspend Loan",
            $"Are you sure you want to suspend deductions for this loan: '{loan.LoanDescription}'?");

        if (!confirm) return;

        IsBusy = true;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var result = await _mediator.Send(new SuspendLoanCommand(companyId, loan.Id));
            if (result.IsSuccess)
            {
                _notificationService.Success("Loan suspended successfully.");
                await LoadEmployeeDetailsAsync(SelectedEmployeeDetail.Id);
            }
            else
            {
                _notificationService.Error(result.Error, "Suspension Failed");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error suspending loan");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ResumeLoanAsync(LoanDto? loan)
    {
        if (loan == null || SelectedEmployeeDetail == null) return;

        bool confirm = await _dialogService.ShowConfirmationAsync(
            "Resume Loan",
            $"Are you sure you want to resume deductions for this loan: '{loan.LoanDescription}'?");

        if (!confirm) return;

        IsBusy = true;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var result = await _mediator.Send(new ResumeLoanCommand(companyId, loan.Id));
            if (result.IsSuccess)
            {
                _notificationService.Success("Loan resumed successfully.");
                await LoadEmployeeDetailsAsync(SelectedEmployeeDetail.Id);
            }
            else
            {
                _notificationService.Error(result.Error, "Resume Failed");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error resuming loan");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task WriteOffLoanAsync(LoanDto? loan)
    {
        if (loan == null || SelectedEmployeeDetail == null) return;

        bool confirm = await _dialogService.ShowConfirmationAsync(
            "Write Off Loan",
            $"Are you sure you want to write off the remaining balance of {loan.RemainingBalance:C} for this loan: '{loan.LoanDescription}'? This action cannot be undone.");

        if (!confirm) return;

        IsBusy = true;
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var result = await _mediator.Send(new WriteOffLoanCommand(companyId, loan.Id));
            if (result.IsSuccess)
            {
                _notificationService.Success("Loan written off successfully.");
                await LoadEmployeeDetailsAsync(SelectedEmployeeDetail.Id);
            }
            else
            {
                _notificationService.Error(result.Error, "Write-off Failed");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error writing off loan");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
