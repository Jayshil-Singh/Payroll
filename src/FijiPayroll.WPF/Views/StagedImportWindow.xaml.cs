using FijiPayroll.WPF.ViewModels;
using System.Windows;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Interaction logic for StagedImportWindow.xaml.
/// </summary>
public partial class StagedImportWindow : Window
{
    /// <summary>
    /// Initialises a new instance of the <see cref="StagedImportWindow"/> class.
    /// </summary>
    public StagedImportWindow(StagedImportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
