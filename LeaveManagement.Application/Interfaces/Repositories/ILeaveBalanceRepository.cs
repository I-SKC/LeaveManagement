using LeaveManagement.Domain.Leave;

namespace LeaveManagement.Application.Interfaces.Repositories;

public interface ILeaveBalanceRepository
{
    Task<LeaveBalance?> GetByEmployeeAndLeaveTypeAsync(
        int employeeId,
        int leaveTypeId,
        short year,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveBalance>> GetByEmployeeIdAsync(
        int employeeId,
        short year,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        LeaveBalance balance,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}