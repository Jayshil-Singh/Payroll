using FijiPayroll.WPF.Services;
using FijiPayroll.WPF.ViewModels.Auth;
using FijiPayroll.WPF.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Lightweight ESS shell window — the host container for the Employee Self-Service portal.
/// Replaces MainWindow for users whose only role is "Employee".
/// Shares the same INavigationService singleton so ESSHomeViewModel is resolved correctly.
/// </summary>
public partial class ESSShellWindow : Window
{
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;

    /// <summary>Active toast notifications displayed in the ESS shell overlay.</summary>
    public ObservableCollection<ESSToastItem> ActiveToasts { get; } = new();

    public ESSShellWindow(
        INavigationService navigationService,
        INotificationService notificationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

        InitializeComponent();

        // DataContext exposes CurrentViewModel so the ContentControl resolves the DataTemplate
        DataContext = this;

        ToastContainer.ItemsSource = ActiveToasts;

        _notificationService.NotificationRaised += OnNotificationRaised;
    }

    /// <summary>Current navigation view model, resolved from the shared navigation service.</summary>
    public ViewModelBase? CurrentViewModel => _navigationService.CurrentView;

    /// <summary>Welcome greeting derived from the active ESS view model.</summary>
    public string WelcomeMessage =>
        (_navigationService.CurrentView as ESSHomeViewModel)?.WelcomeMessage
        ?? "Employee Self-Service Portal";

    private void OnNotificationRaised(NotificationMessage message)
    {
        Dispatcher.BeginInvoke(() =>
        {
            var brush = message.Type switch
            {
                NotificationType.Success => new SolidColorBrush(Color.FromRgb(0x48, 0xBB, 0x78)),
                NotificationType.Error   => new SolidColorBrush(Color.FromRgb(0xF5, 0x65, 0x65)),
                NotificationType.Warning => new SolidColorBrush(Color.FromRgb(0xED, 0x89, 0x36)),
                NotificationType.Info    => new SolidColorBrush(Color.FromRgb(0x0D, 0x99, 0xFF)),
                _                        => Brushes.Gray
            };

            var toast = new ESSToastItem(message.Title, message.Message, brush);
            ActiveToasts.Add(toast);

            Task.Delay(5000).ContinueWith(_ =>
                Dispatcher.BeginInvoke(() => ActiveToasts.Remove(toast)));
        });
    }

    private void SignOut_Click(object sender, RoutedEventArgs e)
    {
        // Clear session and restart application to the login screen
        System.Windows.Application.Current.Shutdown(0);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _notificationService.NotificationRaised -= OnNotificationRaised;
    }
}

/// <summary>Toast item for ESS shell notification overlay.</summary>
public sealed class ESSToastItem
{
    public string Title { get; }
    public string Message { get; }
    public Brush TypeBrush { get; }

    public ESSToastItem(string title, string message, Brush typeBrush)
    {
        Title = title;
        Message = message;
        TypeBrush = typeBrush;
    }
}
