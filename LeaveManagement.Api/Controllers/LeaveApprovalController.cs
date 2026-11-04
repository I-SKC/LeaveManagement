
using System.Security.Claims;
using LeaveManagement.Api.Models;
using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Application.Interfaces.Services;
using LeaveManagement.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class LeaveApprovalController : ControllerBase
{
    private readonly ILeaveApprovalService _leaveApprovalService;
    private readonly IEmployeeService _employeeService;

    public LeaveApprovalController(
        ILeaveApprovalService leaveApprovalService,
        IEmployeeService employeeService)
    {
        _leaveApprovalService = leaveApprovalService;
        _employeeService = employeeService;
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Manager,HR")]
    public async Task<ActionResult<IReadOnlyList<PendingApprovalDto>>> GetPending(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentEmployeeId(out var approverEmployeeId) ||
            !TryGetCurrentRole(out var approverRole))
        {
            return Unauthorized(new
            {
                message = "Valid employee identity and role are required."
            });
        }

        try
        {
            var pending = await _leaveApprovalService.GetPendingAsync(
                approverEmployeeId,
                approverRole,
                cancellationToken);

            return Ok(pending);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = ex.Message });
        }
    }

    [HttpPost("{leaveRequestId:long}/approve")]
    [Authorize(Roles = "Manager,HR")]
    public async Task<IActionResult> Approve(
        long leaveRequestId,
        [FromBody] LeaveDecisionDto dto,
        CancellationToken cancellationToken)
    {
        if (leaveRequestId <= 0)
        {
            return BadRequest(new
            {
                message = "Leave request ID must be greater than zero."
            });
        }

        if (!TryGetCurrentEmployeeId(out var approverEmployeeId) ||
            !TryGetCurrentRole(out var approverRole))
        {
            return Unauthorized(new
            {
                message = "Valid employee identity and role are required."
            });
        }

        try
        {
            await _leaveApprovalService.ApproveAsync(
                leaveRequestId,
                approverEmployeeId,
                approverRole,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = ex.Message });
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

    [HttpPost("{leaveRequestId:long}/reject")]
    [Authorize(Roles = "Manager,HR")]
    public async Task<IActionResult> Reject(
        long leaveRequestId,
        [FromBody] LeaveDecisionDto dto,
        CancellationToken cancellationToken)
    {
        if (leaveRequestId <= 0)
        {
            return BadRequest(new
            {
                message = "Leave request ID must be greater than zero."
            });
        }

        if (!TryGetCurrentEmployeeId(out var approverEmployeeId) ||
            !TryGetCurrentRole(out var approverRole))
        {
            return Unauthorized(new
            {
                message = "Valid employee identity and role are required."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Comment))
        {
            return BadRequest(new
            {
                message = "A rejection comment is required."
            });
        }

        try
        {
            await _leaveApprovalService.RejectAsync(
                leaveRequestId,
                approverEmployeeId,
                approverRole,
                dto.Comment,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = ex.Message });
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

    [HttpGet("history")]
    [Authorize(Roles = "Manager,HR")]
    public async Task<ActionResult<IReadOnlyList<ManagerHistoryDto>>> GetHistory(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentEmployeeId(out var approverEmployeeId) ||
            !TryGetCurrentRole(out var approverRole))
        {
            return Unauthorized(new
            {
                message = "Valid employee identity and role are required."
            });
        }

        IReadOnlyList<ManagerHistoryDto> history;

        try
        {
            history = await _leaveApprovalService.GetHistoryAsync(
                approverEmployeeId,
                approverRole,
                cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = ex.Message });
        }

        // Defensive: managers must only see direct reports' history.
        if (User.IsInRole("Manager") && history.Count > 0)
        {
            foreach (var row in history)
            {
                var employee = await _employeeService.GetByIdAsync(
                    row.EmployeeId,
                    cancellationToken);

                if (employee is null || employee.ManagerId != approverEmployeeId)
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        new { message = "Approval history is restricted to your direct reports." });
            }
        }

        return Ok(history);
    }

    private bool TryGetCurrentEmployeeId(out int employeeId)
    {
        employeeId = 0;

        var claim = User.FindFirst("EmployeeId")?.Value;

        return int.TryParse(claim, out employeeId)
            && employeeId > 0;
    }

    private bool TryGetCurrentRole(out Role role)
    {
        role = default;

        var claim = User.FindFirst(ClaimTypes.Role)?.Value;

        return Enum.TryParse(claim, out role)
            && Enum.IsDefined(role);
    }
}