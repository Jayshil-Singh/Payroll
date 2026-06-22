using FijiPayroll.Domain.Entities.Leave;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Repository interface for Leave module entities.
/// </summary>
public interface ILeaveRepository
{
    // ── Leave Types ─────────────────────────────────────────────────────────
    Task<LeaveType?> GetTypeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveType>> GetTypesByCompanyAsync(int companyId, CancellationToken cancellationToken = default);
    Task AddTypeAsync(LeaveType leaveType, CancellationToken cancellationToken = default);

    // ── Leave Balances ──────────────────────────────────────────────────────
    Task<LeaveBalance?> GetBalanceAsync(int employeeId, int leaveTypeId, int fiscalYear, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveBalance>> GetBalancesByEmployeeAsync(int employeeId, int fiscalYear, CancellationToken cancellationToken = default);
    Task AddBalanceAsync(LeaveBalance leaveBalance, CancellationToken cancellationToken = default);

    // ── Leave Requests ──────────────────────────────────────────────────────
    Task<LeaveRequest?> GetRequestByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetRequestsByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetRequestsByCompanyAsync(int companyId, CancellationToken cancellationToken = default);
    Task AddRequestAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default);

    // ── Leave Transactions ──────────────────────────────────────────────────
    Task AddTransactionAsync(LeaveTransaction leaveTransaction, CancellationToken cancellationToken = default);

    // ── Leave Accrual Policies ──────────────────────────────────────────────
    Task<LeaveAccrualPolicy?> GetPolicyByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddPolicyAsync(LeaveAccrualPolicy policy, CancellationToken cancellationToken = default);
}
