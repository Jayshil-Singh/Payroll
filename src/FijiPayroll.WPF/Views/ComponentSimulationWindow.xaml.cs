using FijiPayroll.WPF.ViewModels;
using System.Windows;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Interaction logic for ComponentSimulationWindow.xaml.
/// </summary>
public partial class ComponentSimulationWindow : Window
{
    /// <summary>
    /// Initialises a new instance of the <see cref="ComponentSimulationWindow"/> class.
    /// </summary>
    public ComponentSimulationWindow(ComponentSimulationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (s, e) => await viewModel.LoadEmployeesCommand.ExecuteAsync(null);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
