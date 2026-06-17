using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.SDK.Interfaces;

/// <summary>
/// Defines the contract for importing tabular data from Excel spreadsheets using ClosedXML.
/// </summary>
public interface IImportProvider
{
    /// <summary>
    /// Reads an Excel sheet and converts it into a DataSet.
    /// </summary>
    /// <param name="fileStream">The input file stream containing the spreadsheet.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A DataSet containing the worksheet data.</returns>
    Task<DataSet> ImportExcelAsync(Stream fileStream, CancellationToken cancellationToken = default);
}
