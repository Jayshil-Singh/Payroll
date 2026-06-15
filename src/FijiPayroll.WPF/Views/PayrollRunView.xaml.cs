using FijiPayroll.WPF.ViewModels;
using System.Windows.Controls;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Interaction logic for PayrollRunView.xaml
/// </summary>
public partial class PayrollRunView : UserControl
{
    public PayrollRunView(PayrollRunViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Auto-load runs on layout load
        Loaded += async (s, e) =>
        {
            if (viewModel.LoadRunsCommand.CanExecute(null))
            {
                await viewModel.LoadRunsCommand.ExecuteAsync(null);
            }
        };
    }
}
