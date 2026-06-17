using FijiPayroll.WPF.ViewModels;
using System.Windows;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Interaction logic for CloneWizardWindow.xaml.
/// </summary>
public partial class CloneWizardWindow : Window
{
    /// <summary>
    /// Initialises a new instance of the <see cref="CloneWizardWindow"/> class.
    /// </summary>
    public CloneWizardWindow(CloneWizardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
