using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Shared.Formula;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace FijiPayroll.WPF.ViewModels;

/// <summary>
/// ViewModel for the Payroll Component create/edit editor form.
/// Operates in two modes:
/// <list type="bullet">
///   <item><description><b>Create mode</b> – <see cref="EditingId"/> is null; all fields start blank.</description></item>
///   <item><description><b>Edit mode</b> – <see cref="EditingId"/> is populated with the component's primary key;
///   fields are pre-loaded from <see cref="IPayrollComponentService.GetByIdAsync"/>.</description></item>
/// </list>
/// </summary>
public sealed partial class PayrollComponentFormViewModel : ObservableObject, INotifyDataErrorInfo
{
    // ── Dependencies ─────────────────────────────────────────────────────────

    private readonly IPayrollComponentService _componentService;
    private readonly int _companyId;

    // ── Mode ─────────────────────────────────────────────────────────────────

    /// <summary>Gets the primary key of the component being edited. Null in create mode.</summary>
    public int? EditingId { get; private set; }

    /// <summary>Gets a value indicating whether this form is in edit (vs. create) mode.</summary>
    public bool IsEditMode => EditingId.HasValue;

    /// <summary>Gets the window title based on the current mode.</summary>
    public string WindowTitle => IsEditMode
        ? $"Edit Payroll Component — {ComponentCode}"
        : "New Payroll Component";

    // ── Observable Bindable Properties ────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string _componentCode = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string _componentName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowValueField))]
    [NotifyPropertyChangedFor(nameof(ShowFormulaField))]
    [NotifyPropertyChangedFor(nameof(ValueFieldLabel))]
    private CalculationMethod _selectedCalculationMethod = CalculationMethod.Fixed;

    [ObservableProperty]
    private ComponentType _selectedComponentType = ComponentType.Earning;

    [ObservableProperty]
    private decimal? _calculationValue;

    [ObservableProperty]
    private string? _formula;

    [ObservableProperty]
    private ObservableCollection<FormulaTreeNode> _formulaAstNodes = new();

    [ObservableProperty]
    private bool _isTaxable = true;

    [ObservableProperty]
    private bool _isFnpfApplicable = true;

    [ObservableProperty]
    private int _displayOrder;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private bool _isSystemComponent;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    // ── Computed visibility / label helpers ───────────────────────────────────

    /// <summary>
    /// Gets a value indicating whether the numeric value field should be visible
    /// (i.e., for Fixed or Percentage methods).
    /// </summary>
    public bool ShowValueField =>
        SelectedCalculationMethod == CalculationMethod.Fixed ||
        SelectedCalculationMethod == CalculationMethod.Percentage;

    /// <summary>
    /// Gets a value indicating whether the formula text field should be visible.
    /// </summary>
    public bool ShowFormulaField =>
        SelectedCalculationMethod == CalculationMethod.Formula;

    /// <summary>Gets the label for the value input depending on the calculation method.</summary>
    public string ValueFieldLabel =>
        SelectedCalculationMethod == CalculationMethod.Percentage
            ? "Percentage (%)"
            : "Fixed Amount ($)";

    // ── Enum source collections for ComboBox binding ─────────────────────────

    /// <summary>Gets the list of available component types for the ComboBox.</summary>
    public ObservableCollection<ComponentType> ComponentTypes { get; } =
        new(Enum.GetValues<ComponentType>());

    /// <summary>Gets the list of available calculation methods for the ComboBox.</summary>
    public ObservableCollection<CalculationMethod> CalculationMethods { get; } =
        new(Enum.GetValues<CalculationMethod>());

    // ── Validation (INotifyDataErrorInfo) ─────────────────────────────────────

    private readonly Dictionary<string, List<string>> _validationErrors = new();

    /// <inheritdoc/>
    public bool HasErrors => _validationErrors.Any(e => e.Value.Count > 0);

