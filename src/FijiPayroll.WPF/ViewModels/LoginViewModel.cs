using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Features.Auth.Commands.Login;
using FijiPayroll.Application.Features.Auth.Queries.GetCompaniesForUser;
using FijiPayroll.WPF.ViewModels.Base;
using FijiPayroll.WPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// View model backing the login view.
/// Coordinates authentication, company selection, and session establishment.
/// </summary>
public sealed partial class LoginViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IAuthSessionStore _sessionStore;
    private readonly IApplicationStateStore _stateStore;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CompanyLookupDto> _companies = new();

    [ObservableProperty]
    private CompanyLookupDto? _selectedCompany;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _showCompanySelection;

    [ObservableProperty]
    private bool _isLoadingCompanies;

    [ObservableProperty]
    private bool _isAuthenticating;

    [ObservableProperty]
    private bool _isLoginSuccess;

    /// <summary>
    /// Event triggered when the login is successful and the window should close.
    /// </summary>
    public event EventHandler? RequestClose;

    public LoginViewModel(
        IMediator mediator,
        IAuthSessionStore sessionStore,
        IApplicationStateStore stateStore)
    {
        _mediator = mediator;
        _sessionStore = sessionStore;
        _stateStore = stateStore;

        VerifyUsernameCommand = new AsyncRelayCommand(VerifyUsernameAsync);
        LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
    }

    public IAsyncRelayCommand VerifyUsernameCommand { get; }
    public IAsyncRelayCommand LoginCommand { get; }

    private bool CanLogin()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password) &&
               SelectedCompany != null &&
               !IsAuthenticating;
    }

    partial void OnUsernameChanged(string value)
    {
        // Reset state when username changes
        Companies.Clear();
        SelectedCompany = null;
        ShowCompanySelection = false;
        ErrorMessage = string.Empty;
        LoginCommand.NotifyCanExecuteChanged();
    }

    partial void OnPasswordChanged(string value)
    {
        LoginCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedCompanyChanged(CompanyLookupDto? value)
    {
        LoginCommand.NotifyCanExecuteChanged();
    }

    private async Task VerifyUsernameAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Username is required.";
            return;
        }

        ErrorMessage = string.Empty;
        IsLoadingCompanies = true;

        try
        {
            var query = new GetCompaniesForUserQuery(Username);
            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Value != null && result.Value.Any())
            {
                Companies.Clear();
                foreach (var comp in result.Value)
                {
                    Companies.Add(comp);
                }

                ShowCompanySelection = true;
                if (Companies.Count == 1)
                {
                    SelectedCompany = Companies[0];
                }
            }
            else
            {
                ErrorMessage = "No companies found for this user.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error resolving companies: {ex.Message}";
        }
        finally
        {
            IsLoadingCompanies = false;
            LoginCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task LoginAsync()
    {
        if (SelectedCompany == null)
        {
            ErrorMessage = "Please select a company.";
            return;
        }

        ErrorMessage = string.Empty;
        IsAuthenticating = true;
        LoginCommand.NotifyCanExecuteChanged();

        try
        {
            var command = new FijiPayroll.Application.Features.Auth.Commands.Login.LoginCommand(Username, Password, SelectedCompany.Id);
            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.Value != null)
            {
                // Set the current company ID in state store first so downstream services resolve it
                _stateStore.CurrentCompanyId = SelectedCompany.Id;

                // Establish the authenticated session
                _sessionStore.Establish(result.Value);

                IsLoginSuccess = true;
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ErrorMessage = result.Error ?? "Login failed.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Authentication error: {ex.Message}";
        }
        finally
        {
            IsAuthenticating = false;
            LoginCommand.NotifyCanExecuteChanged();
        }
    }
}
