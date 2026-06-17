using FijiPayroll.WPF.ViewModels;
using System.Windows;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Interaction logic for PayrollComponentEditorWindow.xaml.
/// Hosts the <see cref="PayrollComponentFormViewModel"/> for both create and edit modes.
/// Closes automatically when the ViewModel raises <see cref="PayrollComponentFormViewModel.CloseRequested"/>.
/// </summary>
public partial class PayrollComponentEditorWindow : Window
{
    private readonly PayrollComponentFormViewModel _viewModel;

    /// <summary>
    /// Initialises a new instance of <see cref="PayrollComponentEditorWindow"/> in <b>create mode</b>.
    /// </summary>
    /// <param name="viewModel">The form ViewModel pre-configured for the desired mode.</param>
    public PayrollComponentEditorWindow(PayrollComponentFormViewModel viewModel)
    {
        InitializeComponent();

        _viewModel  = viewModel;
        DataContext = _viewModel;

        // Subscribe to the ViewModel close signal
        _viewModel.CloseRequested += OnCloseRequested;

        Unloaded += (_, _) => _viewModel.CloseRequested -= OnCloseRequested;
    }

    /// <summary>
    /// Gets a value indicating whether the dialog was closed after a successful save.
    /// Intended to be checked by the caller via <see cref="System.Windows.Window.ShowDialog"/>.
    /// </summary>
    public bool SavedSuccessfully { get; private set; }

    // ── Private ──────────────────────────────────────────────────────────

    private void OnCloseRequested(bool success)
    {
        SavedSuccessfully = success;

        // Must run on the Dispatcher if raised from an async context
        if (Dispatcher.CheckAccess())
        {
            DialogResult = success;
            Close();
        }
        else
        {
            Dispatcher.Invoke(() =>
            {
                DialogResult = success;
                Close();
            });
        }
    }
}
