using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Application.Interfaces.Repositories;
using LeaveManagement.Application.Interfaces.Services;
using LeaveManagement.Domain.Leave;

namespace LeaveManagement.Application.Services.Leave;

public sealed class HolidayService : IHolidayService
{
    private const int MaxNameLength = 150;
    private const int MinYear = 2000;
    private const int MaxYear = 2100;

    private readonly IHolidayRepository _holidayRepository;

    public HolidayService(IHolidayRepository holidayRepository)
    {
        _holidayRepository = holidayRepository;
    }

    public async Task<IReadOnlyList<HolidayDto>> GetByYearAsync(
        short year,
        CancellationToken cancellationToken = default)
    {
        ValidateYear(year);

        var holidays = await _holidayRepository.GetByYearAsync(
            year,
            cancellationToken);

        return holidays.Select(MapToDto).ToList();
    }

    public async Task<HolidayDto?> GetByIdAsync(
        int holidayId,
        CancellationToken cancellationToken = default)
    {
        var holiday = await _holidayRepository.GetByIdAsync(
            holidayId,
            cancellationToken);

        return holiday is null ? null : MapToDto(holiday);
    }

    public async Task<HolidayDto> CreateAsync(
        SaveHolidayDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var name = NormalizeName(dto.Name);

        ValidateYear(checked((short)dto.HolidayDate.Year));

        var existing = await _holidayRepository.GetByDateAsync(
            dto.HolidayDate,
            cancellationToken);

        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"{dto.HolidayDate:dd MMM yyyy} is already configured as \"{existing.Name}\".");
        }

        var holiday = new Holiday(dto.HolidayDate, name);

        await _holidayRepository.AddAsync(holiday, cancellationToken);
        await _holidayRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(holiday);
    }

    public async Task<HolidayDto?> UpdateAsync(
        int holidayId,
        SaveHolidayDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (holidayId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(holidayId));
        }

        var holiday = await _holidayRepository.GetByIdAsync(
            holidayId,
            cancellationToken);

        if (holiday is null)
        {
            return null;
        }

        var name = NormalizeName(dto.Name);

        ValidateYear(checked((short)dto.HolidayDate.Year));

        var duplicate = await _holidayRepository.GetByDateAsync(
            dto.HolidayDate,
            cancellationToken);

        if (duplicate is not null && duplicate.HolidayId != holidayId)
        {
            throw new InvalidOperationException(
                $"{dto.HolidayDate:dd MMM yyyy} is already configured as \"{duplicate.Name}\".");
        }

        holiday.Reschedule(dto.HolidayDate);
        holiday.Rename(name);

        await _holidayRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(holiday);
    }

    public async Task<bool> DeleteAsync(
        int holidayId,
        CancellationToken cancellationToken = default)
    {
        var holiday = await _holidayRepository.GetByIdAsync(
            holidayId,
            cancellationToken);

        if (holiday is null)
        {
            return false;
        }

        _holidayRepository.Remove(holiday);

        await _holidayRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static void ValidateYear(short year)
    {
        if (year < MinYear || year > MaxYear)
        {
            throw new ArgumentException(
                $"Holidays can only be configured between {MinYear} and {MaxYear}.");
        }
    }

    private static string NormalizeName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Holiday name is required.");
        }

        if (trimmed.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Holiday name cannot exceed {MaxNameLength} characters.");
        }

        return trimmed;
    }

    private static HolidayDto MapToDto(Holiday holiday)
    {
        return new HolidayDto
        {
            HolidayId = holiday.HolidayId,
            HolidayDate = holiday.HolidayDate,
            Name = holiday.Name,
            Year = holiday.Year
        };
    }
}
