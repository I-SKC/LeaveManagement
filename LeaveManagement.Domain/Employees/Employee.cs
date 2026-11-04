namespace LeaveManagement.Domain.Employees;

public sealed class Employee
{
    public int EmployeeId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public int DepartmentId { get; private set; }

    public int? ManagerId { get; private set; }

    public DateOnly JoinDate { get; private set; }

    public bool IsActive { get; private set; } = true;

    public Department Department { get; private set; } = null!;

    public Employee? Manager { get; private set; }

    private Employee()
    {
    }

    public Employee(
        string name,
        string email,
        int departmentId,
        DateOnly joinDate,
        int? managerId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Employee name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Employee email is required.", nameof(email));

        if (departmentId <= 0)
            throw new ArgumentOutOfRangeException(nameof(departmentId));

        if (managerId.HasValue && managerId.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(managerId));

        Name = name.Trim();
        Email = email.Trim();
        DepartmentId = departmentId;
        JoinDate = joinDate;
        ManagerId = managerId;
    }

    public void UpdateDetails(
        string name,
        string email,
        int departmentId,
        int? managerId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Employee name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Employee email is required.", nameof(email));

        if (departmentId <= 0)
            throw new ArgumentOutOfRangeException(nameof(departmentId));

        if (managerId == EmployeeId)
            throw new InvalidOperationException(
                "An employee cannot be their own manager.");

        if (managerId.HasValue && managerId.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(managerId));

        Name = name.Trim();
        Email = email.Trim();
        DepartmentId = departmentId;
        ManagerId = managerId;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}