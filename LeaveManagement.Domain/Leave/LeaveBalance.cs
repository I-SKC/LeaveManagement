namespace LeaveManagement.Domain.Leave;

public sealed class LeaveBalance
{
    public int BalanceId { get; private set; }

    public int EmployeeId { get; private set; }

    public int LeaveTypeId { get; private set; }

    public short Year { get; private set; }

    public decimal AvailableDays { get; private set; }

    public decimal PendingDays { get; private set; }

    private LeaveBalance()
    {
    }

    public LeaveBalance(
        int employeeId,
        int leaveTypeId,
        short year,
        decimal availableDays)
    {
        if (employeeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(employeeId));

        if (leaveTypeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(leaveTypeId));

        if (year < 1)
            throw new ArgumentOutOfRangeException(nameof(year));

        if (availableDays < 0)
            throw new ArgumentOutOfRangeException(nameof(availableDays));

        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
        Year = year;
        AvailableDays = availableDays;
        PendingDays = 0;
    }

    public void Reserve(decimal days)
    {
        ValidatePositiveDays(days);

        if (days > AvailableDays - PendingDays)
        {
            throw new InvalidOperationException(
                "Insufficient leave balance.");
        }

        PendingDays += days;
    }

    public void Release(decimal days)
    {
        ValidatePositiveDays(days);

        if (days > PendingDays)
        {
            throw new InvalidOperationException(
                "Cannot release more days than are reserved.");
        }

        PendingDays -= days;
    }

    public void Consume(decimal days)
    {
        ValidatePositiveDays(days);

        if (days > PendingDays || days > AvailableDays)
        {
            throw new InvalidOperationException(
                "Insufficient reserved leave balance.");
        }

        PendingDays -= days;
        AvailableDays -= days;
    }

    public void Refund(decimal days, decimal annualAllowance)
    {
        ValidatePositiveDays(days);

        if (annualAllowance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(annualAllowance));
        }

        if (days > annualAllowance - AvailableDays)
        {
            throw new InvalidOperationException(
                "Refund exceeds the annual allowance.");
        }

        AvailableDays += days;
    }

    public void Adjust(decimal newAvailableDays)
    {
        if (newAvailableDays < PendingDays)
        {
            throw new InvalidOperationException(
                "Available days cannot be less than reserved days.");
        }

        if (newAvailableDays < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(newAvailableDays));
        }

        AvailableDays = newAvailableDays;
    }

    private static void ValidatePositiveDays(decimal days)
    {
        if (days <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(days),
                "Days must be greater than zero.");
        }
    }
}