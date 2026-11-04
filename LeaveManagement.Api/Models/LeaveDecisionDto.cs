namespace LeaveManagement.Api.Models;

public sealed class LeaveDecisionDto
{
    public int ApproverEmployeeId { get; set; }

    public string Comment { get; set; } = string.Empty;
}
