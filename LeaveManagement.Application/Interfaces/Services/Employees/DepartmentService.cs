using LeaveManagement.Application.DTOs.Employees;
using LeaveManagement.Application.Interfaces.Repositories;
using LeaveManagement.Application.Interfaces.Services;
using LeaveManagement.Domain.Employees;

namespace LeaveManagement.Application.Services.Employees;

public sealed class DepartmentService : IDepartmentService
{
    private const int MaxNameLength = 100;

    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public DepartmentService(
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository)
    {
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var departments = await _departmentRepository.GetAllAsync(
            cancellationToken);

        return departments.Select(MapToDto).ToList();
    }

    public async Task<DepartmentDto?> GetByIdAsync(
        int departmentId,
        CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetByIdAsync(
            departmentId,
            cancellationToken);

        return department is null ? null : MapToDto(department);
    }

    public async Task<DepartmentDto> CreateAsync(
        CreateDepartmentDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var name = NormalizeName(dto.Name);

        var existing = (await _departmentRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(department => string.Equals(
                department.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"A department named \"{existing.Name}\" already exists.");
        }

        var department = new Department(name);

        await _departmentRepository.AddAsync(
            department,
            cancellationToken);

        await _departmentRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(department);
    }

    public async Task<DepartmentDto?> UpdateAsync(
        int departmentId,
        UpdateDepartmentDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (departmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(departmentId));
        }

        var department = await _departmentRepository.GetByIdAsync(
            departmentId,
            cancellationToken);

        if (department is null)
        {
            return null;
        }

        var name = NormalizeName(dto.Name);

        var duplicate = (await _departmentRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(other => other.DepartmentId != departmentId &&
                string.Equals(other.Name, name, StringComparison.OrdinalIgnoreCase));

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"A department named \"{duplicate.Name}\" already exists.");
        }

        department.Rename(name);

        await _departmentRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(department);
    }

    public async Task<bool> DeactivateAsync(
        int departmentId,
        CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetByIdAsync(
            departmentId,
            cancellationToken);

        if (department is null)
        {
            return false;
        }

        var assignedEmployees = await _employeeRepository.CountByDepartmentAsync(
            departmentId,
            cancellationToken);

        if (assignedEmployees > 0)
        {
            // Departments are referenced by employees, so they are retired
            // instead of deleted and only while nobody is assigned to them.
            throw new InvalidOperationException(
                $"\"{department.Name}\" still has {assignedEmployees} employee(s) assigned. " +
                "Move them to another department first.");
        }

        department.Deactivate();

        await _departmentRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static string NormalizeName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Department name is required.");
        }

        if (trimmed.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Department name cannot exceed {MaxNameLength} characters.");
        }

        return trimmed;
    }

    private static DepartmentDto MapToDto(Department department)
    {
        return new DepartmentDto
        {
            DepartmentId = department.DepartmentId,
            Name = department.Name,
            IsActive = department.IsActive
        };
    }
}
