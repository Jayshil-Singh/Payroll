using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Features.Lookups.Commands.ArchiveLookup;
using FijiPayroll.Application.Features.Lookups.Commands.CreateLookup;
using FijiPayroll.Application.Features.Lookups.Commands.UpdateLookup;
using FijiPayroll.Application.Features.Lookups.Queries.GetLookups;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.WPF.Services;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel managing polymorphic reference data lookups (Departments, Banks, positions, etc.).
/// </summary>
public sealed partial class MasterLookupManagerViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly int _companyId = 1; // Default company context

    [ObservableProperty]
    private string _selectedCategory = "DEPARTMENTS";

    [ObservableProperty]
    private MasterLookup? _selectedLookup;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private DateTime _effectiveFrom = DateTime.Today;

    [ObservableProperty]
    private DateTime _effectiveTo = DateTime.Today.AddYears(10);

    [ObservableProperty]
    private int? _parentId;

    [ObservableProperty]
    private int _displayOrder;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private string _archiveReason = string.Empty;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Gets the list of available categories.</summary>
    public ObservableCollection<string> Categories { get; } = new()
    {
        "DEPARTMENTS",
        "BANKS",
        "BRANCHES",
        "POSITIONS",
        "COST_CENTERS"
    };

    /// <summary>Gets the loaded lookups for the selected category.</summary>
    public ObservableCollection<MasterLookup> Lookups { get; } = new();

    /// <summary>Initializes dependencies.</summary>
    public MasterLookupManagerViewModel(
        IMediator mediator,
        INotificationService notificationService,
        IDialogService dialogService)
    {
        _mediator = mediator;
        _notificationService = notificationService;
        _dialogService = dialogService;

        LoadLookupsCommand = new AsyncRelayCommand(LoadLookupsAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ArchiveCommand = new AsyncRelayCommand(ArchiveAsync, () => SelectedLookup != null && !SelectedLookup.IsArchived);
        ClearFormCommand = new RelayCommand(ClearForm);

        // Load initially
        _ = LoadLookupsAsync();
    }

    /// <summary>Gets the load lookups command.</summary>
    public IAsyncRelayCommand LoadLookupsCommand { get; }

    /// <summary>Gets the save command.</summary>
    public IAsyncRelayCommand SaveCommand { get; }

    /// <summary>Gets the archive command.</summary>
    public IAsyncRelayCommand ArchiveCommand { get; }

    /// <summary>Gets the clear form command.</summary>
    public IRelayCommand ClearFormCommand { get; }

    partial void OnSelectedCategoryChanged(string value)
    {
        _ = LoadLookupsAsync();
        ClearForm();
    }

    partial void OnSelectedLookupChanged(MasterLookup? value)
    {
        if (value != null)
        {
            Code = value.Code;
            Name = value.Name;
            EffectiveFrom = value.EffectiveFrom;
            EffectiveTo = value.EffectiveTo;
            ParentId = value.ParentId;
            DisplayOrder = value.DisplayOrder;
            IsActive = value.IsActive;
            IsEditing = true;
            ArchiveReason = value.ArchiveReason ?? string.Empty;
        }
        else
        {
            ClearForm();
        }
        ArchiveCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadLookupsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Lookups.Clear();

        try
        {
            var result = await _mediator.Send(new GetLookupsQuery(SelectedCategory));
            if (result.IsSuccess)
            {
                foreach (var item in result.Value)
                {
                    Lookups.Add(item);
                }
            }
            else
            {
                ErrorMessage = result.Error;
                _notificationService.Error(result.Error, "Failed to Load Lookups");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _notificationService.Error(ex.Message, "Error Loading Lookups");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            _notificationService.Warning("Code is required.");
            return;
        }
        if (string.IsNullOrWhiteSpace(Name))
        {
            _notificationService.Warning("Name is required.");
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            if (IsEditing && SelectedLookup != null)
            {
                var cmd = new UpdateLookupCommand(
                    Id: SelectedLookup.Id,
                    CompanyId: _companyId,
                    Name: Name,
                    EffectiveFrom: EffectiveFrom,
                    EffectiveTo: EffectiveTo,
                    ParentId: ParentId,
                    DisplayOrder: DisplayOrder,
                    IsActive: IsActive
                );

                var result = await _mediator.Send(cmd);
                if (result.IsSuccess)
                {
                    _notificationService.Success("Lookup updated successfully.");
                    await LoadLookupsAsync();
                    ClearForm();
                }
                else
                {
                    _notificationService.Error(result.Error, "Failed to Update");
                }
            }
            else
            {
                var cmd = new CreateLookupCommand(
                    CompanyId: _companyId,
                    Category: SelectedCategory,
                    Code: Code,
                    Name: Name,
                    EffectiveFrom: EffectiveFrom,
                    EffectiveTo: EffectiveTo,
                    ParentId: ParentId,
                    DisplayOrder: DisplayOrder,
                    IsActive: IsActive
                );

                var result = await _mediator.Send(cmd);
                if (result.IsSuccess)
                {
                    _notificationService.Success("Lookup created successfully.");
                    await LoadLookupsAsync();
                    ClearForm();
                }
                else
                {
                    _notificationService.Error(result.Error, "Failed to Create");
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error Saving Lookup");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ArchiveAsync()
    {
        if (SelectedLookup == null) return;

        if (string.IsNullOrWhiteSpace(ArchiveReason))
        {
            _notificationService.Warning("Archive reason is required.");
            return;
        }

        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Archive Lookup",
            $"Are you sure you want to archive '{SelectedLookup.Name}' ({SelectedLookup.Code})? This action is permanent.");

        if (!confirmed) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var cmd = new ArchiveLookupCommand(
                Id: SelectedLookup.Id,
                CompanyId: _companyId,
                Reason: ArchiveReason
            );

            var result = await _mediator.Send(cmd);
            if (result.IsSuccess)
            {
                _notificationService.Success("Lookup archived successfully.");
                await LoadLookupsAsync();
                ClearForm();
            }
            else
            {
                _notificationService.Error(result.Error, "Failed to Archive");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error(ex.Message, "Error Archiving Lookup");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClearForm()
    {
        SelectedLookup = null;
        Code = string.Empty;
        Name = string.Empty;
        EffectiveFrom = DateTime.Today;
        EffectiveTo = DateTime.Today.AddYears(10);
        ParentId = null;
        DisplayOrder = 0;
        IsActive = true;
        ArchiveReason = string.Empty;
        IsEditing = false;
        ArchiveCommand.NotifyCanExecuteChanged();
    }
}
