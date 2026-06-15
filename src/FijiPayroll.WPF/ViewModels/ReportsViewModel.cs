using FijiPayroll.WPF.ViewModels.Base;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel managing report generating panels, payslip distributions, and statutory reports.
/// </summary>
public sealed class ReportsViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the panel title.
    /// </summary>
    public string Title => "Reports & Analytics";
}
