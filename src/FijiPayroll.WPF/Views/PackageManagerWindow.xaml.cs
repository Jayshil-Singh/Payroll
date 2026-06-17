using FijiPayroll.WPF.ViewModels;
using System.Windows;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Interaction logic for PackageManagerWindow.xaml.
/// </summary>
public partial class PackageManagerWindow : Window
{
    /// <summary>
    /// Initialises a new instance of the <see cref="PackageManagerWindow"/> class.
    /// </summary>
    public PackageManagerWindow(PackageManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (s, e) => viewModel.LoadInstalledPackagesCommand.Execute(null);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
