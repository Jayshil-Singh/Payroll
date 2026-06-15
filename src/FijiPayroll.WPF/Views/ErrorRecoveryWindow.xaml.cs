using System;
using System.Diagnostics;
using System.Windows;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Interaction logic for ErrorRecoveryWindow.xaml
/// </summary>
public partial class ErrorRecoveryWindow : Window
{
    public ErrorRecoveryWindow(Exception exception)
    {
        InitializeComponent();
        
        // Populate tech details
        string fullMessage = $"{exception.GetType().FullName}: {exception.Message}\n\n{exception.StackTrace}";
        if (exception.InnerException != null)
        {
            fullMessage += $"\n\nInner Exception: {exception.InnerException.GetType().FullName}: {exception.InnerException.Message}\n\n{exception.InnerException.StackTrace}";
        }
        
        StackTraceTextBox.Text = fullMessage;
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Restart_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Perform a safe process restart
            string? processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath))
            {
                Process.Start(new ProcessStartInfo(processPath) { UseShellExecute = true });
            }
            else
            {
                // Fallback to command line args
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length > 0)
                {
                    Process.Start(new ProcessStartInfo(args[0]) { UseShellExecute = true });
                }
            }
        }
        catch
        {
            // Safe swallow if relaunch fails, at least proceed to shutdown
        }

        System.Windows.Application.Current.Shutdown();
    }
}
