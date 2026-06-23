using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Formula;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// View model backing the Payroll Component Simulation view.
/// Allows simulating rule changes against an employee context.
/// </summary>
public sealed partial class ComponentSimulationViewModel : ObservableObject
{
    private readonly SimulationEngine _simulationEngine;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty]
    private string _expressionText = string.Empty;

    [ObservableProperty]
    private string _componentCode = string.Empty;

    [ObservableProperty]
    private decimal _originalResultValue;

    [ObservableProperty]
    private decimal _simulatedResultValue;

    [ObservableProperty]
    private decimal _differenceValue;

    [ObservableProperty]
    private string _executionLog = string.Empty;

    [ObservableProperty]
    private bool _isSimulating;

    /// <summary>
    /// Gets the list of available employees for selection.
    /// </summary>
    public ObservableCollection<EmployeeDto> Employees { get; } = new();

    [ObservableProperty]
    private EmployeeDto? _selectedEmployee;

    /// <summary>
    /// Initialises a new instance of the <see cref="ComponentSimulationViewModel"/> class.
    /// </summary>
    public ComponentSimulationViewModel(
        SimulationEngine simulationEngine,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _simulationEngine = simulationEngine;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;

        RunSimulationCommand = new AsyncRelayCommand(RunSimulationAsync);
        LoadEmployeesCommand = new AsyncRelayCommand(LoadEmployeesAsync);
    }

    /// <summary>Gets the run simulation command.</summary>
    public IAsyncRelayCommand RunSimulationCommand { get; }

    /// <summary>Gets the load employees command.</summary>
    public IAsyncRelayCommand LoadEmployeesCommand { get; }

    private async Task LoadEmployeesAsync()
    {
        try
        {
            int companyId = _tenantProvider.GetCurrentCompanyId();
            var empList = await _unitOfWork.Employees.GetPagedAsync(
                companyId, searchTerm: null, departmentFilter: null, pageNumber: 1, pageSize: 100);
            Employees.Clear();
            foreach (var emp in empList.Items)
            {
                Employees.Add(new EmployeeDto(emp.Id, emp.FullName, emp.BaseSalary));
            }
            if (Employees.Count > 0)
            {
                SelectedEmployee = Employees[0];
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading employees: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RunSimulationAsync()
    {
        if (SelectedEmployee == null)
        {
            MessageBox.Show("Please select an employee first.", "Simulation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsSimulating = true;
        try
        {
            var context = new SimulationEngine.SimulationContext
            {
                CompanyId = _tenantProvider.GetCurrentCompanyId(),
                EmployeeId = SelectedEmployee.Id,
                ComponentCode = ComponentCode,
                OriginalExpression = ExpressionText,
                OverriddenExpression = ExpressionText, // In wizard, user edits this expression
                InputVariables = new Dictionary<string, decimal>
                {
                    { "BASIC", SelectedEmployee.BaseSalary },
                    { "HourlyRate", SelectedEmployee.BaseSalary / 160m },
                    { "OvertimeHours", 10m }
                }
            };

            var res = await _simulationEngine.RunSimulationAsync(context);
            OriginalResultValue = res.OriginalValue;
            SimulatedResultValue = res.SimulatedValue;
            DifferenceValue = res.Difference;
            ExecutionLog = res.SimulatedRun.ExecutionTrace;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Simulation failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsSimulating = false;
        }
    }
}

/// <summary>
/// Simple DTO for employee selection.
/// </summary>
public sealed record EmployeeDto(int Id, string FullName, decimal BaseSalary);
