using FijiPayroll.Domain.Entities.Common;

namespace FijiPayroll.Domain.Entities.Audit;

/// <summary>
/// Domain entity auditing every export execution.
/// </summary>
public sealed class ExportHistory : BaseEntity
{
    private ExportHistory() { }

    public ExportHistory(
        string report,
        string user,
        DateTime date,
        string filter,
        int recordCount,
        string exportType,
        int downloadCount,
        string ipAddress,
        string generatedByVersion,
        string hash)
    {
        Report = report;
        User = user;
        Date = date;
        Filter = filter;
        RecordCount = recordCount;
        ExportType = exportType;
        DownloadCount = downloadCount;
        IPAddress = ipAddress;
        GeneratedByVersion = generatedByVersion;
        Hash = hash;
    }

    public string Report { get; private set; } = string.Empty;
    public string User { get; private set; } = string.Empty;
    public DateTime Date { get; private set; }
    public string Filter { get; private set; } = string.Empty;
    public int RecordCount { get; private set; }
    public string ExportType { get; private set; } = string.Empty;
    public int DownloadCount { get; private set; }
    public string IPAddress { get; private set; } = string.Empty;
    public string GeneratedByVersion { get; private set; } = string.Empty;
    public string Hash { get; private set; } = string.Empty;

    public void IncrementDownloadCount()
    {
        DownloadCount++;
    }
}
