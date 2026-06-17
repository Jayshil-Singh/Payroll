using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using MediatR;

namespace FijiPayroll.Application.Features.Compliance.Commands;

/// <summary>
/// CQRS Command to request an override of compliance limits or rules.
/// </summary>
public sealed record SubmitOverrideCommand(
    int CompanyId,
    string ActionType,
    decimal Value,
    ApprovalRole Role,
    string User
) : IRequest<Result>;

/// <summary>
/// Handles SubmitOverrideCommand.
/// </summary>
public sealed class SubmitOverrideCommandHandler : IRequestHandler<SubmitOverrideCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitOverrideCommandHandler"/> class.
    /// </summary>
    public SubmitOverrideCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(SubmitOverrideCommand request, CancellationToken cancellationToken)
    {
        // Fetch approval matrix config for the role and action
        var matrix = await _unitOfWork.Compliance.GetApprovalMatrixAsync(
            request.CompanyId,
            request.Role,
            request.ActionType,
            cancellationToken);

        string payloadJson = JsonSerializer.Serialize(new
        {
            request.Role,
            request.ActionType,
            request.Value,
            request.User,
            MatrixId = matrix?.Id,
            MatrixMin = matrix?.MinThreshold,
            MatrixMax = matrix?.MaxThreshold
        });

        bool approved = false;
        string message;

        if (matrix == null)
        {
            // If no rule matrix is configured, default to reject (fail-safe security model)
            message = $"No approval matrix configuration found for role '{request.Role}' and action '{request.ActionType}'.";
        }
        else if (request.Value >= matrix.MinThreshold && request.Value <= matrix.MaxThreshold)
        {
            approved = true;
            message = "Override approved within matrix thresholds.";
        }
        else
        {
            message = $"Override value {request.Value:N2} lies outside authorized matrix bounds ({matrix.MinThreshold:N2} - {matrix.MaxThreshold:N2}).";
        }

        // Log the decision in the Compliance Event Store
        var ev = ComplianceEvent.Create(
            correlationId: Guid.NewGuid(),
            companyId: request.CompanyId,
            eventType: approved ? "OverrideApproved" : "OverrideRejected",
            user: request.User,
            machine: Environment.MachineName,
            applicationVersion: "1.0.0",
            payloadJson: payloadJson
        );

        await _unitOfWork.Compliance.AddComplianceEventAsync(ev, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return approved ? Result.Success() : Result.Failure(message);
    }
}
