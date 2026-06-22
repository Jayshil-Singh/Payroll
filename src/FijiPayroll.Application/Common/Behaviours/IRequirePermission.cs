namespace FijiPayroll.Application.Common.Behaviours;

/// <summary>
/// Marker interface indicating that a command request requires a permission check.
/// </summary>
public interface IRequirePermission
{
    /// <summary>Gets the permission code required to execute this request.</summary>
    string Permission { get; }
}
