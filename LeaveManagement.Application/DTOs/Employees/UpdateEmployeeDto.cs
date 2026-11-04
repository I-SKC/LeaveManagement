namespace LeaveManagement.Application.DTOs.Employees;

public sealed class UpdateEmployeeDto
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    public int? ManagerId { get; set; }
}