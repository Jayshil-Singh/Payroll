using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Entities.Company;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Repository interface for Company Setup Wizard entities and configurations.
/// </summary>
public interface ISetupRepository
{
    /// <summary>Retrieves a company by its ID.</summary>
    Task<Company?> GetCompanyByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Retrieves the setup state for a company, including tasks.</summary>
    Task<CompanySetupState?> GetSetupStateAsync(int companyId, CancellationToken cancellationToken);

    /// <summary>Adds a new setup state.</summary>
    Task AddSetupStateAsync(CompanySetupState state, CancellationToken cancellationToken);

    /// <summary>Adds a setup task.</summary>
    Task AddSetupTaskAsync(CompanySetupTask task, CancellationToken cancellationToken);

    /// <summary>Retrieves all setup tasks for a company.</summary>
    Task<IReadOnlyList<CompanySetupTask>> GetSetupTasksAsync(int companyId, CancellationToken cancellationToken);

    /// <summary>Adds a setup checkpoint.</summary>
    Task AddSetupCheckpointAsync(SetupCheckpoint checkpoint, CancellationToken cancellationToken);

    /// <summary>Removes all checkpoints for a company.</summary>
    Task RemoveCheckpointsAsync(int companyId, CancellationToken cancellationToken);

    /// <summary>Adds a setup execution record.</summary>
    Task AddSetupExecutionRecordAsync(SetupExecutionRecord record, CancellationToken cancellationToken);

    /// <summary>Retrieves a setup execution record by company ID and execution ID.</summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task returning the SetupExecutionRecord, or null if not found.</returns>
    Task<SetupExecutionRecord?> GetSetupExecutionRecordAsync(int companyId, Guid executionId, CancellationToken cancellationToken);

    /// <summary>Removes all execution records for a company.</summary>
    Task RemoveExecutionRecordsAsync(int companyId, CancellationToken cancellationToken);

    /// <summary>Adds a setup audit record.</summary>
    Task AddSetupAuditAsync(CompanySetupAudit audit, CancellationToken cancellationToken);

    /// <summary>Retrieves all fiscal calendars for a company, including periods.</summary>
    Task<IReadOnlyList<FiscalCalendar>> GetFiscalCalendarsAsync(int companyId, CancellationToken cancellationToken);

    /// <summary>Retrieves all payroll frequency definitions for a company, including schedules.</summary>
    Task<IReadOnlyList<PayrollFrequencyDefinition>> GetPayrollFrequencyDefinitionsAsync(int companyId, CancellationToken cancellationToken);

    /// <summary>Retrieves all company bank accounts.</summary>
    Task<IReadOnlyList<CompanyBankAccount>> GetCompanyBankAccountsAsync(int companyId, CancellationToken cancellationToken);

    /// <summary>Retrieves all approval configurations.</summary>
    Task<IReadOnlyList<ApprovalConfig>> GetApprovalConfigsAsync(int companyId, CancellationToken cancellationToken);

    /// <summary>Removes the setup state and all associated tasks for a company.</summary>
    Task RemoveSetupStateAsync(int companyId, CancellationToken cancellationToken);
}
