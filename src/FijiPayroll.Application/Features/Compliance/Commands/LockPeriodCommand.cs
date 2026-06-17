using System;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using MediatR;

namespace FijiPayroll.Application.Features.Compliance.Commands;

/// <summary>
/// CQRS Command to transition a compliance period state to Locked or Open.
/// </summary>
public sealed record LockPeriodCommand(int PeriodId, bool Lock) : IRequest<Result>;

/// <summary>
/// Handles LockPeriodCommand.
/// </summary>
public sealed class LockPeriodCommandHandler : IRequestHandler<LockPeriodCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="LockPeriodCommandHandler"/> class.
    /// </summary>
    public LockPeriodCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(LockPeriodCommand request, CancellationToken cancellationToken)
    {
        var period = await _unitOfWork.Compliance.GetPeriodByIdAsync(request.PeriodId, cancellationToken);
        if (period == null)
        {
            return Result.Failure($"Compliance period with ID {request.PeriodId} not found.");
        }

        try
        {
            if (request.Lock)
            {
                period.Lock();
            }
            else
            {
                period.Unlock();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to change period state: {ex.Message}");
        }
    }
}
