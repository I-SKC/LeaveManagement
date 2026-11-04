using LeaveManagement.Application.DTOs.Employees;
using LeaveManagement.Application.Interfaces.Repositories;
using LeaveManagement.Application.Interfaces.Services;
using LeaveManagement.Domain.Employees;

namespace LeaveManagement.Application.Services.Employees;

public sealed class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<EmployeeDto?> GetByIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(
            employeeId, cancellationToken);

        return employee is null ? null : MapToDto(employee);
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var employees = await _employeeRepository.GetAllAsync(
            cancellationToken);

        return employees.Select(MapToDto).ToList();
    }

    public async Task<EmployeeDto> CreateAsync(
        CreateEmployeeDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var email = (dto.Email ?? string.Empty).Trim();

        if (email.Length == 0 || !email.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("A valid email address is required.");
        }

        if (await _employeeRepository.ExistsByEmailAsync(
                email, cancellationToken))
        {
            throw new InvalidOperationException(
                "An employee with this email already exists.");
        }

        await EnsureDepartmentIsAssignableAsync(dto.DepartmentId, cancellationToken);
        await EnsureManagerIsAssignableAsync(dto.ManagerId, cancellationToken);

        var employee = new Employee(
            dto.Name,
            email,
            dto.DepartmentId,
            dto.JoinDate,
            dto.ManagerId);

        await _employeeRepository.AddAsync(
            employee, cancellationToken);

        await _employeeRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(employee);
    }

    public async Task<EmployeeDto?> UpdateAsync(
        int employeeId,
        UpdateEmployeeDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var employee = await _employeeRepository.GetByIdAsync(
            employeeId, cancellationToken);

        if (employee is null)
            return null;

        var existingEmployee =
            await _employeeRepository.GetByEmailAsync(
                dto.Email, cancellationToken);

        if (existingEmployee is not null &&
            existingEmployee.EmployeeId != employeeId)
        {
            throw new InvalidOperationException(
                "An employee with this email already exists.");
        }

        await EnsureDepartmentIsAssignableAsync(dto.DepartmentId, cancellationToken);
        await EnsureManagerIsAssignableAsync(dto.ManagerId, cancellationToken);

        employee.UpdateDetails(
            dto.Name,
            dto.Email,
            dto.DepartmentId,
            dto.ManagerId);

        await _employeeRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(employee);
    }

    public async Task<bool> DeactivateAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(
            employeeId, cancellationToken);

        if (employee is null)
            return false;

        employee.Deactivate();

        await _employeeRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<EmployeeDto?> ReactivateAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(
            employeeId,
            cancellationToken);

        if (employee is null)
        {
            return null;
        }

        if (employee.IsActive)
        {
            return MapToDto(employee);
        }

        employee.Activate();

        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(employee);
    }

    private async Task EnsureDepartmentIsAssignableAsync(
        int departmentId,
        CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(
            departmentId,
            cancellationToken);

        if (department is null || !department.IsActive)
        {
            throw new ArgumentException(
                "Select an active department for the employee.");
        }
    }

    private async Task EnsureManagerIsAssignableAsync(
        int? managerId,
        CancellationToken cancellationToken)
    {
        if (!managerId.HasValue)
        {
            return;
        }

        var manager = await _employeeRepository.GetByIdAsync(
            managerId.Value,
            cancellationToken);

        if (manager is null || !manager.IsActive)
        {
            throw new ArgumentException(
                "Select an active employee as the manager.");
        }
    }

    private static EmployeeDto MapToDto(Employee employee)
    {
        return new EmployeeDto
        {
            EmployeeId = employee.EmployeeId,
            Name = employee.Name,
            Email = employee.Email,
            DepartmentId = employee.DepartmentId,
            ManagerId = employee.ManagerId,
            JoinDate = employee.JoinDate,
            IsActive = employee.IsActive
        };
    }
}