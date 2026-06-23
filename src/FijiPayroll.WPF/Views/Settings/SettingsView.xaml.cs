using FijiPayroll.WPF.ViewModels.Settings;
using System.Windows.Controls;

namespace FijiPayroll.WPF.Views.Settings;

/// <summary>
/// Code-behind for SettingsView. Handles the PasswordBox password binding workaround
/// (WPF PasswordBox does not support two-way data binding for security reasons).
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        Loaded += SettingsView_Loaded;
    }

    private void SettingsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        // Pre-populate password field from ViewModel on first load
        if (DataContext is SettingsViewModel vm)
        {
            SmtpPasswordBox.Password = vm.SmtpPassword;
        }
    }

    private void SmtpPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.SmtpPassword = SmtpPasswordBox.Password;
        }
    }
}
