namespace LeaveManagement.Application.DTOs.Leave;

public sealed class CreateLeaveRequestDto
{
    public int LeaveTypeId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string Reason { get; set; } = string.Empty;
}