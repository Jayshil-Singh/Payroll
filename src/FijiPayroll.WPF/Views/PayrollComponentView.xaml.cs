using FijiPayroll.WPF.ViewModels;
using System.Windows.Controls;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Interaction logic for PayrollComponentView.xaml.
/// </summary>
public partial class PayrollComponentView : UserControl
{
    /// <summary>
    /// Initialises a new instance of the <see cref="PayrollComponentView"/> class.
    /// </summary>
    public PayrollComponentView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="PayrollComponentView"/> class with VM.
    /// </summary>
    /// <param name="viewModel">The view model instance.</param>
    public PayrollComponentView(PayrollComponentViewModel viewModel) : this()
    {
        DataContext = viewModel;

        Loaded += async (s, e) =>
        {
            if (viewModel.LoadComponentsCommand.CanExecute(null))
            {
                await viewModel.LoadComponentsCommand.ExecuteAsync(null);
            }
        };
    }
}
