using FijiPayroll.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FijiPayroll.WPF.Views;

/// <summary>
/// Interaction logic for AdminView.xaml.
/// </summary>
public partial class AdminView : UserControl
{
    public AdminView()
    {
        InitializeComponent();
        DataContextChanged += AdminView_DataContextChanged;
    }

    private void AdminView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is AdminViewModel oldVm)
        {
            oldVm.PropertyChanged -= Vm_PropertyChanged;
        }
        if (e.NewValue is AdminViewModel newVm)
        {
            newVm.PropertyChanged += Vm_PropertyChanged;
            // Sync initial state if any
            NewUserPasswordBox.Password = newVm.NewPassword;
        }
    }

    private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AdminViewModel.NewPassword))
        {
            if (DataContext is AdminViewModel vm && NewUserPasswordBox.Password != vm.NewPassword)
            {
                NewUserPasswordBox.Password = vm.NewPassword ?? string.Empty;
            }
        }
    }

    private void NewUserPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminViewModel vm)
        {
            vm.NewPassword = NewUserPasswordBox.Password;
        }
    }
}
