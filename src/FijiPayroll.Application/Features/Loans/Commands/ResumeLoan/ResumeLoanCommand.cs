using FijiPayroll.Application.Common.Behaviours;
using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Loans.Commands.ResumeLoan;

/// <summary>
/// Command to resume repayments on a suspended employee loan.
/// </summary>
public sealed record ResumeLoanCommand(
    int CompanyId,
    int LoanId
) : IRequest<Result<Unit>>, ITransactional, IRequirePermission
{
    public string Permission => PermissionConstants.LoansManage;
}

/// <summary>
/// Handler for <see cref="ResumeLoanCommand"/>.
/// </summary>
public sealed class ResumeLoanCommandHandler : IRequestHandler<ResumeLoanCommand, Result<Unit>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<ResumeLoanCommandHandler> _logger;

    /// <summary>Initializes dependencies.</summary>
    public ResumeLoanCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeService dateTime,
        ILogger<ResumeLoanCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<Unit>> Handle(ResumeLoanCommand request, CancellationToken cancellationToken)
    {
        // 1. Permission check
        if (!_currentUser.HasPermission(PermissionConstants.LoansManage))
        {
            throw new ForbiddenAccessException(PermissionConstants.LoansManage);
        }

        // 2. Company access check
        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<Unit>.Failure("Forbidden: You do not have access to this company.");
        }

        // 3. Load loan
        var loan = await _unitOfWork.Loans.GetByIdAsync(request.LoanId, cancellationToken);
        if (loan == null)
        {
            return Result<Unit>.Failure($"Loan with ID {request.LoanId} was not found.");
        }

        if (loan.CompanyId != request.CompanyId)
        {
            return Result<Unit>.Failure("Loan company mismatch.");
        }

        // 4. Perform resume
        try
        {
            loan.Resume();
            loan.ModifiedBy = _currentUser.Username;
            loan.ModifiedAt = _dateTime.UtcNow;
        }
        catch (Exception ex) when (ex is FijiPayroll.Domain.Exceptions.DomainException)
        {
            _logger.LogWarning(ex, "Domain exception trying to resume loan {LoanId}.", request.LoanId);
            return Result<Unit>.Failure(ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Loan {LoanId} was resumed by user {User}", request.LoanId, _currentUser.Username);
        return Result<Unit>.Success(Unit.Value);
    }
}
