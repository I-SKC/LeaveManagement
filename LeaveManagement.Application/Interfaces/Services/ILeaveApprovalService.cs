
using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Domain.Identity;

namespace LeaveManagement.Application.Interfaces.Services;

public interface ILeaveApprovalService
{
    Task<IReadOnlyList<LeaveManagement.Application.DTOs.Leave.PendingApprovalDto>>
        GetPendingAsync(
            int approverEmployeeId,
            Role approverRole,
            CancellationToken cancellationToken = default);

    Task ApproveAsync(
        long leaveRequestId,
        int approverEmployeeId,
        Role approverRole,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveManagement.Application.DTOs.Leave.ManagerHistoryDto>> GetHistoryAsync(
        int approverEmployeeId,
        Role approverRole,
        CancellationToken cancellationToken = default);

    Task RejectAsync(
        long leaveRequestId,
        int approverEmployeeId,
        Role approverRole,
        string comment,
        CancellationToken cancellationToken = default);
}