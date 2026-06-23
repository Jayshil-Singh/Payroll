using System;
using System.Windows;

namespace FijiPayroll.WPF.Views.Dialogs;

/// <summary>
/// Interaction logic for PasswordInputDialog.xaml
/// </summary>
public partial class PasswordInputDialog : Window
{
    public string EnteredPassword => NewPasswordBox.Password;

    public PasswordInputDialog(string displayName)
    {
        InitializeComponent();
        
        // Center on owner or default screen center
        if (System.Windows.Application.Current?.MainWindow != null)
        {
            Owner = System.Windows.Application.Current.MainWindow;
        }

        InstructionsTextBlock.Text = $"Please enter a new temporary password for '{displayName}':";
        NewPasswordBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
        {
            MessageBox.Show("Password cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (NewPasswordBox.Password.Length < 8)
        {
            MessageBox.Show("Password must be at least 8 characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
