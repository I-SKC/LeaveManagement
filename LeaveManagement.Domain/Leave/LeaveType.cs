namespace LeaveManagement.Domain.Leave;

public sealed class LeaveType
{
    public int LeaveTypeId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public decimal AnnualAllowance { get; private set; }

    public bool IsActive { get; private set; } = true;

    public bool RequiresManagerApproval { get; private set; } = true;

    private LeaveType()
    {
    }

    public LeaveType(string name, decimal annualAllowance)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Leave type name is required.", nameof(name));

        if (annualAllowance < 0)
            throw new ArgumentOutOfRangeException(nameof(annualAllowance));

        Name = name.Trim();
        AnnualAllowance = annualAllowance;
    }

    public void Update(string name, decimal annualAllowance, bool requiresManagerApproval)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Leave type name is required.", nameof(name));

        if (annualAllowance < 0)
            throw new ArgumentOutOfRangeException(nameof(annualAllowance));

        Name = name.Trim();
        AnnualAllowance = annualAllowance;
        RequiresManagerApproval = requiresManagerApproval;
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