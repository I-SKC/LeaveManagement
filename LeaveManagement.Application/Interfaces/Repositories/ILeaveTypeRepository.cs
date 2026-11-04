using LeaveManagement.Domain.Leave;

namespace LeaveManagement.Application.Interfaces.Repositories;

public interface ILeaveTypeRepository
{
    Task<LeaveType?> GetByIdAsync(
        int leaveTypeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveType>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        LeaveType leaveType,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}