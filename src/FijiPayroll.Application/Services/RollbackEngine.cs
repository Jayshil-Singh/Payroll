using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Service to perform transaction rollbacks on payroll runs, reversing ledger postings and restoring state parameters.
/// </summary>
public sealed class RollbackEngine
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="RollbackEngine"/> class.
    /// </summary>
    public RollbackEngine(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Rolls back an approved/posted payroll run, writing reversing ledger entries and resetting run state.
    /// </summary>
    public async Task RollbackAsync(int payrollRunId, string reason, string user, CancellationToken cancellationToken)
    {
        var run = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(payrollRunId, cancellationToken);
        if (run == null)
        {
            throw new InvalidOperationException($"Payroll run with ID {payrollRunId} not found.");
        }

        // Find active ledger
        var activeLedger = await _unitOfWork.Compliance.GetLedgerHeaderByRunIdAsync(payrollRunId, cancellationToken);
        if (activeLedger != null && activeLedger.IsReversed)
        {
            activeLedger = null;
        }

        if (activeLedger != null)
        {
            // 1. Mark original ledger as reversed
            activeLedger.Reverse(reason, user);

            // 2. Create reversal ledger header
            var reversalLedger = PayrollLedger.Create(
                run.CompanyId,
                run.Id,
                -activeLedger.TotalGross,
                -activeLedger.TotalPAYE,
                -activeLedger.TotalFNPFEmployee,
                -activeLedger.TotalFNPFEmployer,
                -activeLedger.TotalNetPay,
                user,
                $"REV-{activeLedger.Hash}"
            );

            // 3. Create reversing double-entry transactions
            foreach (var tx in activeLedger.Transactions)
            {
                var revTx = PayrollLedgerTransaction.Create(
                    run.CompanyId,
                    tx.PayrollLedgerComponentId,
                    tx.EmployeeId,
                    tx.AccountCode,
                    tx.Credit, // swap credit and debit to reverse
                    tx.Debit,
                    $"Reversal: {tx.Description}"
                );
                reversalLedger.AddTransaction(revTx);
            }

            await _unitOfWork.Compliance.AddLedgerAsync(reversalLedger, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 4. Create trace link
            var link = PayrollLedgerReversal.Create(
                run.CompanyId,
                activeLedger.Id,
                reversalLedger.Id,
                reason,
                user
            );
            await _unitOfWork.PayrollLedgerReversals.AddAsync(link, cancellationToken);
        }

        // 5. Remove snapshots for this run
        var snapshots = await _unitOfWork.PayrollSnapshots.GetByRunIdAsync(payrollRunId, cancellationToken);
        foreach (var snap in snapshots)
        {
            _unitOfWork.PayrollSnapshots.Remove(snap);
        }

        // 6. Clear exceptions
        var exceptions = await _unitOfWork.PayrollExceptionQueues.GetByRunIdAsync(payrollRunId, cancellationToken);
        foreach (var ex in exceptions)
        {
            _unitOfWork.PayrollExceptionQueues.Remove(ex);
        }

        // 7. Revert run state & employee calculations
        run.RevertToDraft(user, reason);
        _unitOfWork.PayrollRuns.Update(run);

        // Mark active calculations as superseded to allow recalculating cleanly and preserve audit
        foreach (var emp in run.Employees.Where(e => !e.IsSuperseded))
        {
            emp.SetSuperseded();
        }

        // 8. Log run history
        var history = PayrollRunHistory.Create(
            run.CompanyId,
            run.Id,
            "Rollback",
            user,
            "Server",
            Guid.NewGuid().ToString(),
            $"Rollback executed: {reason}",
            null,
            null
        );
        await _unitOfWork.PayrollRunHistories.AddAsync(history, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
