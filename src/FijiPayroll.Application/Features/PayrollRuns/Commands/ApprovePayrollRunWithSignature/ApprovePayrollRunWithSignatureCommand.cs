using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Commands.ApprovePayrollRunWithSignature;

public sealed record ApprovePayrollRunWithSignatureCommand(
    int PayrollRunId,
    string CertificateThumbprint,
    string DigitalSignature,
    string Machine,
    string Ip,
    string CorrelationId
) : IRequest<Result>;

public sealed class ApprovePayrollRunWithSignatureCommandHandler : IRequestHandler<ApprovePayrollRunWithSignatureCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDigitalSignatureService _digitalSignatureService;

    public ApprovePayrollRunWithSignatureCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IDigitalSignatureService digitalSignatureService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _digitalSignatureService = digitalSignatureService;
    }

    public async Task<Result> Handle(ApprovePayrollRunWithSignatureCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsApprove))
        {
            return Result.Failure("Forbidden: You do not have permission to approve payroll runs.");
        }

        var run = await _unitOfWork.PayrollRuns.GetByIdAsync(request.PayrollRunId, cancellationToken);
        if (run == null)
        {
            return Result.Failure($"Payroll run with ID {request.PayrollRunId} was not found.");
        }

        if (!_currentUser.HasCompanyAccess(run.CompanyId))
        {
            return Result.Failure("Forbidden: You do not have access to this company's payroll runs.");
        }

        if (string.IsNullOrWhiteSpace(run.SnapshotHash))
        {
            return Result.Failure("Cannot approve payroll run: it has no calculated snapshot hash.");
        }

        try
        {
            // Verify signature using the digital signature service
            bool isVerified = _digitalSignatureService.VerifySignature(run.SnapshotHash, request.DigitalSignature);
            if (!isVerified)
            {
                return Result.Failure("Verification Failed: Digital signature is invalid for the payroll run's snapshot hash.");
            }

            run.Approve(
                _currentUser.Username,
                "Payroll Officer",
                request.Machine,
                request.Ip,
                run.SnapshotHash,
                request.DigitalSignature,
                request.CertificateThumbprint,
                request.CorrelationId
            );

            _unitOfWork.PayrollRuns.Update(run);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Approval failed: {ex.Message}");
        }
    }
}