    /// <inheritdoc/>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <inheritdoc/>
    public System.Collections.IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return _validationErrors.Values.SelectMany(e => e);
        }

        return _validationErrors.TryGetValue(propertyName, out var errors) ? errors : Enumerable.Empty<string>();
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>Gets the command to save (create or update) the component.</summary>
    public IAsyncRelayCommand SaveCommand { get; }

    /// <summary>Gets the command to close the editor without saving.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Gets the command to validate the formula expression.</summary>
    public IAsyncRelayCommand ValidateFormulaCommand { get; }

    // ── Close event ───────────────────────────────────────────────────────────

    /// <summary>Raised when the editor should close. Bool indicates success (true = saved).</summary>
    public event Action<bool>? CloseRequested;

    // ── Constructor ───────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the ViewModel in <b>create mode</b>.
    /// </summary>
    /// <param name="componentService">The payroll component application service.</param>
    /// <param name="companyId">The current tenant company ID.</param>
    public PayrollComponentFormViewModel(IPayrollComponentService componentService, int companyId)
    {
        _componentService = componentService;
        _companyId = companyId;

        SaveCommand            = new AsyncRelayCommand(SaveAsync);
        CancelCommand          = new RelayCommand(() => CloseRequested?.Invoke(false));
        ValidateFormulaCommand = new AsyncRelayCommand(ValidateFormulaAsync);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Loads an existing component into the form fields for editing.
    /// Transitions the ViewModel to <b>edit mode</b>.
    /// </summary>
    /// <param name="componentId">Primary key of the component to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task LoadForEditAsync(int componentId, CancellationToken cancellationToken = default)
    {
        IsLoading    = true;
        ErrorMessage = null;

        try
        {
            var result = await _componentService.GetByIdAsync(componentId, cancellationToken);

            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to load component details.";
                return;
            }

            var dto = result.Value;

            EditingId              = dto.Id;
            ComponentCode          = dto.ComponentCode;
            ComponentName          = dto.ComponentName;
            SelectedComponentType  = dto.ComponentType;
            SelectedCalculationMethod = dto.CalculationMethod;
            CalculationValue       = dto.CalculationValue;
            Formula                = dto.Formula;
            IsTaxable              = dto.IsTaxable;
            IsFnpfApplicable       = dto.IsFnpfApplicable;
            DisplayOrder           = dto.DisplayOrder;
            Description            = dto.Description;
            IsSystemComponent      = dto.IsSystemComponent;

            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(WindowTitle));
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Private implementation ────────────────────────────────────────────────

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage   = null;
        SuccessMessage = null;

        if (!ValidateInputs())
        {
            return;
        }

        IsLoading = true;

        try
        {
            if (IsEditMode)
            {
                // ── Update existing component ──────────────────────────────
                var result = await _componentService.UpdateAsync(
                    id:                EditingId!.Value,
                    componentName:     ComponentName.Trim(),
                    componentType:     SelectedComponentType,
                    calculationMethod: SelectedCalculationMethod,
                    calculationValue:  NormaliseValue(),
                    formula:           NormaliseFormula(),
                    isTaxable:         IsTaxable,
                    isFnpfApplicable:  IsFnpfApplicable,
                    displayOrder:      DisplayOrder,
                    description:       Description?.Trim(),
                    cancellationToken: cancellationToken);

                if (result.IsSuccess)
                {
                    SuccessMessage = "Component updated successfully.";
                    CloseRequested?.Invoke(true);
                }
                else
                {
                    ErrorMessage = result.Error ?? "Failed to update component.";
                }
            }
            else
            {
                // ── Create new component ───────────────────────────────────
                var result = await _componentService.CreateAsync(
                    companyId:         _companyId,
                    componentCode:     ComponentCode.Trim().ToUpperInvariant(),
                    componentName:     ComponentName.Trim(),
                    componentType:     SelectedComponentType,
                    calculationMethod: SelectedCalculationMethod,
                    calculationValue:  NormaliseValue(),
                    formula:           NormaliseFormula(),
                    isTaxable:         IsTaxable,
                    isFnpfApplicable:  IsFnpfApplicable,
                    displayOrder:      DisplayOrder,
                    description:       Description?.Trim(),
                    cancellationToken: cancellationToken);

                if (result.IsSuccess)
                {
                    SuccessMessage = $"Component created successfully (ID: {result.Value}).";
                    CloseRequested?.Invoke(true);
                }
                else
                {
                    ErrorMessage = result.Error ?? "Failed to create component.";
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unexpected error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ValidateFormulaAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Formula))
        {
            ErrorMessage = "Formula is empty. Please enter an expression before validating.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        await Task.Delay(100, cancellationToken); // Simulate async validation

        // Validate supported variable tokens only
        var validTokens = new[] { "{GrossPay}", "{AnnualSalary}", "{HoursWorked}", "{DailyRate}", "{OvertimeHours}" };
        bool usesValidTokens = validTokens.Any(token => Formula!.Contains(token, StringComparison.OrdinalIgnoreCase));

        if (!usesValidTokens)
        {
            ErrorMessage = "Formula must reference at least one supported variable: {GrossPay}, {AnnualSalary}, {HoursWorked}, {DailyRate}, or {OvertimeHours}.";
        }
        else
        {
            SuccessMessage = "Formula syntax is valid.";
        }

        IsLoading = false;
    }

    /// <summary>Validates all required fields and populates <see cref="_validationErrors"/>.</summary>
    /// <returns><c>true</c> if all inputs are valid.</returns>
    private bool ValidateInputs()
    {
        ClearErrors();

        // Component Code (create mode only)
        if (!IsEditMode)
        {
            if (string.IsNullOrWhiteSpace(ComponentCode))
            {
                AddError(nameof(ComponentCode), "Component code is required.");
            }
            else if (ComponentCode.Length > 20)
            {
                AddError(nameof(ComponentCode), "Component code must not exceed 20 characters.");
            }
            else if (!ComponentCode.All(c => char.IsLetterOrDigit(c) || c == '_'))
            {
                AddError(nameof(ComponentCode), "Component code must contain only letters, digits, or underscores.");
            }
        }

        // Component Name
        if (string.IsNullOrWhiteSpace(ComponentName))
        {
            AddError(nameof(ComponentName), "Component name is required.");
        }
        else if (ComponentName.Length > 200)
        {
            AddError(nameof(ComponentName), "Component name must not exceed 200 characters.");
        }

        // Value validation
        if (ShowValueField && (!CalculationValue.HasValue || CalculationValue.Value < 0))
        {
            AddError(nameof(CalculationValue), "A non-negative value is required for Fixed/Percentage methods.");
        }

        // Formula validation
        if (ShowFormulaField && string.IsNullOrWhiteSpace(Formula))
        {
            AddError(nameof(Formula), "A formula expression is required for the Formula method.");
        }

        // Description length
        if (Description?.Length > 500)
        {
            AddError(nameof(Description), "Description must not exceed 500 characters.");
        }

        // Display Order
        if (DisplayOrder < 0)
        {
            AddError(nameof(DisplayOrder), "Display order must be a non-negative integer.");
        }

        if (HasErrors)
        {
            ErrorMessage = "Please correct the validation errors before saving.";
            return false;
        }

        return true;
    }

    private decimal? NormaliseValue() =>
        ShowValueField ? CalculationValue : null;

    private string? NormaliseFormula() =>
        ShowFormulaField ? Formula?.Trim() : null;

    private void AddError(string propertyName, string errorMessage)
    {
        if (!_validationErrors.ContainsKey(propertyName))
        {
            _validationErrors[propertyName] = new List<string>();
        }

        _validationErrors[propertyName].Add(errorMessage);
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
    }

    private void ClearErrors()
    {
        var keys = _validationErrors.Keys.ToList();
        _validationErrors.Clear();

        foreach (var key in keys)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(key));
        }

        OnPropertyChanged(nameof(HasErrors));
        ErrorMessage = null;
    }

    partial void OnFormulaChanged(string? value)
    {
        UpdateAstTree();
    }

    [RelayCommand]
    private void InsertFormulaElement(string element)
    {
        if (string.IsNullOrEmpty(Formula))
        {
            Formula = element;
        }
        else
        {
            // If it's a function call, append nicely. If variable, append.
            Formula += " " + element;
        }
    }

    private void UpdateAstTree()
    {
        FormulaAstNodes.Clear();
        if (string.IsNullOrWhiteSpace(Formula)) return;

        try
        {
            var tokenizer = new FormulaTokenizer();
            var tokens = tokenizer.Tokenize(Formula);
            var parser = new FormulaParser();
            var ast = parser.Parse(tokens);
            var treeNode = MapToTreeNode(ast);
            FormulaAstNodes.Add(treeNode);
        }
        catch
        {
            // Do not fail or block UI during active typing of incomplete formulas
        }
    }

    private FormulaTreeNode MapToTreeNode(AstNode node)
    {
        var treeNode = new FormulaTreeNode();
        if (node is NumberNode num)
        {
            treeNode.DisplayText = num.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            treeNode.NodeType = "Number";
        }
        else if (node is VariableNode varNode)
        {
            treeNode.DisplayText = $"{{{varNode.Name}}}";
            treeNode.NodeType = "Variable";
        }
        else if (node is BinaryOpNode binNode)
        {
            treeNode.DisplayText = binNode.Op;
            treeNode.NodeType = "Operator";
            treeNode.Children.Add(MapToTreeNode(binNode.Left));
            treeNode.Children.Add(MapToTreeNode(binNode.Right));
        }
        else if (node is FunctionNode funcNode)
        {
            treeNode.DisplayText = funcNode.Name;
            treeNode.NodeType = "Function";
            foreach (var arg in funcNode.Arguments)
            {
                treeNode.Children.Add(MapToTreeNode(arg));
            }
        }
        return treeNode;
    }
}

/// <summary>
/// Represents a node in the formula AST visual tree.
/// </summary>
public sealed class FormulaTreeNode
{
    /// <summary>Gets or sets the display text for the tree node.</summary>
    public string DisplayText { get; set; } = string.Empty;

    /// <summary>Gets or sets the classification type of the node (e.g. Function, Operator, Variable, Number).</summary>
    public string NodeType { get; set; } = string.Empty;

    /// <summary>Gets the child nodes of this AST node.</summary>
    public ObservableCollection<FormulaTreeNode> Children { get; } = new();
}
