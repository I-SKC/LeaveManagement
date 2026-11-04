using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Application.Interfaces.Repositories;
using LeaveManagement.Application.Interfaces.Services;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Identity;
using LeaveManagement.Domain.Leave;

using ManagerHistoryDto = LeaveManagement.Application.DTOs.Leave.ManagerHistoryDto;

namespace LeaveManagement.Application.Services.Leave;

public sealed class LeaveApprovalService : ILeaveApprovalService
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILeaveTypeRepository _leaveTypeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveApprovalService(
        ILeaveRequestRepository leaveRequestRepository,
        ILeaveBalanceRepository leaveBalanceRepository,
        IEmployeeRepository employeeRepository,
        ILeaveTypeRepository leaveTypeRepository,
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _leaveBalanceRepository = leaveBalanceRepository;
        _employeeRepository = employeeRepository;
        _leaveTypeRepository = leaveTypeRepository;
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PendingApprovalDto>> GetPendingAsync(
        int approverEmployeeId,
        Role approverRole,
        CancellationToken cancellationToken = default)
    {
        if (approverEmployeeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(approverEmployeeId));

        if (approverRole is not Role.Manager and not Role.HR)
            throw new UnauthorizedAccessException(
                "Only managers and HR can view pending leave requests.");

        var approver = await _employeeRepository.GetByIdAsync(
            approverEmployeeId,
            cancellationToken);

        if (approver is null || !approver.IsActive)
            throw new UnauthorizedAccessException(
                "The approver is not an active employee.");

        var requests = approverRole == Role.HR
            ? await _leaveRequestRepository.GetAllPendingAsync(cancellationToken)
            : await _leaveRequestRepository.GetPendingByManagerIdAsync(
                approverEmployeeId,
                cancellationToken);

        if (requests.Count == 0)
            return Array.Empty<PendingApprovalDto>();

        // Fixed number of lookups (employees, departments, leave types) instead
        // of one request per employee.
        var employees = (await _employeeRepository.GetAllAsync(cancellationToken))
            .ToDictionary(employee => employee.EmployeeId);

        var departments = (await _departmentRepository.GetAllAsync(cancellationToken))
            .ToDictionary(
                department => department.DepartmentId,
                department => department.Name);

        var leaveTypes = (await _leaveTypeRepository.GetAllAsync(cancellationToken))
            .ToDictionary(
                leaveType => leaveType.LeaveTypeId,
                leaveType => leaveType.Name);

        var rows = new List<PendingApprovalDto>(requests.Count);

        foreach (var request in requests)
        {
            // Inactive employees never appear in an approval queue.
            if (!employees.TryGetValue(request.EmployeeId, out var employee) ||
                !employee.IsActive)
            {
                continue;
            }

            rows.Add(new PendingApprovalDto
            {
                LeaveRequestId = request.LeaveRequestId,
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.Name,
                EmployeeEmail = employee.Email,
                DepartmentId = employee.DepartmentId,
                DepartmentName = departments.TryGetValue(
                    employee.DepartmentId,
                    out var departmentName)
                    ? departmentName
                    : string.Empty,
                LeaveTypeId = request.LeaveTypeId,
                LeaveTypeName = leaveTypes.TryGetValue(
                    request.LeaveTypeId,
                    out var leaveTypeName)
                    ? leaveTypeName
                    : string.Empty,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                TotalDays = request.TotalDays,
                Reason = request.Reason,
                Status = request.Status.ToString(),
                CreatedAtUtc = request.CreatedAtUtc
            });
        }

        return rows;
    }

    public async Task ApproveAsync(
        long leaveRequestId,
        int approverEmployeeId,
        Role approverRole,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var (request, approver, employee, leaveType) =
                await ValidateApprovalAsync(
                    leaveRequestId,
                    approverEmployeeId,
                    approverRole,
                    ct);

            EnsureApprovable(employee, leaveType);

            var balance = await LoadBalanceAsync(request, ct);

            var decidedAtUtc = DateTime.UtcNow;

            // Claim the pending request first. If another decision lands in
            // between, the conditional update affects no rows and nothing is
            // deducted. The claim and the balance change commit together.
            var claimed = await _leaveRequestRepository.TryClaimDecisionAsync(
                request.LeaveRequestId,
                LeaveRequestStatus.Pending,
                LeaveRequestStatus.Approved,
                approver.EmployeeId,
                decidedAtUtc,
                null,
                ct);

            if (!claimed)
                throw new InvalidOperationException(
                    "This leave request has already been decided.");

            request.Approve(approver.EmployeeId, decidedAtUtc);
            balance.Consume(request.TotalDays);

            await _leaveRequestRepository.SaveChangesAsync(ct);

            return true;
        }, cancellationToken);
    }

    public async Task RejectAsync(
        long leaveRequestId,
        int approverEmployeeId,
        Role approverRole,
        string comment,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(comment))
            throw new ArgumentException(
                "A rejection comment is required.",
                nameof(comment));

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var (request, approver, employee, leaveType) =
                await ValidateApprovalAsync(
                    leaveRequestId,
                    approverEmployeeId,
                    approverRole,
                    ct);

            EnsureApprovable(employee, leaveType);

            var balance = await LoadBalanceAsync(request, ct);

            var decidedAtUtc = DateTime.UtcNow;

            var claimed = await _leaveRequestRepository.TryClaimDecisionAsync(
                request.LeaveRequestId,
                LeaveRequestStatus.Pending,
                LeaveRequestStatus.Rejected,
                approver.EmployeeId,
                decidedAtUtc,
                comment,
                ct);

            if (!claimed)
                throw new InvalidOperationException(
                    "This leave request has already been decided.");

            request.Reject(approver.EmployeeId, comment, decidedAtUtc);
            balance.Release(request.TotalDays);

            await _leaveRequestRepository.SaveChangesAsync(ct);

            return true;
        }, cancellationToken);
    }

    private static void EnsureApprovable(
        Employee employee,
        LeaveType leaveType)
    {
        if (!employee.IsActive)
            throw new InvalidOperationException(
                "The employee is no longer active, so this request cannot be decided.");

        if (!leaveType.IsActive)
            throw new InvalidOperationException(
                "The leave type is no longer active, so this request cannot be decided.");
    }

    private async Task<LeaveBalance> LoadBalanceAsync(
        LeaveRequest request,
        CancellationToken cancellationToken)
    {
        var year = checked((short)request.StartDate.Year);

        var balance =
            await _leaveBalanceRepository.GetByEmployeeAndLeaveTypeAsync(
                request.EmployeeId,
                request.LeaveTypeId,
                year,
                cancellationToken);

        return balance
            ?? throw new InvalidOperationException("Leave balance not found.");
    }

    private async Task<(LeaveRequest Request,
        Employee Approver,
        Employee Employee,
        LeaveType LeaveType)> ValidateApprovalAsync(
        long leaveRequestId,
        int approverEmployeeId,
        Role approverRole,
        CancellationToken cancellationToken)
    {
        if (leaveRequestId <= 0)
            throw new ArgumentOutOfRangeException(nameof(leaveRequestId));

        if (approverEmployeeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(approverEmployeeId));

        if (approverRole is not Role.Manager and not Role.HR)
            throw new UnauthorizedAccessException(
                "Only managers and HR can approve or reject leave requests.");

        var request = await _leaveRequestRepository.GetByIdAsync(
            leaveRequestId,
            cancellationToken);

        if (request is null)
            throw new KeyNotFoundException("Leave request not found.");

        var approver = await _employeeRepository.GetByIdAsync(
            approverEmployeeId,
            cancellationToken);

        if (approver is null || !approver.IsActive)
            throw new UnauthorizedAccessException(
                "The approver is not an active employee.");

        if (approver.EmployeeId == request.EmployeeId)
            throw new UnauthorizedAccessException(
                "You cannot approve or reject your own leave request.");

        var employee = await _employeeRepository.GetByIdAsync(
            request.EmployeeId,
            cancellationToken);

        if (employee is null)
            throw new InvalidOperationException(
                "The employee for this leave request no longer exists.");

        if (approverRole == Role.Manager &&
            employee.ManagerId != approver.EmployeeId)
        {
            throw new UnauthorizedAccessException(
                "Managers can approve or reject requests only for their direct reports.");
        }

        var leaveType = await _leaveTypeRepository.GetByIdAsync(
            request.LeaveTypeId,
            cancellationToken);

        if (leaveType is null)
            throw new InvalidOperationException(
                "The leave type for this leave request no longer exists.");

        return (request, approver, employee, leaveType);
    }

    public async Task<IReadOnlyList<ManagerHistoryDto>> GetHistoryAsync(
        int approverEmployeeId,
        Role approverRole,
        CancellationToken cancellationToken = default)
    {
        if (approverEmployeeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(approverEmployeeId));

        if (approverRole is not Role.Manager and not Role.HR)
            throw new UnauthorizedAccessException(
                "Only managers and HR can view approval history.");

        var approver = await _employeeRepository.GetByIdAsync(
            approverEmployeeId,
            cancellationToken);

        if (approver is null || !approver.IsActive)
            throw new UnauthorizedAccessException(
                "The approver is not an active employee.");

        var requests = approverRole == Role.HR
            ? await _leaveRequestRepository.GetHrAllHistoryAsync(cancellationToken)
            : await _leaveRequestRepository.GetManagerHistoryAsync(
                approverEmployeeId,
                cancellationToken);

        if (requests.Count == 0)
            return Array.Empty<ManagerHistoryDto>();

        var employees = (await _employeeRepository.GetAllAsync(cancellationToken))
            .ToDictionary(e => e.EmployeeId);

        var departments = (await _departmentRepository.GetAllAsync(cancellationToken))
            .ToDictionary(d => d.DepartmentId, d => d.Name);

        var leaveTypes = (await _leaveTypeRepository.GetAllAsync(cancellationToken))
            .ToDictionary(l => l.LeaveTypeId, l => l.Name);

        return requests.Select(r => new ManagerHistoryDto
        {
            LeaveRequestId = r.LeaveRequestId,
            EmployeeId = r.EmployeeId,
            EmployeeName = employees.TryGetValue(r.EmployeeId, out var emp)
                ? emp.Name : string.Empty,
            EmployeeEmail = employees.TryGetValue(r.EmployeeId, out var emp2)
                ? emp2.Email : string.Empty,
            DepartmentId = employees.TryGetValue(r.EmployeeId, out var emp3)
                ? emp3.DepartmentId : 0,
            DepartmentName = employees.TryGetValue(r.EmployeeId, out var emp4)
                && departments.TryGetValue(emp4.DepartmentId, out var dn)
                ? dn : string.Empty,
            LeaveTypeId = r.LeaveTypeId,
            LeaveTypeName = leaveTypes.TryGetValue(
                r.LeaveTypeId, out var lt) ? lt : string.Empty,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            TotalDays = r.TotalDays,
            Reason = r.Reason,
            Status = r.Status.ToString(),
            CreatedAtUtc = r.CreatedAtUtc,
            DecidedAtUtc = r.DecidedAtUtc,
            ApprovedBy = r.ApprovedBy,
            DecisionComment = r.DecisionComment,
            ApproverName = r.ApprovedBy is { } aid &&
                employees.TryGetValue(aid, out var approverEmp)
                ? approverEmp.Name : "Unknown"
        }).ToList().AsReadOnly();
    }
}
