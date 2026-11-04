namespace LeaveManagement.Application.DTOs.Leave;

/// <summary>
/// A historical (already decided) leave request shown in a manager or HR
/// approval history view. No pending requests are returned.
/// </summary>
public sealed class ManagerHistoryDto
{
    public long LeaveRequestId { get; init; }
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string EmployeeEmail { get; init; } = string.Empty;
    public int DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public int LeaveTypeId { get; init; }
    public string LeaveTypeName { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal TotalDays { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? DecidedAtUtc { get; init; }
    public int? ApprovedBy { get; init; }
    public string ApproverName { get; init; } = string.Empty;
    public string? DecisionComment { get; init; }
}
