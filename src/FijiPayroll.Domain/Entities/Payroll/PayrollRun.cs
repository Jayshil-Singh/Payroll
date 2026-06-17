using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Shared.Guards;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Aggregate root representing a payroll run period.
/// Enforces the state machine lifecycle, concurrency locking, and snapshots.
/// </summary>
public sealed class PayrollRun : AuditableEntity
{
    private string _runCode = string.Empty;
    private string _periodName = string.Empty;
    private string? _description;
    private string? _lockedBy;
    private string? _snapshotHash;

    private readonly List<PayrollRunEmployee> _employees = [];
    private readonly List<PayrollRunStateHistory> _stateHistory = [];

    private PayrollRun() { }

    /// <summary>
    /// Foreign key to Company.
    /// </summary>
    public int CompanyId { get; private set; }

    /// <summary>
    /// Code identifying the run (e.g. "PR-2026-06-W01").
    /// </summary>
    public string RunCode
    {
        get => _runCode;
        private set => _runCode = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Display name of the pay period (e.g. "June 2026 Week 1").
    /// </summary>
    public string PeriodName
    {
        get => _periodName;
        private set => _periodName = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Start date of the pay period.
    /// </summary>
    public DateTime StartDate { get; private set; }

    /// <summary>
    /// End date of the pay period.
    /// </summary>
    public DateTime EndDate { get; private set; }

    /// <summary>
    /// Target bank disbursement/payment date.
    /// </summary>
    public DateTime PaymentDate { get; private set; }

    /// <summary>
    /// Pay frequency of the run.
    /// </summary>
    public PayrollFrequencyType Frequency { get; private set; }

    /// <summary>
    /// Current state machine status.
    /// </summary>
    public PayrollRunStatus Status { get; private set; }

    /// <summary>
    /// Optional description of this run.
    /// </summary>
    public string? Description
    {
        get => _description;
        private set => _description = value;
    }

    /// <summary>
    /// Username of the calculation lock owner.
    /// </summary>
    public string? LockedBy
    {
        get => _lockedBy;
        private set => _lockedBy = value;
    }

    /// <summary>
    /// Timestamp when calculation lock was acquired.
    /// </summary>
    public DateTime? LockedAt { get; private set; }

    /// <summary>
    /// Guid identifying the active calculation request (Idempotency check).
    /// </summary>
    public Guid? CurrentRequestId { get; private set; }

    /// <summary>
    /// Deterministic hash of the input snapshot calculated by PayrollSnapshotHasher.
    /// </summary>
    public string? SnapshotHash
    {
        get => _snapshotHash;
        private set => _snapshotHash = value;
    }

    /// <summary>
    /// Set of employees computed in this run.
    /// </summary>
    public IReadOnlyCollection<PayrollRunEmployee> Employees => _employees.AsReadOnly();

    /// <summary>
    /// Transition history log for compliance audits.
    /// </summary>
    public IReadOnlyCollection<PayrollRunStateHistory> StateHistory => _stateHistory.AsReadOnly();

    /// <summary>
    /// Adds a run employee.
    /// </summary>
    public void AddEmployee(PayrollRunEmployee employee)
    {
        Guard.AgainstNull(employee);
        _employees.Add(employee);
    }

    /// <summary>
    /// Acquires the calculation lock. Supports recovery of stuck locks (5-minute timeout).
    /// </summary>
    public void AcquireLock(Guid requestId, string userName)
    {
        bool isStuckLock = Status == PayrollRunStatus.Calculating && LockedAt.HasValue && LockedAt.Value.AddMinutes(5) < DateTime.UtcNow;

        if (Status != PayrollRunStatus.Draft && !isStuckLock)
        {
            throw new InvalidOperationException($"Cannot calculate payroll run in status '{Status}'.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.Calculating;
        CurrentRequestId = requestId;
        LockedAt = DateTime.UtcNow;
        _lockedBy = userName;

        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, $"Calculation started. Request ID: {requestId}"));
    }

    /// <summary>
    /// Releases lock to Calculated and stores snapshot hash.
    /// </summary>
    public void ReleaseLockToCalculated(string snapshotHash, string userName)
    {
        if (Status != PayrollRunStatus.Calculating)
        {
            throw new InvalidOperationException($"Cannot release calculation lock from status '{Status}'.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.Calculated;
        SnapshotHash = snapshotHash;
        CurrentRequestId = null;
        LockedAt = null;
        _lockedBy = null;

        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, $"Calculation completed. Hash: {snapshotHash}"));
    }

    /// <summary>
    /// Releases lock back to Draft due to execution failure.
    /// </summary>
    public void ReleaseLockToDraft(string userName, string reason)
    {
        if (Status != PayrollRunStatus.Calculating)
        {
            throw new InvalidOperationException($"Cannot release calculation lock to draft from status '{Status}'.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.Draft;
        CurrentRequestId = null;
        LockedAt = null;
        _lockedBy = null;

        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, $"Calculation lock aborted/released. Reason: {reason}"));
    }

    /// <summary>
    /// Reset operation (Calculated -> Draft). Marks non-superseded employees as superseded.
    /// Non-chaining: Does not trigger calculation automatically.
    /// </summary>
    public void Reset(string userName)
    {
        if (Status != PayrollRunStatus.Calculated && Status != PayrollRunStatus.Calculating)
        {
            throw new InvalidOperationException($"Cannot reset payroll run from status '{Status}'.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.Draft;
        SnapshotHash = null;
        CurrentRequestId = null;
        LockedAt = null;
        _lockedBy = null;

        foreach (var employee in _employees)
        {
            if (!employee.IsSuperseded)
            {
                employee.SetSuperseded();
            }
        }

        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, "Payroll run reset to draft. Calculations superseded."));
    }

    /// <summary>
    /// Approves the calculated payroll run.
    /// </summary>
    public void Approve(string userName)
    {
        if (Status != PayrollRunStatus.Calculated)
        {
            throw new InvalidOperationException($"Cannot approve payroll run in status '{Status}'.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.Approved;

        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, "Payroll run approved."));
    }

    /// <summary>
    /// Posts the approved payroll run.
    /// </summary>
    public void Post(string userName)
    {
        if (Status != PayrollRunStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot post payroll run in status '{Status}'.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.Posted;

        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, "Payroll run posted."));
    }

    /// <summary>
    /// Locks the posted payroll run.
    /// </summary>
    public void LockRun(string userName)
    {
        if (Status != PayrollRunStatus.Posted)
        {
            throw new InvalidOperationException($"Cannot lock payroll run in status '{Status}'.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.Locked;

        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, "Payroll run locked."));
    }

    /// <summary>
    /// Reverses a posted or locked payroll run (financial correction).
    /// </summary>
    public void ReverseRun(string userName, string reason)
    {
        if (Status != PayrollRunStatus.Posted && Status != PayrollRunStatus.Locked)
        {
            throw new InvalidOperationException($"Cannot reverse payroll run in status '{Status}'. Reversal only permitted post-posting.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.Reversed;

        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, $"Payroll run reversed. Reason: {reason}"));
    }

    /// <summary>
    /// Factory method to build a new PayrollRun.
    /// </summary>
    public static PayrollRun Create(
        int companyId,
        string runCode,
        string periodName,
        DateTime startDate,
        DateTime endDate,
        DateTime paymentDate,
        PayrollFrequencyType frequency,
        string? description)
    {
        if (startDate >= endDate)
        {
            throw new ArgumentException("Start date must be before end date.");
        }

        return new PayrollRun
        {
            CompanyId = companyId,
            RunCode = runCode,
            PeriodName = periodName,
            StartDate = startDate,
            EndDate = endDate,
            PaymentDate = paymentDate,
            Frequency = Guard.AgainstInvalidEnum(frequency),
            Status = PayrollRunStatus.Draft,
            Description = description
        };
    }
}
