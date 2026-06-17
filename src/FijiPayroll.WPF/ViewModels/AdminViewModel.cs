using FijiPayroll.WPF.ViewModels.Base;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel managing role permission views, audit logs, and general settings.
/// </summary>
public sealed class AdminViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Gets the panel title.
    /// </summary>
    public string Title => "Administration Settings";

    /// <summary>
    /// Initialises a new instance of the <see cref="AdminViewModel"/> class.
    /// </summary>
    public AdminViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        OpenStagedImportCommand = new RelayCommand(OpenStagedImport);
    }

    /// <summary>Gets the open staged import command.</summary>
    public IRelayCommand OpenStagedImportCommand { get; }

    private void OpenStagedImport()
    {
        var vm = _serviceProvider.GetRequiredService<StagedImportViewModel>();
        var window = new Views.StagedImportWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
    }
}
