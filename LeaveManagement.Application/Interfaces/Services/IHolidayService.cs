using LeaveManagement.Application.DTOs.Leave;

namespace LeaveManagement.Application.Interfaces.Services;

public interface IHolidayService
{
    Task<IReadOnlyList<HolidayDto>> GetByYearAsync(
        short year,
        CancellationToken cancellationToken = default);

    Task<HolidayDto?> GetByIdAsync(
        int holidayId,
        CancellationToken cancellationToken = default);

    Task<HolidayDto> CreateAsync(
        SaveHolidayDto dto,
        CancellationToken cancellationToken = default);

    Task<HolidayDto?> UpdateAsync(
        int holidayId,
        SaveHolidayDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int holidayId,
        CancellationToken cancellationToken = default);
}
