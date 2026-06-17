using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using MediatR;

namespace FijiPayroll.Application.Features.Compliance.Commands;

/// <summary>
/// Result model returning the generated compliance pack details.
/// </summary>
public sealed record EvidencePackResultModel(
    Guid CorrelationId,
    byte[] PdfBytes,
    string JsonContent,
    byte[] ZipBytes
);

/// <summary>
/// MediatR Command to generate an immutable compliance evidence pack for a payroll run.
/// </summary>
public sealed record GenerateEvidencePackCommand(
    int PayrollRunId,
    string RequestedBy
) : IRequest<Result<EvidencePackResultModel>>;

/// <summary>
/// Command Handler for <see cref="GenerateEvidencePackCommand"/>.
/// </summary>
public sealed class GenerateEvidencePackCommandHandler : IRequestHandler<GenerateEvidencePackCommand, Result<EvidencePackResultModel>>
{
    private readonly IEvidencePackGeneratorService _generatorService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateEvidencePackCommandHandler"/> class.
    /// </summary>
    public GenerateEvidencePackCommandHandler(
        IEvidencePackGeneratorService generatorService,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _generatorService = generatorService ?? throw new ArgumentNullException(nameof(generatorService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
    }

    /// <inheritdoc/>
    public async Task<Result<EvidencePackResultModel>> Handle(GenerateEvidencePackCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch the payroll run aggregate to enforce security boundaries
        var run = await _unitOfWork.PayrollRuns.GetByIdAsync(request.PayrollRunId, cancellationToken);
        if (run == null)
        {
            return Result<EvidencePackResultModel>.Failure("Payroll run not found.");
        }

        // Enforce strict multi-tenant isolation check
        int currentCompanyId = _tenantProvider.GetCurrentCompanyId();
        if (run.CompanyId != currentCompanyId)
        {
            return Result<EvidencePackResultModel>.Failure("Unauthorized context access: payroll run belongs to another company.");
        }

        try
        {
            // 2. Generate the transient EvidencePack aggregate
            var pack = await _generatorService.GenerateEvidencePackAsync(
                currentCompanyId,
                request.PayrollRunId,
                request.RequestedBy,
                cancellationToken);

            // 3. Serialize the pack to JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            string jsonContent = JsonSerializer.Serialize(pack, options);

            // 4. Generate the ZIP archive containing the JSON, PDF, manifests and reports
            byte[] zipBytes = await _generatorService.GenerateEvidenceZipArchiveAsync(pack, cancellationToken);

            // 5. Render the standalone Executive Summary PDF from the zip package or by calling SimplePdfGenerator
            // In our architecture, the ZIP contains executive_summary.pdf. We can extract it or reconstruct it.
            // Let's reconstruct or retrieve it using a direct call, or we can look it up from the archive.
            // Since we need to return PdfBytes in the CQRS result model, we'll reconstruct it deterministically.
            // To ensure 100% byte consistency, we use the exact same PDF generator call used in the ZIP packaging.
            // Let's assume the SimplePdfGenerator can render it directly.
            // We'll write a simple internal method or resolve SimplePdfGenerator inside the service.
            // In our design, we can extract the PDF bytes from the ZIP or implement PDF generation. Let's make sure it's fully populated.
            // We'll define a simple PDF generator dependency in the service or call a helper.
            // Let's retrieve it from the zip or generate it.
            
            // To keep things super clean and decoupled, let's extract the pdf from the ZIP archive
            // or we can generate it directly if we have the PDF generator instance.
            // Let's just generate the PDF directly. Since SimplePdfGenerator is in the infrastructure layer,
            // we will let the service handle PDF generation and make sure we can access it.
            // Let's extract executive_summary.pdf from the zip bytes, or add a PDF generation method.
            // Since we are zipping it inside the service, we can get it from the service or ZIP.
            // Let's get it from the ZIP for perfect byte-level parity!
            byte[] pdfBytes = ExtractFileFromZip(zipBytes, "executive_summary.pdf");

            var result = new EvidencePackResultModel(
                CorrelationId: pack.CorrelationId,
                PdfBytes: pdfBytes,
                JsonContent: jsonContent,
                ZipBytes: zipBytes
            );

            return Result<EvidencePackResultModel>.Success(result);
        }
        catch (InvalidOperationException ex)
        {
            return Result<EvidencePackResultModel>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<EvidencePackResultModel>.Failure($"Failed to generate compliance evidence pack: {ex.Message}");
        }
    }

    private static byte[] ExtractFileFromZip(byte[] zipBytes, string fileName)
    {
        using var zipStream = new System.IO.MemoryStream(zipBytes);
        using var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);
        var entry = archive.GetEntry(fileName);
        if (entry == null)
        {
            throw new System.IO.FileNotFoundException($"Could not extract {fileName} from evidence archive.");
        }

        using var entryStream = entry.Open();
        using var ms = new System.IO.MemoryStream();
        entryStream.CopyTo(ms);
        return ms.ToArray();
    }
}
