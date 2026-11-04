
using LeaveManagement.Application.Interfaces.Repositories;
using LeaveManagement.Domain.Leave;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Repositories;

public sealed class LeaveTypeRepository : ILeaveTypeRepository
{
    private readonly AppDbContext _context;

    public LeaveTypeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveType?> GetByIdAsync(
        int leaveTypeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.LeaveTypes
            .FirstOrDefaultAsync(
                l => l.LeaveTypeId == leaveTypeId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveType>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.LeaveTypes
            .AsNoTracking()
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        LeaveType leaveType,
        CancellationToken cancellationToken = default)
    {
        await _context.LeaveTypes.AddAsync(
            leaveType, cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}