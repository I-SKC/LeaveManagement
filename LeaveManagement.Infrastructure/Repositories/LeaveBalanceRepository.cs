
using LeaveManagement.Application.Interfaces.Repositories;
using LeaveManagement.Domain.Leave;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Repositories;

public sealed class LeaveBalanceRepository : ILeaveBalanceRepository
{
    private readonly AppDbContext _context;

    public LeaveBalanceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveBalance?> GetByEmployeeAndLeaveTypeAsync(
        int employeeId,
        int leaveTypeId,
        short year,
        CancellationToken cancellationToken = default)
    {
        return await _context.LeaveBalances
            .FirstOrDefaultAsync(
                b => b.EmployeeId == employeeId
                     && b.LeaveTypeId == leaveTypeId
                     && b.Year == year,
                cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveBalance>> GetByEmployeeIdAsync(
        int employeeId,
        short year,
        CancellationToken cancellationToken = default)
    {
        // Order by leave type name so balances match the order used by the
        // leave-type list (Apply Leave, My Leaves).
        return await _context.LeaveBalances
            .AsNoTracking()
            .Where(b => b.EmployeeId == employeeId && b.Year == year)
            .Join(
                _context.LeaveTypes,
                balance => balance.LeaveTypeId,
                leaveType => leaveType.LeaveTypeId,
                (balance, leaveType) => new { balance, leaveType.Name })
            .OrderBy(row => row.Name)
            .Select(row => row.balance)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        LeaveBalance balance,
        CancellationToken cancellationToken = default)
    {
        await _context.LeaveBalances.AddAsync(
            balance, cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}