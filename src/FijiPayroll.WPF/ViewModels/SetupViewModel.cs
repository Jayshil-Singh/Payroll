using FijiPayroll.WPF.ViewModels.Base;
using System;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using FijiPayroll.Application.Common.Interfaces;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel managing setup configuration panels (e.g. tax tables, allowances configuration).
/// </summary>
public sealed class SetupViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuthSessionStore _sessionStore;

    /// <summary>
    /// Initialises a new instance of the <see cref="SetupViewModel"/> class.
    /// </summary>
    public SetupViewModel(
        PayrollComponentViewModel componentViewModel, 
        MasterLookupManagerViewModel lookupManagerViewModel,
        IServiceProvider serviceProvider,
        IAuthSessionStore sessionStore)
    {
        ComponentViewModel = componentViewModel;
        LookupManagerViewModel = lookupManagerViewModel;
        _serviceProvider = serviceProvider;
        _sessionStore = sessionStore;

        LaunchSetupWizardCommand = new RelayCommand(LaunchSetupWizard);
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

    /// <summary>
    /// Checks if the current session belongs to an administrator.
    /// </summary>
    public bool IsAdmin => _sessionStore.Current?.Roles?.Any(r => 
        r.Equals("PayrollAdministrator", StringComparison.OrdinalIgnoreCase) || 
        r.Equals("Administrator", StringComparison.OrdinalIgnoreCase)) == true;

    /// <summary>
    /// Gets the command to launch the Setup Wizard.
    /// </summary>
    public ICommand LaunchSetupWizardCommand { get; }

    private void LaunchSetupWizard()
    {
        var window = _serviceProvider.GetRequiredService<Views.Setup.SetupWizardWindow>();
        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();
    }
}
