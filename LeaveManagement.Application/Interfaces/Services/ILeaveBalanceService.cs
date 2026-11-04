using LeaveManagement.Application.DTOs.Leave;

namespace LeaveManagement.Application.Interfaces.Services;

public interface ILeaveBalanceService
{
    Task<IReadOnlyList<LeaveBalanceDto>> GetMyBalancesAsync(
        int employeeId,
        short year,
        CancellationToken cancellationToken = default);
}