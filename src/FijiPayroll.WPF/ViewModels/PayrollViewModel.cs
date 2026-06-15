using FijiPayroll.WPF.ViewModels.Base;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// Container ViewModel for the payroll processing operations, hosting the detailed run manager view model.
/// </summary>
public sealed class PayrollViewModel : ViewModelBase
{
    /// <summary>
    /// Initialises a new instance of the <see cref="PayrollViewModel"/> class.
    /// </summary>
    public PayrollViewModel(PayrollRunViewModel runViewModel)
    {
        RunViewModel = runViewModel;
    }

    /// <summary>
    /// Gets the underlying payroll run manager view model.
    /// </summary>
    public PayrollRunViewModel RunViewModel { get; }

    /// <summary>
    /// Gets the panel title.
    /// </summary>
    public string Title => "Payroll Module";
}
