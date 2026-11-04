
using LeaveManagement.Application.Interfaces.Repositories;
using LeaveManagement.Domain.Leave;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Repositories;

public sealed class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly AppDbContext _context;

    public LeaveRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveRequest?> GetByIdAsync(
        long leaveRequestId,
        CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .FirstOrDefaultAsync(
                r => r.LeaveRequestId == leaveRequestId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetByEmployeeIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOverlappingRequestAsync(
        int employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .AsNoTracking()
            .AnyAsync(
                r => r.EmployeeId == employeeId
                     && (r.Status == LeaveRequestStatus.Pending
                         || r.Status == LeaveRequestStatus.Approved)
                     && r.StartDate <= endDate
                     && r.EndDate >= startDate,
                cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetAllPendingAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .AsNoTracking()
            .Where(r => r.Status == LeaveRequestStatus.Pending)
            .OrderBy(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryClaimDecisionAsync(
        long leaveRequestId,
        LeaveRequestStatus expectedStatus,
        LeaveRequestStatus newStatus,
        int approvedBy,
        DateTime decidedAtUtc,
        string? decisionComment,
        CancellationToken cancellationToken = default)
    {
        // Conditional update: only the first decision on a pending request can
        // win, so two simultaneous approvals cannot both deduct the balance.
        var affected = await _context.LeaveRequests
            .Where(r => r.LeaveRequestId == leaveRequestId
                        && r.Status == expectedStatus)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, newStatus)
                    .SetProperty(r => r.ApprovedBy, approvedBy)
                    .SetProperty(r => r.DecidedAtUtc, decidedAtUtc)
                    .SetProperty(r => r.DecisionComment, decisionComment),
                cancellationToken);

        return affected == 1;
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetPendingByManagerIdAsync(
        int managerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .AsNoTracking()
            .Where(r =>
                r.Status == LeaveRequestStatus.Pending &&
                _context.Employees.Any(e =>
                    e.EmployeeId == r.EmployeeId &&
                    e.ManagerId == managerId))
            .OrderBy(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetManagerHistoryAsync(
        int managerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .AsNoTracking()
            .Where(r =>
                (r.Status == LeaveRequestStatus.Approved ||
                 r.Status == LeaveRequestStatus.Rejected) &&
                _context.Employees.Any(e =>
                    e.EmployeeId == r.EmployeeId &&
                    e.ManagerId == managerId))
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetHrAllHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .AsNoTracking()
            .Where(r =>
                r.Status == LeaveRequestStatus.Approved ||
                r.Status == LeaveRequestStatus.Rejected)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        LeaveRequest leaveRequest,
        CancellationToken cancellationToken = default)
    {
        await _context.LeaveRequests.AddAsync(
            leaveRequest, cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}