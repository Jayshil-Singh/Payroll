using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollRunById;

public sealed record GetPayrollRunByIdQuery(int PayrollRunId) : IRequest<Result<PayrollRunDetailDto>>;

public sealed class GetPayrollRunByIdQueryHandler : IRequestHandler<GetPayrollRunByIdQuery, Result<PayrollRunDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetPayrollRunByIdQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PayrollRunDetailDto>> Handle(GetPayrollRunByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsView))
        {
            return Result<PayrollRunDetailDto>.Failure("Forbidden: You do not have permission to view payroll runs.");
        }

        var run = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(request.PayrollRunId, cancellationToken);
        if (run == null)
        {
            return Result<PayrollRunDetailDto>.Failure($"Payroll run with ID {request.PayrollRunId} was not found.");
        }

        if (!_currentUser.HasCompanyAccess(run.CompanyId))
        {
            return Result<PayrollRunDetailDto>.Failure("Forbidden: You do not have access to this company's payroll runs.");
        }

        var dto = new PayrollRunDetailDto
        {
            Id = run.Id,
            CompanyId = run.CompanyId,
            RunCode = run.RunCode,
            PeriodName = run.PeriodName,
            StartDate = run.StartDate,
            EndDate = run.EndDate,
            PaymentDate = run.PaymentDate,
            Frequency = run.Frequency,
            Status = run.Status,
            Description = run.Description,
            SnapshotHash = run.SnapshotHash,
            CurrentRequestId = run.CurrentRequestId,
            // Filter non-superseded computed employees
            Employees = run.Employees
                .Where(e => !e.IsSuperseded)
                .Select(e => new PayrollRunEmployeeDto
                {
                    Id = e.Id,
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.EmployeeName,
                    Tin = e.Tin,
                    FnpfNumber = e.FnpfNumber,
                    ResidencyStatus = e.ResidencyStatus,
                    Department = e.Department,
                    BaseSalary = e.BaseSalary,
                    GrossPay = e.GrossPay,
                    TotalAllowances = e.TotalAllowances,
                    TotalDeductions = e.TotalDeductions,
                    NetPay = e.NetPay,
                    PayeTax = e.PayeTax,
                    FnpfEmployeeContribution = e.FnpfEmployeeContribution,
                    FnpfEmployerContribution = e.FnpfEmployerContribution,
                    TaxVersionUsed = e.TaxVersionUsed,
                    IsSuperseded = e.IsSuperseded,
                    TraceText = e.Trace?.TraceText,
                    LineItems = e.LineItems.Select(li => new PayrollRunLineItemDto
                    {
                        Id = li.Id,
                        ComponentId = li.ComponentId,
                        ComponentCode = li.ComponentCode,
                        ComponentName = li.ComponentName,
                        ComponentType = li.ComponentType.ToString(),
                        Amount = li.Amount,
                        IsTaxable = li.IsTaxable,
                        AffectsFnpf = li.AffectsFnpf,
                        EmployerContributionFlag = li.EmployerContributionFlag
                    }).ToList().AsReadOnly()
                }).ToList().AsReadOnly()
        };

        return Result<PayrollRunDetailDto>.Success(dto);
    }
}
