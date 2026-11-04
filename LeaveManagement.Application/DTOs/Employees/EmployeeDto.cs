namespace LeaveManagement.Application.DTOs.Employees;

public sealed class EmployeeDto
{
    public int EmployeeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    public int? ManagerId { get; set; }

    public DateOnly JoinDate { get; set; }

    public bool IsActive { get; set; }
}