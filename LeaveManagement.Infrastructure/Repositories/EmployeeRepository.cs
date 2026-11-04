
using LeaveManagement.Application.Interfaces.Repositories;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Repositories;

public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .FirstOrDefaultAsync(
                e => e.EmployeeId == employeeId,
                cancellationToken);
    }

    public async Task<Employee?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .FirstOrDefaultAsync(
                e => e.Email == email,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Employee employee,
        CancellationToken cancellationToken = default)
    {
        await _context.Employees.AddAsync(employee, cancellationToken);
    }

    public async Task<int> CountByDepartmentAsync(
        int departmentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .CountAsync(
                e => e.DepartmentId == departmentId && e.IsActive,
                cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AnyAsync(e => e.Email == email, cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}