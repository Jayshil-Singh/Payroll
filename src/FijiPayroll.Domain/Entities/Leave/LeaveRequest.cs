using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Events;
using FijiPayroll.Domain.Exceptions;
using FijiPayroll.Shared.Guards;

namespace FijiPayroll.Domain.Entities.Leave;

/// <summary>
/// Aggregate root representing a single leave request submitted by an employee.
/// Enforces the approval state machine: Pending → Approved | Rejected | Cancelled.
/// Maps to <c>leave.LeaveRequests</c>.
/// </summary>
public sealed class LeaveRequest : SoftDeleteEntity
{
    private string? _notes;
    private string? _approvedRejectedBy;
    private string? _rejectionReason;
    private string? _cancellationReason;

    private LeaveRequest() { }

    /// <summary>Gets the company this leave request belongs to (multi-tenant isolation).</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the employee who submitted this leave request.</summary>
    public int EmployeeId { get; private set; }

    /// <summary>Gets the leave type applied for.</summary>
    public int LeaveTypeId { get; private set; }

    /// <summary>Navigation property to the associated leave type.</summary>
    public LeaveType? LeaveType { get; private set; }

    /// <summary>Gets the first day of the requested leave period (inclusive).</summary>
    public DateTime StartDate { get; private set; }

    /// <summary>Gets the last day of the requested leave period (inclusive).</summary>
    public DateTime EndDate { get; private set; }

    /// <summary>Gets the total number of working days requested.</summary>
    public decimal TotalDays { get; private set; }

    /// <summary>Gets the current approval status of this leave request.</summary>
    public LeaveStatus Status { get; private set; }

    /// <summary>Gets optional employee notes/reason for the leave.</summary>
    public string? Notes
    {
        get => _notes;
        private set => _notes = value;
    }

    /// <summary>Gets the username of the manager who approved or rejected this request.</summary>
    public string? ApprovedRejectedBy
    {
        get => _approvedRejectedBy;
        private set => _approvedRejectedBy = value;
    }

    /// <summary>Gets the UTC timestamp when the request was approved or rejected.</summary>
    public DateTime? ApprovedRejectedAt { get; private set; }

    /// <summary>Gets the reason provided when rejecting a leave request.</summary>
    public string? RejectionReason
    {
        get => _rejectionReason;
        private set => _rejectionReason = value;
    }

    /// <summary>Gets the reason provided when cancelling a leave request.</summary>
    public string? CancellationReason
    {
        get => _cancellationReason;
        private set => _cancellationReason = value;
    }

    /// <summary>
    /// Gets whether a medical certificate is required for this leave request.
    /// Computed from the leave type configuration.
    /// </summary>
    public bool MedicalCertificateRequired { get; private set; }

    /// <summary>Gets whether the medical certificate has been provided.</summary>
    public bool MedicalCertificateProvided { get; private set; }

    /// <summary>Domain events raised by this aggregate for out-of-process consumers.</summary>
    private readonly List<object> _domainEvents = [];

    /// <summary>Gets the domain events raised by this aggregate (read-only projection).</summary>
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Clears accumulated domain events (called after publishing).</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Factory method to submit a new leave request.
    /// Business rule: Start date must not exceed end date.
    /// Business rule: Total days must be positive.
    /// Business rule: End date must not be in the past.
    /// </summary>
    /// <param name="companyId">Company identifier (multi-tenant).</param>
    /// <param name="employeeId">Employee submitting the request.</param>
    /// <param name="leaveTypeId">The leave type being requested.</param>
    /// <param name="startDate">First day of leave (inclusive).</param>
    /// <param name="endDate">Last day of leave (inclusive).</param>
    /// <param name="totalDays">Working days count (caller computes, accounting for weekends/holidays).</param>
    /// <param name="medicalCertificateRequired">Whether a certificate is required per leave type config.</param>
    /// <param name="notes">Optional employee notes.</param>
    /// <returns>A new <see cref="LeaveRequest"/> in <see cref="LeaveStatus.Pending"/> state.</returns>
    /// <exception cref="DomainException">Thrown when business rules are violated.</exception>
    public static LeaveRequest Submit(
        int companyId,
        int employeeId,
        int leaveTypeId,
        DateTime startDate,
        DateTime endDate,
        decimal totalDays,
        bool medicalCertificateRequired,
        string? notes = null)
    {
        if (companyId <= 0)
            throw new DomainException("CompanyId must be a positive integer.");

        if (employeeId <= 0)
            throw new DomainException("EmployeeId must be a positive integer.");

        if (leaveTypeId <= 0)
            throw new DomainException("LeaveTypeId must be a positive integer.");

        if (startDate.Date > endDate.Date)
            throw new DomainException("Leave start date must not be after the end date.");

        if (totalDays <= 0)
            throw new DomainException("Total leave days must be greater than zero.");

        if (endDate.Date < DateTime.UtcNow.Date)
            throw new DomainException("Leave request end date cannot be in the past.");

        var request = new LeaveRequest
        {
            CompanyId = companyId,
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            TotalDays = totalDays,
            Status = LeaveStatus.Pending,
            Notes = notes,
            MedicalCertificateRequired = medicalCertificateRequired,
            MedicalCertificateProvided = false
        };

        request._domainEvents.Add(new LeaveRequestSubmittedEvent(
            request.CompanyId,
            request.EmployeeId,
            request.LeaveTypeId,
            request.StartDate,
            request.EndDate,
            request.TotalDays));

        return request;
    }

