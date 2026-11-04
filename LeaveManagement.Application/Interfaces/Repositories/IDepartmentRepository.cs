using LeaveManagement.Domain.Employees;

namespace LeaveManagement.Application.Interfaces.Repositories;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(
        int departmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Department>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Department department,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}