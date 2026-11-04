
using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Application.Interfaces.Repositories;
using LeaveManagement.Application.Interfaces.Services;
using LeaveManagement.Domain.Leave;

namespace LeaveManagement.Application.Services.Leave;

public sealed class LeaveTypeService : ILeaveTypeService
{
    private readonly ILeaveTypeRepository _leaveTypeRepository;

    public LeaveTypeService(
        ILeaveTypeRepository leaveTypeRepository)
    {
        _leaveTypeRepository = leaveTypeRepository;
    }

    public async Task<IReadOnlyList<LeaveTypeDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var leaveTypes = await _leaveTypeRepository.GetAllAsync(
            cancellationToken);

        return leaveTypes.Select(MapToDto).ToList();
    }

    public async Task<LeaveTypeDto?> GetByIdAsync(
        int leaveTypeId,
        CancellationToken cancellationToken = default)
    {
        var leaveType = await _leaveTypeRepository.GetByIdAsync(
            leaveTypeId,
            cancellationToken);

        return leaveType is null ? null : MapToDto(leaveType);
    }

    public async Task<LeaveTypeDto> CreateAsync(
        CreateLeaveTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var name = (dto.Name ?? string.Empty).Trim();

        if (name.Length == 0)
        {
            throw new ArgumentException("Leave type name is required.");
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Leave type name cannot exceed 100 characters.");
        }

        if (dto.AnnualAllowance < 0 || dto.AnnualAllowance > 365)
        {
            throw new ArgumentException(
                "The annual allowance must be between 0 and 365 days.");
        }

        var existing = (await _leaveTypeRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(leaveType => string.Equals(
                leaveType.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"A leave type named \"{existing.Name}\" already exists.");
        }

        var leaveType = new LeaveType(
            name,
            dto.AnnualAllowance);

        // The domain constructor creates approval-required types; the request
        // decides the actual policy.
        leaveType.Update(
            name,
            dto.AnnualAllowance,
            dto.RequiresManagerApproval);

        await _leaveTypeRepository.AddAsync(
            leaveType,
            cancellationToken);

        await _leaveTypeRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(leaveType);
    }

    public async Task<LeaveTypeDto?> UpdateAsync(
        int leaveTypeId,
        CreateLeaveTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (leaveTypeId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaveTypeId));
        }

        var leaveType = await _leaveTypeRepository.GetByIdAsync(
            leaveTypeId,
            cancellationToken);

        if (leaveType is null)
        {
            return null;
        }

        var name = (dto.Name ?? string.Empty).Trim();

        if (name.Length == 0)
        {
            throw new ArgumentException("Leave type name is required.");
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Leave type name cannot exceed 100 characters.");
        }

        if (dto.AnnualAllowance < 0 || dto.AnnualAllowance > 365)
        {
            throw new ArgumentException(
                "The annual allowance must be between 0 and 365 days.");
        }

        var duplicate = (await _leaveTypeRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(leaveType => leaveType.LeaveTypeId != leaveTypeId &&
                string.Equals(leaveType.Name, name, StringComparison.OrdinalIgnoreCase));

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"A leave type named \"{duplicate.Name}\" already exists.");
        }

        leaveType.Update(
            name,
            dto.AnnualAllowance,
            dto.RequiresManagerApproval);

        await _leaveTypeRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(leaveType);
    }

    public async Task<LeaveTypeDto?> SetActiveAsync(
        int leaveTypeId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var leaveType = await _leaveTypeRepository.GetByIdAsync(
            leaveTypeId,
            cancellationToken);

        if (leaveType is null)
        {
            return null;
        }

        // Deactivation hides the policy from new requests but keeps every
        // existing request and balance untouched.
        if (isActive)
        {
            leaveType.Activate();
        }
        else
        {
            leaveType.Deactivate();
        }

        await _leaveTypeRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(leaveType);
    }

    private static LeaveTypeDto MapToDto(LeaveType leaveType)
    {
        return new LeaveTypeDto
        {
            LeaveTypeId = leaveType.LeaveTypeId,
            Name = leaveType.Name,
            AnnualAllowance = leaveType.AnnualAllowance,
            IsActive = leaveType.IsActive,
            RequiresManagerApproval =
                leaveType.RequiresManagerApproval
        };
    }
}