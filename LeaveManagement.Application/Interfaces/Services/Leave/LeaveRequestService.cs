using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Application.Interfaces.Repositories;
using LeaveManagement.Application.Interfaces.Services;
using LeaveManagement.Domain.Leave;
using LeaveManagement.Domain.Services;

namespace LeaveManagement.Application.Services.Leave;

public sealed class LeaveRequestService : ILeaveRequestService
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;
    private readonly ILeaveTypeRepository _leaveTypeRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly IBusinessClock _businessClock;

    public LeaveRequestService(
        ILeaveRequestRepository leaveRequestRepository,
        ILeaveBalanceRepository leaveBalanceRepository,
        ILeaveTypeRepository leaveTypeRepository,
        IHolidayRepository holidayRepository,
        IBusinessClock businessClock)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _leaveBalanceRepository = leaveBalanceRepository;
        _leaveTypeRepository = leaveTypeRepository;
        _holidayRepository = holidayRepository;
        _businessClock = businessClock;
    }

    public async Task<LeaveRequestDto> CreateAsync(
        int employeeId,
        CreateLeaveRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (employeeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(employeeId));

        // Dates before the current business date cannot be requested.
        if (dto.StartDate < _businessClock.Today)
            throw new InvalidOperationException(
                "Leave cannot start on a past date.");

        var leaveType = await _leaveTypeRepository.GetByIdAsync(
            dto.LeaveTypeId, cancellationToken);

        if (leaveType is null || !leaveType.IsActive)
            throw new InvalidOperationException(
                "The selected leave type is unavailable.");

        var year = checked((short)dto.StartDate.Year);

        if (dto.StartDate.Year != dto.EndDate.Year)
            throw new InvalidOperationException(
                "Leave requests must fall within a single calendar year.");

        var holidays = await _holidayRepository.GetByYearAsync(
            year, cancellationToken);

        var totalDays = WorkingDayCalculator.Count(
            dto.StartDate, dto.EndDate, holidays);

        if (totalDays <= 0)
            throw new InvalidOperationException(
                "The selected dates contain no working days.");

        // Pending and approved requests block any overlapping range.
        // Cancelled and rejected requests do not.
        if (await _leaveRequestRepository.HasOverlappingRequestAsync(
                employeeId,
                dto.StartDate,
                dto.EndDate,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "You already have a leave request that overlaps these dates.");
        }

        var balance =
            await _leaveBalanceRepository.GetByEmployeeAndLeaveTypeAsync(
                employeeId, dto.LeaveTypeId, year, cancellationToken);

        if (balance is null)
            throw new InvalidOperationException(
                "No leave balance exists for this employee and leave type.");

        balance.Reserve(totalDays);

        var request = new LeaveRequest(
            employeeId,
            dto.LeaveTypeId,
            dto.StartDate,
            dto.EndDate,
            totalDays,
            dto.Reason,
            DateTime.UtcNow);

        await _leaveRequestRepository.AddAsync(
            request, cancellationToken);

        // The reserved balance and the new request are tracked by the same
        // scoped AppDbContext, so a single save commits them atomically.
        await _leaveRequestRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(request);
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetMyRequestsAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var requests = await _leaveRequestRepository.GetByEmployeeIdAsync(
            employeeId, cancellationToken);

        return requests.Select(MapToDto).ToList();
    }

    public async Task<LeaveRequestDto?> GetByIdAsync(
        long leaveRequestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _leaveRequestRepository.GetByIdAsync(
            leaveRequestId, cancellationToken);

        return request is null ? null : MapToDto(request);
    }

    public async Task<LeaveRequestDto?> CancelAsync(
        long leaveRequestId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        if (leaveRequestId <= 0)
            throw new ArgumentOutOfRangeException(nameof(leaveRequestId));

        if (employeeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(employeeId));

        var request = await _leaveRequestRepository.GetByIdAsync(
            leaveRequestId, cancellationToken);

        if (request is null)
            return null;

        if (request.EmployeeId != employeeId)
        {
            throw new UnauthorizedAccessException(
                "Only the employee who submitted the request can cancel it.");
        }

        // Capture the prior status: the domain only allows Pending requests,
        // or Approved requests whose start date is still in the future.
        var wasPending = request.Status == LeaveRequestStatus.Pending;
        var wasApproved = request.Status == LeaveRequestStatus.Approved;

        request.Cancel(employeeId, _businessClock.Today);

        var year = checked((short)request.StartDate.Year);

        var balance =
            await _leaveBalanceRepository.GetByEmployeeAndLeaveTypeAsync(
                request.EmployeeId,
                request.LeaveTypeId,
                year,
                cancellationToken);

        if (balance is null)
            throw new InvalidOperationException("Leave balance not found.");

        if (wasPending)
        {
            // Reserved days were never consumed: release the reservation.
            balance.Release(request.TotalDays);
        }
        else if (wasApproved)
        {
            // Approval consumed the days from the allowance: give them back.
            var leaveType = await _leaveTypeRepository.GetByIdAsync(
                request.LeaveTypeId, cancellationToken)
                ?? throw new InvalidOperationException("Leave type not found.");

            balance.Refund(request.TotalDays, leaveType.AnnualAllowance);
        }

        // Status change and balance adjustment are tracked by the same scoped
        // AppDbContext, so one save commits them atomically.
        await _leaveRequestRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(request);
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var requests = await _leaveRequestRepository.GetAllAsync(cancellationToken);

        return requests.Select(MapToDto).ToList();
    }

    private static LeaveRequestDto MapToDto(LeaveRequest request)
    {
        return new LeaveRequestDto
        {
            LeaveRequestId = request.LeaveRequestId,
            EmployeeId = request.EmployeeId,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalDays = request.TotalDays,
            Reason = request.Reason,
            Status = request.Status.ToString(),
            ApprovedBy = request.ApprovedBy,
            CreatedAtUtc = request.CreatedAtUtc,
            DecisionComment = request.DecisionComment
        };
    }
}