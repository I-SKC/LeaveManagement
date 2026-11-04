
using System.Security.Claims;
using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Application.Interfaces.Services;
using LeaveManagement.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class LeaveBalanceController : ControllerBase
{
    private readonly ILeaveBalanceService _leaveBalanceService;
    private readonly IEmployeeService _employeeService;

    public LeaveBalanceController(
        ILeaveBalanceService leaveBalanceService,
        IEmployeeService employeeService)
    {
        _leaveBalanceService = leaveBalanceService;
        _employeeService = employeeService;
    }

    [HttpGet("employee/{employeeId:int}")]
    [Authorize(Roles = "Employee,Manager,HR")]
    public async Task<ActionResult<IReadOnlyList<LeaveBalanceDto>>> GetForEmployee(
        int employeeId,
        [FromQuery] short? year,
        CancellationToken cancellationToken)
    {
        if (employeeId <= 0)
        {
            return BadRequest(new
            {
                message = "EmployeeId must be greater than zero."
            });
        }

        var employeeIdClaim = User.FindFirstValue("EmployeeId");

        if (!int.TryParse(employeeIdClaim, out var currentEmployeeId))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = "Employee identity is missing from the access token." });
        }

        // Employees can read only their own balance.
        if (User.IsInRole(nameof(Role.Employee)) &&
            currentEmployeeId != employeeId)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = "Employees can view only their own leave balance." });
        }

        // Managers can read only their direct reports' balances. HR is unrestricted.
        if (User.IsInRole(nameof(Role.Manager)) &&
            currentEmployeeId != employeeId)
        {
            var employee = await _employeeService.GetByIdAsync(
                employeeId,
                cancellationToken);

            if (employee is null ||
                employee.ManagerId != currentEmployeeId)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { message = "Managers can view balances only for their direct reports." });
            }
        }

        var selectedYear = year ?? checked((short)DateTime.UtcNow.Year);

        if (selectedYear < 1)
        {
            return BadRequest(new
            {
                message = "Year must be greater than zero."
            });
        }

        var balances = await _leaveBalanceService.GetMyBalancesAsync(
            employeeId,
            selectedYear,
            cancellationToken);

        return Ok(balances);
    }
}