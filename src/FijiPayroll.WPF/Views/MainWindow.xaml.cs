using FijiPayroll.WPF.Services;
using FijiPayroll.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;

    // Callbacks for the custom modal overlay dialogs
    private Action? _dialogOkAction;
    private Action<bool>? _dialogConfirmAction;

    /// <summary>
    /// Gets the list of active floating toast notifications.
    /// </summary>
    public ObservableCollection<ToastNotificationItem> ActiveToasts { get; } = new();

    public MainWindow(
        MainViewModel viewModel,
        INavigationService navigationService,
        INotificationService notificationService,
        IDialogService dialogService,
        ILoadingService loadingService)
    {
        InitializeComponent();
        
        DataContext = viewModel;
        _notificationService = notificationService;
        _dialogService = dialogService;

        // Bind items control directly to active toast collection
        ToastContainer.ItemsSource = ActiveToasts;

        // Subscribe to global UI events
        _notificationService.NotificationRaised += OnNotificationRaised;
        _dialogService.MessageRequested += OnMessageRequested;
        _dialogService.ConfirmationRequested += OnConfirmationRequested;
    }

    private void OnNotificationRaised(NotificationMessage message)
    {
        // Ensure UI updates happen on the WPF dispatcher thread
        Dispatcher.BeginInvoke(() =>
        {
            var brush = message.Type switch
            {
                NotificationType.Success => new SolidColorBrush(Color.FromRgb(0x48, 0xBB, 0x78)),
                NotificationType.Error => new SolidColorBrush(Color.FromRgb(0xF5, 0x65, 0x65)),
                NotificationType.Warning => new SolidColorBrush(Color.FromRgb(0xED, 0x89, 0x36)),
                NotificationType.Info => new SolidColorBrush(Color.FromRgb(0x0D, 0x99, 0xFF)),
                _ => Brushes.Gray
            };

            var toast = new ToastNotificationItem(message.Title, message.Message, brush);
            ActiveToasts.Add(toast);

            // Automatically dismiss toast after 5 seconds
            Task.Delay(5000).ContinueWith(_ =>
            {
                Dispatcher.BeginInvoke(() => ActiveToasts.Remove(toast));
            });
        });
    }

    private void OnMessageRequested(string title, string message, Action callback)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _dialogOkAction = callback;
            _dialogConfirmAction = null;

            DialogTitle.Text = title;
            DialogMessage.Text = message;
            DialogCancelButton.Visibility = Visibility.Collapsed;
            DialogOverlay.Visibility = Visibility.Visible;
        });
    }

    private void OnConfirmationRequested(string title, string message, Action<bool> callback)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _dialogOkAction = null;
            _dialogConfirmAction = callback;

            DialogTitle.Text = title;
            DialogMessage.Text = message;
            DialogCancelButton.Visibility = Visibility.Visible;
            DialogOverlay.Visibility = Visibility.Visible;
        });
    }

    private void DialogOk_Click(object sender, RoutedEventArgs e)
    {
        DialogOverlay.Visibility = Visibility.Collapsed;

        if (_dialogOkAction != null)
        {
            var action = _dialogOkAction;
            _dialogOkAction = null;
            action.Invoke();
        }
        else if (_dialogConfirmAction != null)
        {
            var action = _dialogConfirmAction;
            _dialogConfirmAction = null;
            action.Invoke(true);
        }
    }

    private void DialogCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogOverlay.Visibility = Visibility.Collapsed;

        if (_dialogConfirmAction != null)
        {
            var action = _dialogConfirmAction;
            _dialogConfirmAction = null;
            action.Invoke(false);
        }
    }

    private void DismissToast_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ToastNotificationItem item)
        {
            ActiveToasts.Remove(item);
        }
    }

    private void DialogOverlay_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Prevent clicking background from closing dialog accidentally, keeping it modal
        e.Handled = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        
        // Unsubscribe from singleton services to avoid leaks
        _notificationService.NotificationRaised -= OnNotificationRaised;
        _dialogService.MessageRequested -= OnMessageRequested;
        _dialogService.ConfirmationRequested -= OnConfirmationRequested;
    }
}

/// <summary>
/// Container holding visual state and data for individual active toast cards.
/// </summary>
public sealed class ToastNotificationItem
{
    public string Title { get; }
    public string Message { get; }
    public Brush TypeBrush { get; }

    public ToastNotificationItem(string title, string message, Brush typeBrush)
    {
        Title = title;
        Message = message;
        TypeBrush = typeBrush;
    }
}
