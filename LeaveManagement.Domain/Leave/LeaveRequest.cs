namespace LeaveManagement.Domain.Leave;

public sealed class LeaveRequest
{
    public long LeaveRequestId { get; private set; }

    public int EmployeeId { get; private set; }

    public int LeaveTypeId { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public decimal TotalDays { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public LeaveRequestStatus Status { get; private set; }
        = LeaveRequestStatus.Pending;

    public int? ApprovedBy { get; private set; }

    public DateTime? DecidedAtUtc { get; private set; }

    public string? DecisionComment { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private LeaveRequest()
    {
    }

    public LeaveRequest(
        int employeeId,
        int leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        decimal totalDays,
        string reason,
        DateTime createdAtUtc)
    {
        if (employeeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(employeeId));

        if (leaveTypeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(leaveTypeId));

        if (endDate < startDate)
        {
            throw new ArgumentException(
                "End date cannot precede start date.");
        }

        if (totalDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalDays));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));

        if (reason.Trim().Length > 500)
            throw new ArgumentException(
                "Reason cannot exceed 500 characters.", nameof(reason));

        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
        StartDate = startDate;
        EndDate = endDate;
        TotalDays = totalDays;
        Reason = reason.Trim();
        CreatedAtUtc = createdAtUtc;
        Status = LeaveRequestStatus.Pending;
    }

    public void Approve(int approverEmployeeId, DateTime decidedAtUtc)
    {
        EnsurePending();

        if (approverEmployeeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(approverEmployeeId));

        if (approverEmployeeId == EmployeeId)
        {
            throw new InvalidOperationException(
                "Employees cannot approve their own leave requests.");
        }

        ApprovedBy = approverEmployeeId;
        DecidedAtUtc = decidedAtUtc;
        DecisionComment = null;
        Status = LeaveRequestStatus.Approved;
    }

    public void Reject(
        int approverEmployeeId,
        string comment,
        DateTime decidedAtUtc)
    {
        EnsurePending();

        if (approverEmployeeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(approverEmployeeId));

        if (approverEmployeeId == EmployeeId)
        {
            throw new InvalidOperationException(
                "Employees cannot reject their own leave requests.");
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new ArgumentException(
                "A rejection comment is required.", nameof(comment));
        }

        if (comment.Trim().Length > 500)
        {
            throw new ArgumentException(
                "Comment cannot exceed 500 characters.", nameof(comment));
        }

        ApprovedBy = approverEmployeeId;
        DecidedAtUtc = decidedAtUtc;
        DecisionComment = comment.Trim();
        Status = LeaveRequestStatus.Rejected;
    }

    public void Cancel(int actorEmployeeId, DateOnly today)
    {
        if (actorEmployeeId != EmployeeId)
        {
            throw new InvalidOperationException(
                "Only the employee who submitted the request can cancel it.");
        }

        if (Status == LeaveRequestStatus.Pending)
        {
            Status = LeaveRequestStatus.Cancelled;
            return;
        }

        if (Status == LeaveRequestStatus.Approved && StartDate > today)
        {
            Status = LeaveRequestStatus.Cancelled;
            return;
        }

        throw new InvalidOperationException(
            "This leave request cannot be cancelled.");
    }

    private void EnsurePending()
    {
        if (Status != LeaveRequestStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only pending requests can be approved or rejected.");
        }
    }
}