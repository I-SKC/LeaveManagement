using LeaveManagement.Application.Interfaces.Services;

namespace LeaveManagement.Infrastructure.Time;

public sealed class BusinessClock : IBusinessClock
{
    private readonly TimeZoneInfo _timeZone;

    public BusinessClock(string? timeZoneId)
    {
        _timeZone = ResolveTimeZone(timeZoneId);
    }

    public DateOnly Today
    {
        get
        {
            var businessNow = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                _timeZone);

            return DateOnly.FromDateTime(businessNow);
        }
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Local;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Local;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }
}
