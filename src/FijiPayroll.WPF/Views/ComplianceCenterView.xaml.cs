using System.Windows.Controls;
using FijiPayroll.WPF.ViewModels;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Interaction logic for ComplianceCenterView.xaml.
/// </summary>
public partial class ComplianceCenterView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComplianceCenterView"/> class.
    /// </summary>
    public ComplianceCenterView(ComplianceCenterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Load the compliance dashboard on view activation
        Loaded += async (s, e) => await viewModel.LoadDashboardAsync();
    }
}
