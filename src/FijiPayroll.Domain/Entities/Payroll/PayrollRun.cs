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
    /// Foreign key to PayrollPeriod.
    /// </summary>
    public int? PayrollPeriodId { get; private set; }

    /// <summary>
    /// Foreign key to PayrollGroup.
    /// </summary>
    public int? PayrollGroupId { get; private set; }

    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? ApprovalRole { get; private set; }
    public string? ApprovalMachine { get; private set; }
    public string? ApprovalIp { get; private set; }
    public string? ApprovalHash { get; private set; }
    public string? ApprovalSignature { get; private set; }
    public string? ApprovalThumbprint { get; private set; }
    public string? ApprovalCorrelationId { get; private set; }

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
    /// Transition run to Validating status.
    /// </summary>
    public void TransitionToValidating(string userName)
    {
        if (Status != PayrollRunStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot transition to Validating from status '{Status}'.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.Validating;
        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, "Transitioned to Validating."));
    }

    /// <summary>
    /// Acquires the calculation lock. Supports recovery of stuck locks (5-minute timeout).
    /// </summary>
    public void AcquireLock(Guid requestId, string userName)
    {
        bool isStuckLock = Status == PayrollRunStatus.Calculating && LockedAt.HasValue && LockedAt.Value.AddMinutes(5) < DateTime.UtcNow;

        if (Status != PayrollRunStatus.Validating && Status != PayrollRunStatus.Draft && !isStuckLock)
        {
            throw new InvalidOperationException($"Cannot calculate payroll run in status '{Status}'. Must be in Validating or Draft state.");
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
        if (Status != PayrollRunStatus.Calculated && Status != PayrollRunStatus.Calculating && Status != PayrollRunStatus.Validating)
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
    /// Reverts the payroll run back to Draft status (rollback).
    /// </summary>
    public void RevertToDraft(string userName, string reason)
    {
        var oldStatus = Status;
        Status = PayrollRunStatus.Draft;
        SnapshotHash = null;
        CurrentRequestId = null;
        LockedAt = null;
        _lockedBy = null;

        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, $"Payroll run rolled back to Draft. Reason: {reason}"));
    }

    /// <summary>
    /// Approves the calculated payroll run.
    /// </summary>
    public void Approve(string userName)
    {
        Approve(userName, "Payroll Officer", "Localhost", "127.0.0.1", string.Empty, string.Empty, string.Empty, Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Approves the calculated payroll run with digital signatures.
    /// </summary>
    public void Approve(
        string userName,
        string role,
        string machine,
        string ip,
        string hash,
        string signature,
        string thumbprint,
        string correlationId)
    {
        if (Status != PayrollRunStatus.Calculated)
        {
            throw new InvalidOperationException($"Cannot approve payroll run in status '{Status}'.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.Approved;
        ApprovedBy = userName;
        ApprovedAt = DateTime.UtcNow;
        ApprovalRole = role;
        ApprovalMachine = machine;
        ApprovalIp = ip;
        ApprovalHash = hash;
        ApprovalSignature = signature;
        ApprovalThumbprint = thumbprint;
        ApprovalCorrelationId = correlationId;

        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, $"Payroll run digitally approved by {userName} ({role}) on {machine}"));
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
    /// Transition to BankExported.
    /// </summary>
    public void TransitionToBankExported(string userName)
    {
        if (Status != PayrollRunStatus.Posted)
        {
            throw new InvalidOperationException($"Cannot transition to BankExported from status '{Status}'.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.BankExported;
        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, "Bank file exported successfully."));
    }

    /// <summary>
    /// Transition to FrcsExported.
    /// </summary>
    public void TransitionToFrcsExported(string userName)
    {
        if (Status != PayrollRunStatus.BankExported)
        {
            throw new InvalidOperationException($"Cannot transition to FRCSExported from status '{Status}'.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.FrcsExported;
        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, "FRCS file exported successfully."));
    }

    /// <summary>
    /// Transition to FnpfExported.
    /// </summary>
    public void TransitionToFnpfExported(string userName)
    {
        if (Status != PayrollRunStatus.FrcsExported)
        {
            throw new InvalidOperationException($"Cannot transition to FNPFExported from status '{Status}'.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.FnpfExported;
        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, "FNPF file exported successfully."));
    }

    /// <summary>
    /// Transition to EvidencePackGenerated.
    /// </summary>
    public void TransitionToEvidenceGenerated(string userName)
    {
        if (Status != PayrollRunStatus.FnpfExported)
        {
            throw new InvalidOperationException($"Cannot transition to EvidenceGenerated from status '{Status}'.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.EvidencePackGenerated;
        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, "Evidence Pack generated and cryptographically signed."));
    }

    /// <summary>
    /// Locks the posted payroll run.
    /// </summary>
    public void LockRun(string userName)
    {
        if (Status != PayrollRunStatus.EvidencePackGenerated && Status != PayrollRunStatus.Posted)
        {
            throw new InvalidOperationException($"Cannot lock payroll run in status '{Status}'. Must be Posted or EvidencePackGenerated.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.Locked;

        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, "Payroll run locked."));
    }

    /// <summary>
    /// Archives the locked run.
    /// </summary>
    public void Archive(string userName)
    {
        if (Status != PayrollRunStatus.Locked)
        {
            throw new InvalidOperationException($"Cannot archive payroll run in status '{Status}'. Must be Locked.");
        }

        var oldStatus = Status;
        Status = PayrollRunStatus.Archived;

        _stateHistory.Add(PayrollRunStateHistory.Create(Id, oldStatus, Status, userName, "Payroll run archived."));
    }

    /// <summary>
    /// Reverses a posted or locked payroll run (financial correction).
    /// </summary>
    public void ReverseRun(string userName, string reason)
    {
        if (Status != PayrollRunStatus.Posted && Status != PayrollRunStatus.Locked && Status != PayrollRunStatus.BankExported && Status != PayrollRunStatus.FrcsExported && Status != PayrollRunStatus.FnpfExported && Status != PayrollRunStatus.EvidencePackGenerated)
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
        return Create(companyId, runCode, periodName, startDate, endDate, paymentDate, frequency, description, null, null);
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
        string? description,
        int? payrollPeriodId = null,
        int? payrollGroupId = null)
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
            Description = description,
            PayrollPeriodId = payrollPeriodId,
            PayrollGroupId = payrollGroupId
        };
    }
}
