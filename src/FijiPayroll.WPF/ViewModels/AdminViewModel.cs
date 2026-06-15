using FijiPayroll.WPF.ViewModels.Base;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel managing role permission views, audit logs, and general settings.
/// </summary>
public sealed class AdminViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the panel title.
    /// </summary>
    public string Title => "Administration Settings";
}
