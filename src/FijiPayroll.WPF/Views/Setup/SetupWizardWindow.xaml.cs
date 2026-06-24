using System.Windows;
using System.Windows.Controls;
using FijiPayroll.WPF.ViewModels.Setup;

namespace FijiPayroll.WPF.Views.Setup;

/// <summary>
/// Interaction logic for SetupWizardWindow.xaml
/// </summary>
public partial class SetupWizardWindow : Window
{
    public SetupWizardWindow(SetupWizardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.SetupCompletedSuccessfully += () =>
        {
            Dispatcher.Invoke(() =>
            {
                DialogResult = true;
                Close();
            });
        };
    }

    private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel viewModel && sender is PasswordBox pbox)
        {
            viewModel.AdminPassword = pbox.Password;
        }
    }

    private void TxtConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel viewModel && sender is PasswordBox pbox)
        {
            viewModel.AdminConfirmPassword = pbox.Password;
        }
    }

    private void TxtDbPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel viewModel && sender is PasswordBox pbox)
        {
            viewModel.DbPassword = pbox.Password;
        }
    }
}