    /// <summary>
    /// Approves the leave request.
    /// Business rule: Only <see cref="LeaveStatus.Pending"/> requests can be approved.
    /// </summary>
    /// <param name="approvedBy">Username of the approver.</param>
    /// <exception cref="DomainException">Thrown if status transition is invalid.</exception>
    public void Approve(string approvedBy)
    {
        Guard.AgainstNullOrWhiteSpace(approvedBy, nameof(approvedBy));

        if (Status != LeaveStatus.Pending)
            throw new DomainException($"Cannot approve a leave request in '{Status}' status. Only 'Pending' requests can be approved.");

        Status = LeaveStatus.Approved;
        ApprovedRejectedBy = approvedBy;
        ApprovedRejectedAt = DateTime.UtcNow;

        _domainEvents.Add(new LeaveRequestApprovedEvent(
            CompanyId,
            EmployeeId,
            LeaveTypeId,
            Id,
            TotalDays,
            approvedBy));
    }

    /// <summary>
    /// Rejects the leave request.
    /// Business rule: Only <see cref="LeaveStatus.Pending"/> requests can be rejected.
    /// </summary>
    /// <param name="rejectedBy">Username of the rejector.</param>
    /// <param name="reason">Mandatory reason for rejection.</param>
    /// <exception cref="DomainException">Thrown if status transition is invalid or reason is empty.</exception>
    public void Reject(string rejectedBy, string reason)
    {
        Guard.AgainstNullOrWhiteSpace(rejectedBy, nameof(rejectedBy));
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));

        if (Status != LeaveStatus.Pending)
            throw new DomainException($"Cannot reject a leave request in '{Status}' status. Only 'Pending' requests can be rejected.");

        Status = LeaveStatus.Rejected;
        ApprovedRejectedBy = rejectedBy;
        ApprovedRejectedAt = DateTime.UtcNow;
        RejectionReason = reason;

        _domainEvents.Add(new LeaveRequestRejectedEvent(
            CompanyId,
            EmployeeId,
            LeaveTypeId,
            Id,
            reason));
    }

    /// <summary>
    /// Cancels the leave request.
    /// Business rule: Only <see cref="LeaveStatus.Pending"/> or <see cref="LeaveStatus.Approved"/> requests can be cancelled.
    /// </summary>
    /// <param name="cancelledBy">Username of the person performing the cancellation.</param>
    /// <param name="reason">Optional cancellation reason.</param>
    /// <exception cref="DomainException">Thrown if status transition is invalid.</exception>
    public void Cancel(string cancelledBy, string? reason = null)
    {
        Guard.AgainstNullOrWhiteSpace(cancelledBy, nameof(cancelledBy));

        if (Status == LeaveStatus.Rejected || Status == LeaveStatus.Cancelled)
            throw new DomainException($"Cannot cancel a leave request that is already '{Status}'.");

        Status = LeaveStatus.Cancelled;
        CancellationReason = reason;
    }

    /// <summary>
    /// Marks the medical certificate as provided.
    /// Business rule: Only applies when <see cref="MedicalCertificateRequired"/> is true.
    /// </summary>
    /// <exception cref="DomainException">Thrown if no certificate is required for this request.</exception>
    public void ProvideMedicalCertificate()
    {
        if (!MedicalCertificateRequired)
            throw new DomainException("A medical certificate is not required for this leave request.");

        MedicalCertificateProvided = true;
    }
}
