using System;
using System.Windows;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Interaction logic for SplashWindow.xaml
/// </summary>
public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Thread-safely updates the progress value and stage label texts.
    /// </summary>
    public void UpdateProgress(double percent, string title, string details)
    {
        Dispatcher.BeginInvoke(() =>
        {
            LoadingProgressBar.Value = percent;
            ProgressTitleLabel.Text = title;
            ProgressDetailsLabel.Text = details;
        });
    }
}
