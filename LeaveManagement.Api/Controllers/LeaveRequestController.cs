
using System.Security.Claims;
using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class LeaveRequestController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly IEmployeeService _employeeService;

    public LeaveRequestController(
        ILeaveRequestService leaveRequestService,
        IEmployeeService employeeService)
    {
        _leaveRequestService = leaveRequestService;
        _employeeService = employeeService;
    }

    [HttpPost("employee/{employeeId:int}")]
    [Authorize(Roles = "Employee,Manager,HR")]
    public async Task<ActionResult<LeaveRequestDto>> Create(
        int employeeId,
        [FromBody] CreateLeaveRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (employeeId <= 0)
        {
            return BadRequest(new
            {
                message = "EmployeeId must be greater than zero."
            });
        }

        if (!TryGetCurrentEmployeeId(out var currentEmployeeId))
        {
            return Unauthorized(new
            {
                message = "Employee identity is missing from the access token."
            });
        }

        // Employees can create leave requests only for themselves.
        if (User.IsInRole("Employee") && employeeId != currentEmployeeId)
        {
            return Forbidden(
                "Employees can create leave requests only for themselves.");
        }

        // Managers can create leave requests only for their direct reports.
        if (User.IsInRole("Manager") &&
            employeeId != currentEmployeeId &&
            !await IsDirectReportAsync(
                employeeId,
                currentEmployeeId,
                cancellationToken))
        {
            return Forbidden(
                "Managers can create leave requests only for their direct reports.");
        }

        try
        {
            var request = await _leaveRequestService.CreateAsync(
                employeeId,
                dto,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = request.LeaveRequestId },
                request);
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

    [HttpGet("employee/{employeeId:int}")]
    [Authorize(Roles = "Employee,Manager,HR")]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> GetForEmployee(
        int employeeId,
        CancellationToken cancellationToken)
    {
        if (employeeId <= 0)
        {
            return BadRequest(new
            {
                message = "EmployeeId must be greater than zero."
            });
        }

        if (!TryGetCurrentEmployeeId(out var currentEmployeeId))
        {
            return Unauthorized(new
            {
                message = "Employee identity is missing from the access token."
            });
        }

        // Employees can view only their own leave requests.
        if (User.IsInRole("Employee") && employeeId != currentEmployeeId)
        {
            return Forbidden(
                "Employees can view only their own leave requests.");
        }

        // Managers can view only their direct reports' leave requests.
        if (User.IsInRole("Manager") &&
            employeeId != currentEmployeeId &&
            !await IsDirectReportAsync(
                employeeId,
                currentEmployeeId,
                cancellationToken))
        {
            return Forbidden(
                "Managers can view leave requests only for their direct reports.");
        }

        var requests = await _leaveRequestService.GetMyRequestsAsync(
            employeeId,
            cancellationToken);

        return Ok(requests);
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "Employee,Manager,HR")]
    public async Task<ActionResult<LeaveRequestDto>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Leave request ID must be greater than zero."
            });
        }

        if (!TryGetCurrentEmployeeId(out var currentEmployeeId))
        {
            return Unauthorized(new
            {
                message = "Employee identity is missing from the access token."
            });
        }

        var request = await _leaveRequestService.GetByIdAsync(
            id,
            cancellationToken);

        if (request is null)
        {
            return NotFound();
        }

        // Employees can view only their own leave requests.
        if (User.IsInRole("Employee") &&
            request.EmployeeId != currentEmployeeId)
        {
            return Forbidden(
                "Employees can view only their own leave requests.");
        }

        // Managers can view only their direct reports' leave requests.
        if (User.IsInRole("Manager") &&
            request.EmployeeId != currentEmployeeId &&
            !await IsDirectReportAsync(
                request.EmployeeId,
                currentEmployeeId,
                cancellationToken))
        {
            return Forbidden(
                "Managers can view leave requests only for their direct reports.");
        }

        return Ok(request);
    }

    [HttpPost("{id:long}/cancel")]
    [Authorize(Roles = "Employee,Manager,HR")]
    public async Task<ActionResult<LeaveRequestDto>> Cancel(
        long id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Leave request ID must be greater than zero."
            });
        }

        if (!TryGetCurrentEmployeeId(out var currentEmployeeId))
        {
            return Unauthorized(new
            {
                message = "Employee identity is missing from the access token."
            });
        }

        try
        {
            var request = await _leaveRequestService.CancelAsync(
                id,
                currentEmployeeId,
                cancellationToken);

            return request is null ? NotFound() : Ok(request);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbidden(ex.Message);
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

    private async Task<bool> IsDirectReportAsync(
        int employeeId,
        int managerEmployeeId,
        CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetByIdAsync(
            employeeId,
            cancellationToken);

        return employee is not null &&
               employee.ManagerId == managerEmployeeId;
    }

    private ObjectResult Forbidden(string message)
    {
        return StatusCode(
            StatusCodes.Status403Forbidden,
            new { message });
    }

    [HttpGet("all")]
    [Authorize(Roles = "HR")]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> GetAllRequests(
        CancellationToken cancellationToken)
    {
        var requests = await _leaveRequestService.GetAllAsync(cancellationToken);

        return Ok(requests);
    }

    private bool TryGetCurrentEmployeeId(out int employeeId)
    {
        employeeId = 0;

        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        return int.TryParse(employeeIdClaim, out employeeId)
            && employeeId > 0;
    }
}