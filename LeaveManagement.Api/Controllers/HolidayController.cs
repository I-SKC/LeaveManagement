
using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class HolidayController : ControllerBase
{
    private readonly IHolidayService _holidayService;

    public HolidayController(IHolidayService holidayService)
    {
        _holidayService = holidayService;
    }

    [HttpGet]
    [Authorize(Roles = "Employee,Manager,HR")]
    public async Task<ActionResult<IReadOnlyList<HolidayDto>>> GetByYear(
        [FromQuery] short? year,
        CancellationToken cancellationToken)
    {
        var selectedYear = year ?? checked((short)DateTime.UtcNow.Year);

        if (selectedYear < 1)
        {
            return BadRequest(new
            {
                message = "Year must be greater than zero."
            });
        }

        try
        {
            var holidays = await _holidayService.GetByYearAsync(
                selectedYear,
                cancellationToken);

            return Ok(holidays);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Employee,Manager,HR")]
    public async Task<ActionResult<HolidayDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var holiday = await _holidayService.GetByIdAsync(id, cancellationToken);

        return holiday is null ? NotFound() : Ok(holiday);
    }

    [HttpPost]
    [Authorize(Roles = "HR")]
    public async Task<ActionResult<HolidayDto>> Create(
        [FromBody] SaveHolidayDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var holiday = await _holidayService.CreateAsync(
                dto, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = holiday.HolidayId },
                holiday);
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
    public async Task<ActionResult<HolidayDto>> Update(
        int id,
        [FromBody] SaveHolidayDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var holiday = await _holidayService.UpdateAsync(
                id, dto, cancellationToken);

            return holiday is null
                ? NotFound(new { message = "Holiday not found." })
                : Ok(holiday);
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

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _holidayService.DeleteAsync(id, cancellationToken);

        return deleted
            ? NoContent()
            : NotFound(new { message = "Holiday not found." });
    }
}
