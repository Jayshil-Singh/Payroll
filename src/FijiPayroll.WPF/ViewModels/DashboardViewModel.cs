using FijiPayroll.WPF.ViewModels.Base;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel for the executive dashboard panel containing KPI widgets and status summaries.
/// </summary>
public sealed class DashboardViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the panel screen title.
    /// </summary>
    public string Title => "Dashboard";
}
