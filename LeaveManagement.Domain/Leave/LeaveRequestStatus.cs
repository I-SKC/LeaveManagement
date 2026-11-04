namespace LeaveManagement.Domain.Leave;

public enum LeaveRequestStatus : byte
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}