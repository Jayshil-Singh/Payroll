namespace FijiPayroll.Domain.Events;

/// <summary>Raised when a leave request is submitted for approval.</summary>
public sealed class LeaveRequestSubmittedEvent : IDomainEvent
{
    public int CompanyId { get; }
    public int EmployeeId { get; }
    public int LeaveTypeId { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public decimal TotalDays { get; }
    public DateTime OccurredOn { get; }

    public LeaveRequestSubmittedEvent(
        int companyId,
        int employeeId,
        int leaveTypeId,
        DateTime startDate,
        DateTime endDate,
        decimal totalDays)
    {
        CompanyId = companyId;
        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
        StartDate = startDate;
        EndDate = endDate;
        TotalDays = totalDays;
        OccurredOn = DateTime.UtcNow;
    }
}

/// <summary>Raised when a leave request is approved.</summary>
public sealed class LeaveRequestApprovedEvent : IDomainEvent
{
    public int CompanyId { get; }
    public int EmployeeId { get; }
    public int LeaveTypeId { get; }
    public int LeaveRequestId { get; }
    public decimal TotalDays { get; }
    public string ApprovedBy { get; }
    public DateTime OccurredOn { get; }

    public LeaveRequestApprovedEvent(
        int companyId,
        int employeeId,
        int leaveTypeId,
        int leaveRequestId,
        decimal totalDays,
        string approvedBy)
    {
        CompanyId = companyId;
        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
        LeaveRequestId = leaveRequestId;
        TotalDays = totalDays;
        ApprovedBy = approvedBy;
        OccurredOn = DateTime.UtcNow;
    }
}

/// <summary>Raised when a leave request is rejected.</summary>
public sealed class LeaveRequestRejectedEvent : IDomainEvent
{
    public int CompanyId { get; }
    public int EmployeeId { get; }
    public int LeaveTypeId { get; }
    public int LeaveRequestId { get; }
    public string Reason { get; }
    public DateTime OccurredOn { get; }

    public LeaveRequestRejectedEvent(
        int companyId,
        int employeeId,
        int leaveTypeId,
        int leaveRequestId,
        string reason)
    {
        CompanyId = companyId;
        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
        LeaveRequestId = leaveRequestId;
        Reason = reason;
        OccurredOn = DateTime.UtcNow;
    }
}
