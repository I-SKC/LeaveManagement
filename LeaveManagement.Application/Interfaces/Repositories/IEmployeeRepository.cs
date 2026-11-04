using LeaveManagement.Domain.Employees;

namespace LeaveManagement.Application.Interfaces.Repositories;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<Employee?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Employee>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Employee employee,
        CancellationToken cancellationToken = default);

    Task<int> CountByDepartmentAsync(
        int departmentId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}