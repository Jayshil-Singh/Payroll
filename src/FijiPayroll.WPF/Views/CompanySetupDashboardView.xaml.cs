using FijiPayroll.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Interaction logic for CompanySetupDashboardView.xaml
/// </summary>
public partial class CompanySetupDashboardView : UserControl
{
    /// <summary>
    /// Initialises a new instance of the <see cref="CompanySetupDashboardView"/> class.
    /// </summary>
    public CompanySetupDashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CompanySetupDashboardViewModel viewModel)
        {
            if (viewModel.LoadStateCommand.CanExecute(null))
            {
                viewModel.LoadStateCommand.Execute(null);
            }
        }
    }
}
