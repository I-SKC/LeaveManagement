namespace LeaveManagement.Application.DTOs.Leave;

public sealed class LeaveBalanceDto
{
    public int EmployeeId { get; set; }

    public int LeaveTypeId { get; set; }

    public short Year { get; set; }

    public decimal AvailableDays { get; set; }

    public decimal PendingDays { get; set; }

    public decimal RemainingDays => AvailableDays - PendingDays;
}