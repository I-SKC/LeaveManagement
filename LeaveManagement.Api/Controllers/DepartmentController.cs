
using LeaveManagement.Application.DTOs.Employees;
using LeaveManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    [Authorize(Roles = "Employee,Manager,HR")]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> GetAll(
        CancellationToken cancellationToken)
        => Ok(await _departmentService.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Employee,Manager,HR")]
    public async Task<ActionResult<DepartmentDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var department = await _departmentService.GetByIdAsync(
            id, cancellationToken);

        return department is null ? NotFound() : Ok(department);
    }

    [HttpPost]
    [Authorize(Roles = "HR")]
    public async Task<ActionResult<DepartmentDto>> Create(
        [FromBody] CreateDepartmentDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var department = await _departmentService.CreateAsync(
                dto, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = department.DepartmentId },
                department);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "HR")]
    public async Task<ActionResult<DepartmentDto>> Update(
        int id,
        [FromBody] UpdateDepartmentDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var department = await _departmentService.UpdateAsync(
                id, dto, cancellationToken);

            return department is null
                ? NotFound(new { message = "Department not found." })
                : Ok(department);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // Departments are retired rather than deleted so historical employee and
    // leave data keeps a valid reference.
    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Deactivate(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var deactivated = await _departmentService.DeactivateAsync(
                id, cancellationToken);

            return deactivated
                ? NoContent()
                : NotFound(new { message = "Department not found." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
