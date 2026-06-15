using FijiPayroll.WPF.ViewModels.Base;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel managing setup configuration panels (e.g. tax tables, allowances configuration).
/// </summary>
public sealed class SetupViewModel : ViewModelBase
{
    /// <summary>
    /// Initialises a new instance of the <see cref="SetupViewModel"/> class.
    /// </summary>
    public SetupViewModel(PayrollComponentViewModel componentViewModel, MasterLookupManagerViewModel lookupManagerViewModel)
    {
        ComponentViewModel = componentViewModel;
        LookupManagerViewModel = lookupManagerViewModel;
    }

    /// <summary>
    /// Gets the underlying component configurations view model.
    /// </summary>
    public PayrollComponentViewModel ComponentViewModel { get; }

    /// <summary>
    /// Gets the lookup manager view model.
    /// </summary>
    public MasterLookupManagerViewModel LookupManagerViewModel { get; }

    /// <summary>
    /// Gets the panel title.
    /// </summary>
    public string Title => "Setup & Configurations";
}
