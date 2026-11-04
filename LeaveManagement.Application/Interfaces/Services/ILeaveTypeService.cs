
using LeaveManagement.Application.DTOs.Leave;

namespace LeaveManagement.Application.Interfaces.Services;

public interface ILeaveTypeService
{
    Task<IReadOnlyList<LeaveTypeDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<LeaveTypeDto?> GetByIdAsync(
        int leaveTypeId,
        CancellationToken cancellationToken = default);

    Task<LeaveTypeDto> CreateAsync(
        CreateLeaveTypeDto dto,
        CancellationToken cancellationToken = default);

    Task<LeaveTypeDto?> UpdateAsync(
        int leaveTypeId,
        CreateLeaveTypeDto dto,
        CancellationToken cancellationToken = default);

    Task<LeaveTypeDto?> SetActiveAsync(
        int leaveTypeId,
        bool isActive,
        CancellationToken cancellationToken = default);
}