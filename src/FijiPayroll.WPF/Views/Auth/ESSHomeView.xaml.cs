using FijiPayroll.WPF.ViewModels.Auth;
using System.Windows.Controls;

namespace FijiPayroll.WPF.Views.Auth;

/// <summary>
/// Interaction logic for ESSHomeView.xaml
/// </summary>
public partial class ESSHomeView : UserControl
{
    public ESSHomeView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Switches visible tab panel based on the checked RadioButton's Tag value.
    /// </summary>
    private void Tab_Checked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.RadioButton rb) return;
        if (!int.TryParse(rb.Tag?.ToString(), out int idx)) return;

        TabDashboard.Visibility = idx == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        TabPayslips.Visibility  = idx == 1 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        TabLeave.Visibility     = idx == 2 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        TabLoans.Visibility     = idx == 3 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        TabProfile.Visibility   = idx == 4 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }
}
