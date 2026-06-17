using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of the ISetupRepository.
/// </summary>
public sealed class SetupRepository : ISetupRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initialises a new instance of the <see cref="SetupRepository"/> class.
    /// </summary>
    public SetupRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Company?> GetCompanyByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Companies.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CompanySetupState?> GetSetupStateAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.CompanySetupStates
            .Include(x => x.Tasks)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddSetupStateAsync(CompanySetupState state, CancellationToken cancellationToken)
    {
        await _context.CompanySetupStates.AddAsync(state, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddSetupTaskAsync(CompanySetupTask task, CancellationToken cancellationToken)
    {
        await _context.CompanySetupTasks.AddAsync(task, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CompanySetupTask>> GetSetupTasksAsync(int companyId, CancellationToken cancellationToken)
    {
        var items = await _context.CompanySetupTasks
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        return items;
    }

    /// <inheritdoc />
    public async Task AddSetupCheckpointAsync(SetupCheckpoint checkpoint, CancellationToken cancellationToken)
    {
        await _context.SetupCheckpoints.AddAsync(checkpoint, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveCheckpointsAsync(int companyId, CancellationToken cancellationToken)
    {
        var items = await _context.SetupCheckpoints
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        _context.SetupCheckpoints.RemoveRange(items);
    }

    /// <inheritdoc />
    public async Task AddSetupExecutionRecordAsync(SetupExecutionRecord record, CancellationToken cancellationToken)
    {
        await _context.SetupExecutionRecords.AddAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SetupExecutionRecord?> GetSetupExecutionRecordAsync(int companyId, Guid executionId, CancellationToken cancellationToken)
    {
        return await _context.SetupExecutionRecords
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.ExecutionId == executionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveExecutionRecordsAsync(int companyId, CancellationToken cancellationToken)
    {
        var items = await _context.SetupExecutionRecords
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        _context.SetupExecutionRecords.RemoveRange(items);
    }

    /// <inheritdoc />
    public async Task AddSetupAuditAsync(CompanySetupAudit audit, CancellationToken cancellationToken)
    {
        await _context.CompanySetupAudits.AddAsync(audit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FiscalCalendar>> GetFiscalCalendarsAsync(int companyId, CancellationToken cancellationToken)
    {
        var items = await _context.FiscalCalendars
            .Include(c => c.Periods)
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        return items;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PayrollFrequencyDefinition>> GetPayrollFrequencyDefinitionsAsync(int companyId, CancellationToken cancellationToken)
    {
        var items = await _context.PayrollFrequencyDefinitions
            .Include(f => f.Schedules)
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        return items;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CompanyBankAccount>> GetCompanyBankAccountsAsync(int companyId, CancellationToken cancellationToken)
    {
        var items = await _context.CompanyBankAccounts
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        return items;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApprovalConfig>> GetApprovalConfigsAsync(int companyId, CancellationToken cancellationToken)
    {
        var items = await _context.ApprovalConfigs
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        return items;
    }

    /// <inheritdoc />
    public async Task RemoveSetupStateAsync(int companyId, CancellationToken cancellationToken)
    {
        var tasks = await _context.CompanySetupTasks
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        _context.CompanySetupTasks.RemoveRange(tasks);

        var state = await _context.CompanySetupStates
            .FirstOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);
        if (state != null)
        {
            _context.CompanySetupStates.Remove(state);
        }
    }
}
