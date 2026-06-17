using System;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using MediatR;

namespace FijiPayroll.Application.Features.Compliance.Queries;

/// <summary>
/// MediatR Query to cryptographically verify a signed compliance evidence pack zip.
/// </summary>
public sealed record VerifyEvidencePackQuery(byte[] ZipBytes) : IRequest<Result>;

/// <summary>
/// Handler for <see cref="VerifyEvidencePackQuery"/>.
/// </summary>
public sealed class VerifyEvidencePackQueryHandler : IRequestHandler<VerifyEvidencePackQuery, Result>
{
    private readonly ISignatureVerifierService _verifierService;

    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyEvidencePackQueryHandler"/> class.
    /// </summary>
    public VerifyEvidencePackQueryHandler(ISignatureVerifierService verifierService)
    {
        _verifierService = verifierService ?? throw new ArgumentNullException(nameof(verifierService));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(VerifyEvidencePackQuery request, CancellationToken cancellationToken)
    {
        if (request.ZipBytes == null || request.ZipBytes.Length == 0)
        {
            return Result.Failure("Invalid input: ZIP bytes are empty.");
        }

        try
        {
            await _verifierService.VerifyEvidencePackSignatureAsync(request.ZipBytes, cancellationToken);
            return Result.Success();
        }
        catch (EvidencePackTamperedException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Verification failed: {ex.Message}");
        }
    }
}
