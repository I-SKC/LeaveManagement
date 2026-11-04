
using System.Security.Claims;
using LeaveManagement.Application.DTOs.Employees;
using LeaveManagement.Application.Interfaces.Services;
using LeaveManagement.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    // HR can view all employees.
    // Managers can also view the employee list for management purposes.
    [HttpGet]
    [Authorize(Roles = "Manager,HR")]
    public async Task<ActionResult<IReadOnlyList<EmployeeDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var employees = await _employeeService.GetAllAsync(cancellationToken);
        return Ok(employees);
    }

    // Employees can view their own details.
    // Managers and HR can view employee details by ID.
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Employee,Manager,HR")]
    public async Task<ActionResult<EmployeeDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(
                User.FindFirstValue("EmployeeId"),
                out var currentEmployeeId))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = "Employee identity is missing from the access token." });
        }

        if (User.IsInRole(nameof(Role.Employee)) && id != currentEmployeeId)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = "Employees can view only their own profile." });
        }

        var employee = await _employeeService.GetByIdAsync(
            id,
            cancellationToken);

        if (employee is null)
        {
            return NotFound(new { message = "Employee not found." });
        }

        // Managers can view only their direct reports' profiles. HR is unrestricted.
        if (User.IsInRole(nameof(Role.Manager)) &&
            id != currentEmployeeId &&
            employee.ManagerId != currentEmployeeId)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = "Managers can view profiles only for their direct reports." });
        }

        return Ok(employee);
    }

    // The logged-in user can retrieve their own employee profile.
    [HttpGet("me")]
    [Authorize(Roles = "Employee,Manager,HR")]
    public async Task<ActionResult<EmployeeDto>> GetMe(
        CancellationToken cancellationToken)
    {
        var employeeIdClaim = User.FindFirstValue("EmployeeId");

        if (!int.TryParse(employeeIdClaim, out var employeeId))
        {
            return Forbid();
        }

        var employee = await _employeeService.GetByIdAsync(
            employeeId,
            cancellationToken);

        return employee is null ? NotFound() : Ok(employee);
    }

    [HttpPost]
    [Authorize(Roles = "HR")]
    public async Task<ActionResult<EmployeeDto>> Create(
        [FromBody] CreateEmployeeDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeService.CreateAsync(
                dto,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = employee.EmployeeId },
                employee);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "HR")]
    public async Task<ActionResult<EmployeeDto>> Update(
        int id,
        [FromBody] UpdateEmployeeDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeService.UpdateAsync(
                id,
                dto,
                cancellationToken);

            return employee is null ? NotFound() : Ok(employee);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/activate")]
    [Authorize(Roles = "HR")]
    public async Task<ActionResult<EmployeeDto>> Activate(
        int id,
        CancellationToken cancellationToken)
    {
        var employee = await _employeeService.ReactivateAsync(
            id,
            cancellationToken);

        return employee is null
            ? NotFound(new { message = "Employee not found." })
            : Ok(employee);
    }

    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Deactivate(
        int id,
        CancellationToken cancellationToken)
    {
        // An HR administrator must not lock themselves out of the system.
        if (int.TryParse(User.FindFirstValue("EmployeeId"), out var currentEmployeeId) &&
            currentEmployeeId == id)
        {
            return BadRequest(new
            {
                message = "You cannot deactivate your own account."
            });
        }

        var deactivated = await _employeeService.DeactivateAsync(
            id,
            cancellationToken);

        return deactivated ? NoContent() : NotFound();
    }
}