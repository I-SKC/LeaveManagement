namespace LeaveManagement.Application.DTOs.Leave;

public sealed class HolidayDto
{
    public int HolidayId { get; set; }

    public DateOnly HolidayDate { get; set; }

    public string Name { get; set; } = string.Empty;

    public short Year { get; set; }
}

public sealed class SaveHolidayDto
{
    public DateOnly HolidayDate { get; set; }

    public string Name { get; set; } = string.Empty;
}
