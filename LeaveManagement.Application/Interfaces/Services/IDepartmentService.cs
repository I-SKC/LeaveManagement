using LeaveManagement.Application.DTOs.Employees;

namespace LeaveManagement.Application.Interfaces.Services;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<DepartmentDto?> GetByIdAsync(
        int departmentId,
        CancellationToken cancellationToken = default);

    Task<DepartmentDto> CreateAsync(
        CreateDepartmentDto dto,
        CancellationToken cancellationToken = default);

    Task<DepartmentDto?> UpdateAsync(
        int departmentId,
        UpdateDepartmentDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        int departmentId,
        CancellationToken cancellationToken = default);
}
