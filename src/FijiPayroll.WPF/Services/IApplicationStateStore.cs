using System.ComponentModel;
using System.Threading.Tasks;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Interface representing the central thread-safe application state container.
/// Supports snapshot/restore, freeze-barrier awareness, and atomic persistence.
/// </summary>
public interface IApplicationStateStore : INotifyPropertyChanged
{
    /// <summary>Gets or sets the currently active Company ID.</summary>
    int CurrentCompanyId { get; set; }

    /// <summary>Gets or sets the active Payroll Run ID.</summary>
    int? CurrentPayrollRunId { get; set; }

    /// <summary>Gets or sets the selected Financial Year.</summary>
    int SelectedFinancialYear { get; set; }

    /// <summary>Gets or sets the currently selected Employee ID.</summary>
    int? SelectedEmployeeId { get; set; }

    /// <summary>Gets or sets whether the login username should be remembered.</summary>
    bool RememberMe { get; set; }

    /// <summary>Gets or sets the remembered username string.</summary>
    string RememberedUsername { get; set; }

    /// <summary>Clears the entire application state to default values.</summary>
    void Clear();

    /// <summary>
    /// Takes an immutable snapshot of the current application state.
    /// Thread-safe; respects the SnapshotCoordinator freeze barrier.
    /// </summary>
    ApplicationStateData TakeSnapshot();

    /// <summary>
    /// Restores application state from a previously taken snapshot.
    /// Validates snapshot before applying.
    /// </summary>
    void RestoreSnapshot(ApplicationStateData snapshot);

    /// <summary>
    /// Persists the current state to disk atomically.
    /// </summary>
    Task PersistCurrentStateAsync();

    /// <summary>
    /// Loads and applies the last persisted state from disk.
    /// Should be called during application startup.
    /// </summary>
    Task LoadPersistedStateAsync();
}
