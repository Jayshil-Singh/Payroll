using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Domain.Entities.Payroll;

namespace FijiPayroll.Infrastructure.Services.ComplianceEvidence;

/// <summary>
/// Packages the compliance evidence artifacts into a structured ZIP package using resource-safe streams and rented buffers.
/// </summary>
public sealed class FileArchiveManager
{
    /// <summary>
    /// Bundles all evidence pack elements into a single compressed ZIP archive.
    /// </summary>
    public async Task<byte[]> CreateEvidenceZipAsync(
        Domain.Entities.Payroll.EvidencePack pack,
        byte[] executiveSummaryPdf,
        IReadOnlyList<ReportSnapshotResult> snapshots,
        CancellationToken cancellationToken = default)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (executiveSummaryPdf == null) throw new ArgumentNullException(nameof(executiveSummaryPdf));
        if (snapshots == null) throw new ArgumentNullException(nameof(snapshots));

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        byte[] buffer = ArrayPool<byte>.Shared.Rent(8192); // Rent 8KB buffer
        try
        {
            using var outputMs = new MemoryStream();
            using (var archive = new ZipArchive(outputMs, ZipArchiveMode.Create, leaveOpen: true))
            {
                // 1. compliance_bundle.json
                string fullJson = JsonSerializer.Serialize(pack, options);
                await AddStringEntryAsync(archive, "compliance_bundle.json", fullJson, buffer, cancellationToken);

                // 2. executive_summary.pdf
                await AddBytesEntryAsync(archive, "executive_summary.pdf", executiveSummaryPdf, buffer, cancellationToken);

                // 3. ledger_manifest.json
                string ledgerManifestJson = JsonSerializer.Serialize(pack.LedgerIntegrity, options);
                await AddStringEntryAsync(archive, "ledger_manifest.json", ledgerManifestJson, buffer, cancellationToken);

                // 4. reports/*.pdf
                foreach (var report in snapshots)
                {
                    string entryName = $"reports/{report.Snapshot.ReportName}.pdf";
                    await AddBytesEntryAsync(archive, entryName, report.PdfBytes, buffer, cancellationToken);
                }

                // 5. traceability_summary.json
                string traceabilityJson = JsonSerializer.Serialize(pack.Traceability, options);
                await AddStringEntryAsync(archive, "traceability_summary.json", traceabilityJson, buffer, cancellationToken);
            }

            return outputMs.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task AddStringEntryAsync(
        ZipArchive archive,
        string entryName,
        string content,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        entry.LastWriteTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using var entryStream = entry.Open();
        byte[] contentBytes = Encoding.UTF8.GetBytes(content);
        using var sourceStream = new MemoryStream(contentBytes);
        await CopyStreamChunkedAsync(sourceStream, entryStream, buffer, cancellationToken);
    }

    private static async Task AddBytesEntryAsync(
        ZipArchive archive,
        string entryName,
        byte[] bytes,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        entry.LastWriteTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using var entryStream = entry.Open();
        using var sourceStream = new MemoryStream(bytes);
        await CopyStreamChunkedAsync(sourceStream, entryStream, buffer, cancellationToken);
    }

    private static async Task CopyStreamChunkedAsync(
        Stream source,
        Stream destination,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }
    }
}
