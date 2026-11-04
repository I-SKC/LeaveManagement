namespace LeaveManagement.Application.DTOs.Leave;

public sealed class LeaveTypeDto
{
    public int LeaveTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal AnnualAllowance { get; set; }

    public bool IsActive { get; set; }

    public bool RequiresManagerApproval { get; set; }
}