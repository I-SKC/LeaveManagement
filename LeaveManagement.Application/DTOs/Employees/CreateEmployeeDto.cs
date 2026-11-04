namespace LeaveManagement.Application.DTOs.Employees;

public sealed class CreateEmployeeDto
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    public DateOnly JoinDate { get; set; }

    public int? ManagerId { get; set; }
}