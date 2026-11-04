namespace LeaveManagement.Application.DTOs.Leave;

public sealed class CreateLeaveTypeDto
{
    public string Name { get; set; } = string.Empty;

    public decimal AnnualAllowance { get; set; }

    public bool RequiresManagerApproval { get; set; } = true;
}