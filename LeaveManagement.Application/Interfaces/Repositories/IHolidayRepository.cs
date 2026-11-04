using LeaveManagement.Domain.Leave;

namespace LeaveManagement.Application.Interfaces.Repositories;

public interface IHolidayRepository
{
    Task<IReadOnlyList<Holiday>> GetByYearAsync(
        short year,
        CancellationToken cancellationToken = default);

    Task<Holiday?> GetByIdAsync(
        int holidayId,
        CancellationToken cancellationToken = default);

    Task<Holiday?> GetByDateAsync(
        DateOnly holidayDate,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Holiday holiday,
        CancellationToken cancellationToken = default);

    void Remove(Holiday holiday);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
