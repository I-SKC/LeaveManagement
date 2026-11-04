using LeaveManagement.Application.DTOs.Leave;

namespace LeaveManagement.Application.Interfaces.Services;

public interface ILeaveRequestService
{
    Task<LeaveRequestDto> CreateAsync(
        int employeeId,
        CreateLeaveRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequestDto>> GetMyRequestsAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestDto?> GetByIdAsync(
        long leaveRequestId,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestDto?> CancelAsync(
        long leaveRequestId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequestDto>> GetAllAsync(
        CancellationToken cancellationToken = default);
}