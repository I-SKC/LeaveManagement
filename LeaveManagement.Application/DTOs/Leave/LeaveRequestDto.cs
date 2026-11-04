namespace LeaveManagement.Application.DTOs.Leave;

public sealed class LeaveRequestDto
{
    public long LeaveRequestId { get; set; }

    public int EmployeeId { get; set; }

    public int LeaveTypeId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal TotalDays { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int? ApprovedBy { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? DecisionComment { get; set; }
}