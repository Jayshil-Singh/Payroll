using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.SDK.Interfaces;

/// <summary>
/// Defines the contract for exporting tabular datasets to Excel spreadsheets using ClosedXML.
/// </summary>
public interface IExportProvider
{
    /// <summary>
    /// Exports a DataSet to an Excel sheet stream.
    /// </summary>
    /// <param name="dataSet">The source dataset to export.</param>
    /// <param name="destinationStream">The target output stream to write the Excel file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExportExcelAsync(DataSet dataSet, Stream destinationStream, CancellationToken cancellationToken = default);
}
