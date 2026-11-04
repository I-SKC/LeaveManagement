namespace LeaveManagement.Domain.Leave;

public sealed class Holiday
{
    public int HolidayId { get; private set; }

    public DateOnly HolidayDate { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public short Year { get; private set; }

    private Holiday()
    {
    }

    public Holiday(DateOnly holidayDate, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Holiday name is required.", nameof(name));
        }

        HolidayDate = holidayDate;
        Name = name.Trim();
        Year = checked((short)holidayDate.Year);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Holiday name is required.", nameof(name));
        }

        Name = name.Trim();
    }

    public void Reschedule(DateOnly holidayDate)
    {
        HolidayDate = holidayDate;
        Year = checked((short)holidayDate.Year);
    }
}