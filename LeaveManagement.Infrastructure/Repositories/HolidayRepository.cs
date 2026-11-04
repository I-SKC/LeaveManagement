
using LeaveManagement.Application.Interfaces.Repositories;
using LeaveManagement.Domain.Leave;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Repositories;

public sealed class HolidayRepository : IHolidayRepository
{
    private readonly AppDbContext _context;

    public HolidayRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Holiday>> GetByYearAsync(
        short year,
        CancellationToken cancellationToken = default)
    {
        return await _context.Holidays
            .AsNoTracking()
            .Where(h => h.Year == year)
            .OrderBy(h => h.HolidayDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Holiday?> GetByIdAsync(
        int holidayId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Holidays
            .FirstOrDefaultAsync(h => h.HolidayId == holidayId, cancellationToken);
    }

    public async Task<Holiday?> GetByDateAsync(
        DateOnly holidayDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Holidays
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HolidayDate == holidayDate, cancellationToken);
    }

    public async Task AddAsync(
        Holiday holiday,
        CancellationToken cancellationToken = default)
    {
        await _context.Holidays.AddAsync(
            holiday, cancellationToken);
    }

    public void Remove(Holiday holiday)
    {
        _context.Holidays.Remove(holiday);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
