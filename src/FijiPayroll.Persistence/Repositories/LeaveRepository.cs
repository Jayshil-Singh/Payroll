using FijiPayroll.Domain.Entities.Leave;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Repositories;

/// <summary>
/// Entity Framework implementation of <see cref="ILeaveRepository"/>.
/// </summary>
public sealed class LeaveRepository : ILeaveRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes dependencies.</summary>
    public LeaveRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Leave Types ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<LeaveType?> GetTypeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveTypes
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeaveType>> GetTypesByCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveTypes
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddTypeAsync(LeaveType leaveType, CancellationToken cancellationToken = default)
    {
        await _context.LeaveTypes.AddAsync(leaveType, cancellationToken);
    }

    // ── Leave Balances ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<LeaveBalance?> GetBalanceAsync(int employeeId, int leaveTypeId, int fiscalYear, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveBalances
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.LeaveTypeId == leaveTypeId && x.FiscalYear == fiscalYear, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeaveBalance>> GetBalancesByEmployeeAsync(int employeeId, int fiscalYear, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveBalances
            .Include(x => x.LeaveType)
            .Where(x => x.EmployeeId == employeeId && x.FiscalYear == fiscalYear)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddBalanceAsync(LeaveBalance leaveBalance, CancellationToken cancellationToken = default)
    {
        await _context.LeaveBalances.AddAsync(leaveBalance, cancellationToken);
    }

    // ── Leave Requests ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<LeaveRequest?> GetRequestByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeaveRequest>> GetRequestsByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .Include(x => x.LeaveType)
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeaveRequest>> GetRequestsByCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .Include(x => x.LeaveType)
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddRequestAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default)
    {
        await _context.LeaveRequests.AddAsync(leaveRequest, cancellationToken);
    }

    // ── Leave Transactions ──────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task AddTransactionAsync(LeaveTransaction leaveTransaction, CancellationToken cancellationToken = default)
    {
        await _context.LeaveTransactions.AddAsync(leaveTransaction, cancellationToken);
    }

    // ── Leave Accrual Policies ──────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<LeaveAccrualPolicy?> GetPolicyByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveAccrualPolicies
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddPolicyAsync(LeaveAccrualPolicy policy, CancellationToken cancellationToken = default)
    {
        await _context.LeaveAccrualPolicies.AddAsync(policy, cancellationToken);
    }
}
