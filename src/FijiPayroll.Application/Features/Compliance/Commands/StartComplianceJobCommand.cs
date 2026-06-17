using System;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using MediatR;

namespace FijiPayroll.Application.Features.Compliance.Commands;

/// <summary>
/// CQRS Command to schedule and start a compliance background job.
/// </summary>
public sealed record StartComplianceJobCommand(int CompanyId, string JobType) : IRequest<Result<int>>;

/// <summary>
/// Handles StartComplianceJobCommand.
/// </summary>
public sealed class StartComplianceJobCommandHandler : IRequestHandler<StartComplianceJobCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartComplianceJobCommandHandler"/> class.
    /// </summary>
    public StartComplianceJobCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc/>
    public async Task<Result<int>> Handle(StartComplianceJobCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var job = ComplianceJob.Create(request.CompanyId, request.JobType);
            await _unitOfWork.Compliance.AddJobAsync(job, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // In production, the background processing channel will pick up this job and execute it.
            // The command returns the newly created job ID.
            return Result<int>.Success(job.Id);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Failed to start compliance job: {ex.Message}");
        }
    }
}
