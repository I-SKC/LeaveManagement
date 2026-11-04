using LeaveManagement.Application.DTOs.Employees;

namespace LeaveManagement.Application.Interfaces.Services;

public interface IEmployeeService
{
    Task<EmployeeDto?> GetByIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<EmployeeDto> CreateAsync(
        CreateEmployeeDto dto,
        CancellationToken cancellationToken = default);

    Task<EmployeeDto?> UpdateAsync(
        int employeeId,
        UpdateEmployeeDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<EmployeeDto?> ReactivateAsync(
        int employeeId,
        CancellationToken cancellationToken = default);
}
