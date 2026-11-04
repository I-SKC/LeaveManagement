namespace LeaveManagement.Application.DTOs.Employees;

public sealed class DepartmentDto
{
    public int DepartmentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}