
using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class LeaveTypeController : ControllerBase
{
    private readonly ILeaveTypeService _leaveTypeService;

    public LeaveTypeController(ILeaveTypeService leaveTypeService)
    {
        _leaveTypeService = leaveTypeService;
    }

    [HttpGet]
    [Authorize(Roles = "Employee,Manager,HR")]
    public async Task<ActionResult<IReadOnlyList<LeaveTypeDto>>> GetAll(
        CancellationToken cancellationToken)
        => Ok(await _leaveTypeService.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Employee,Manager,HR")]
    public async Task<ActionResult<LeaveTypeDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var leaveType = await _leaveTypeService.GetByIdAsync(
            id, cancellationToken);

        return leaveType is null ? NotFound() : Ok(leaveType);
    }

    // Leave policy configuration belongs to HR only.
    [HttpPost]
    [Authorize(Roles = "HR")]
    public async Task<ActionResult<LeaveTypeDto>> Create(
        [FromBody] CreateLeaveTypeDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var leaveType = await _leaveTypeService.CreateAsync(
                dto, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = leaveType.LeaveTypeId },
                leaveType);
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
    public async Task<ActionResult<LeaveTypeDto>> Update(
        int id,
        [FromBody] CreateLeaveTypeDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var leaveType = await _leaveTypeService.UpdateAsync(
                id,
                dto,
                cancellationToken);

            return leaveType is null
                ? NotFound(new { message = "Leave type not found." })
                : Ok(leaveType);
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

    [HttpPatch("{id:int}/activate")]
    [Authorize(Roles = "HR")]
    public Task<ActionResult<LeaveTypeDto>> Activate(
        int id,
        CancellationToken cancellationToken)
        => SetActiveAsync(id, true, cancellationToken);

    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Roles = "HR")]
    public Task<ActionResult<LeaveTypeDto>> Deactivate(
        int id,
        CancellationToken cancellationToken)
        => SetActiveAsync(id, false, cancellationToken);

    private async Task<ActionResult<LeaveTypeDto>> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var leaveType = await _leaveTypeService.SetActiveAsync(
            id,
            isActive,
            cancellationToken);

        return leaveType is null
            ? NotFound(new { message = "Leave type not found." })
            : Ok(leaveType);
    }
}
