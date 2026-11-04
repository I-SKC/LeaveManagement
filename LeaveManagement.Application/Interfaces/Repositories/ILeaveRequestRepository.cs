using LeaveManagement.Domain.Leave;

namespace LeaveManagement.Application.Interfaces.Repositories;

public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> GetByIdAsync(
        long leaveRequestId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequest>> GetByEmployeeIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingRequestAsync(
        int employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequest>> GetPendingByManagerIdAsync(
        int managerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequest>> GetAllPendingAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically moves a request from <paramref name="expectedStatus"/> to
    /// <paramref name="newStatus"/>. Returns false when another decision already
    /// changed the request, which is how concurrent approvals are serialized.
    /// </summary>
    Task<bool> TryClaimDecisionAsync(
        long leaveRequestId,
        LeaveRequestStatus expectedStatus,
        LeaveRequestStatus newStatus,
        int approvedBy,
        DateTime decidedAtUtc,
        string? decisionComment,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        LeaveRequest leaveRequest,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequest>> GetManagerHistoryAsync(
        int managerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequest>> GetHrAllHistoryAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequest>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}