using System.Windows.Controls;
using FijiPayroll.WPF.ViewModels;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Interaction logic for DiagnosticsDashboardView.xaml.
/// </summary>
public partial class DiagnosticsDashboardView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticsDashboardView"/> class.
    /// </summary>
    public DiagnosticsDashboardView(DiagnosticsDashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Start performance tracking on load and stop on unload to prevent leaks
        Loaded += (s, e) => viewModel.StartMonitoring();
        Unloaded += (s, e) => viewModel.StopMonitoring();
    }
}
