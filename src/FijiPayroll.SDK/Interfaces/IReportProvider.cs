using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.SDK.Interfaces;

/// <summary>
/// Defines the integration gateway for paginated reporting services (such as SQL Server Reporting Services).
/// </summary>
public interface IReportProvider
{
    /// <summary>
    /// Renders a template report with parameter variables.
    /// </summary>
    /// <param name="reportName">The name or path of the report layout definition.</param>
    /// <param name="format">The target output file format ("PDF", "Excel", "CSV", "Word").</param>
    /// <param name="parameters">Filter parameters passed to query scopes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Rendered file content bytes.</returns>
    Task<byte[]> RenderReportAsync(
        string reportName,
        string format,
        IDictionary<string, string> parameters,
        CancellationToken cancellationToken = default);
}
