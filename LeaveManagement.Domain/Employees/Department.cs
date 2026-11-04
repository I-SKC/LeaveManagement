namespace LeaveManagement.Domain.Employees;

public sealed class Department
{
    public int DepartmentId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    private Department()
    {
    }

    public Department(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Department name is required.",
                nameof(name));
        }

        Name = name.Trim();
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Department name is required.",
                nameof(name));
        }

        Name = name.Trim();
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