using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Queries.GetLedger;

public sealed record GetLedgerQuery(int PayrollRunId) : IRequest<Result<PayrollLedger>>;

public sealed class GetLedgerQueryHandler : IRequestHandler<GetLedgerQuery, Result<PayrollLedger>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetLedgerQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PayrollLedger>> Handle(GetLedgerQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsView))
        {
            return Result<PayrollLedger>.Failure("Forbidden: You do not have permission to view payroll ledger.");
        }

        var run = await _unitOfWork.PayrollRuns.GetByIdAsync(request.PayrollRunId, cancellationToken);
        if (run == null)
        {
            return Result<PayrollLedger>.Failure($"Payroll run with ID {request.PayrollRunId} was not found.");
        }

        if (!_currentUser.HasCompanyAccess(run.CompanyId))
        {
            return Result<PayrollLedger>.Failure("Forbidden: You do not have access to this company.");
        }

        var ledger = await _unitOfWork.Compliance.GetLedgerHeaderByRunIdAsync(request.PayrollRunId, cancellationToken);
        if (ledger == null)
        {
            return Result<PayrollLedger>.Failure($"No ledger found for payroll run ID {request.PayrollRunId}.");
        }

        return Result<PayrollLedger>.Success(ledger);
    }
}
