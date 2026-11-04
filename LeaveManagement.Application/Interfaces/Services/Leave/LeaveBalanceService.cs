
using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Application.Interfaces.Repositories;
using LeaveManagement.Application.Interfaces.Services;
using LeaveManagement.Domain.Leave;

namespace LeaveManagement.Application.Services.Leave;

public sealed class LeaveBalanceService : ILeaveBalanceService
{
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;
    private readonly ILeaveTypeRepository _leaveTypeRepository;

    public LeaveBalanceService(
        ILeaveBalanceRepository leaveBalanceRepository,
        ILeaveTypeRepository leaveTypeRepository)
    {
        _leaveBalanceRepository = leaveBalanceRepository;
        _leaveTypeRepository = leaveTypeRepository;
    }

    public async Task<IReadOnlyList<LeaveBalanceDto>> GetMyBalancesAsync(
        int employeeId,
        short year,
        CancellationToken cancellationToken = default)
    {
        if (employeeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(employeeId));
        }

        if (year < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        var existingBalances =
            await _leaveBalanceRepository.GetByEmployeeIdAsync(
                employeeId,
                year,
                cancellationToken);

        var leaveTypes = await _leaveTypeRepository.GetAllAsync(
            cancellationToken);

        var existingLeaveTypeIds = existingBalances
            .Select(balance => balance.LeaveTypeId)
            .ToHashSet();

        var newBalances = leaveTypes
            .Where(leaveType =>
                leaveType.IsActive &&
                !existingLeaveTypeIds.Contains(leaveType.LeaveTypeId))
            .Select(leaveType => new LeaveBalance(
                employeeId,
                leaveType.LeaveTypeId,
                year,
                leaveType.AnnualAllowance))
            .ToList();

        if (newBalances.Count > 0)
        {
            foreach (var balance in newBalances)
            {
                await _leaveBalanceRepository.AddAsync(
                    balance,
                    cancellationToken);
            }

            await _leaveBalanceRepository.SaveChangesAsync(
                cancellationToken);
        }

        var balances = await _leaveBalanceRepository.GetByEmployeeIdAsync(
            employeeId,
            year,
            cancellationToken);

        return balances.Select(balance => new LeaveBalanceDto
        {
            EmployeeId = balance.EmployeeId,
            LeaveTypeId = balance.LeaveTypeId,
            Year = balance.Year,
            AvailableDays = balance.AvailableDays,
            PendingDays = balance.PendingDays
        }).ToList();
    }
}