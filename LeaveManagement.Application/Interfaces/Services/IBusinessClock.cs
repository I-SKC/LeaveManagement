namespace LeaveManagement.Application.Interfaces.Services;

/// <summary>
/// Supplies the current date in the configured business timezone so that
/// date-based rules (such as rejecting past leave) do not depend on the
/// server's local time or on UTC.
/// </summary>
public interface IBusinessClock
{
    DateOnly Today { get; }
}
