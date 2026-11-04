namespace LeaveManagement.Application.DTOs.Leave;

/// <summary>
/// A pending leave request as seen by a manager or HR approver. Employee and
/// department details are included so the queue can be rendered from a single
/// request instead of one call per employee.
/// </summary>
public sealed class PendingApprovalDto
{
    public long LeaveRequestId { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public string EmployeeEmail { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public int LeaveTypeId { get; set; }

    public string LeaveTypeName { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal TotalDays { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
