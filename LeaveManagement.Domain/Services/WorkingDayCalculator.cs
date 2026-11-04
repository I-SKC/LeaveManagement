using LeaveManagement.Domain.Leave;

namespace LeaveManagement.Domain.Services;

public static class WorkingDayCalculator
{
    public static int Count(
        DateOnly startDate,
        DateOnly endDate,
        IEnumerable<Holiday> holidays)
    {
        ArgumentNullException.ThrowIfNull(holidays);

        if (endDate < startDate)
        {
            throw new ArgumentException(
                "End date cannot precede start date.");
        }

        var holidayDates = holidays
            .Select(holiday => holiday.HolidayDate)
            .ToHashSet();

        var totalDays = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday
                or DayOfWeek.Sunday)
            {
                continue;
            }

            if (holidayDates.Contains(date))
            {
                continue;
            }

            totalDays++;
        }

        return totalDays;
    }
}