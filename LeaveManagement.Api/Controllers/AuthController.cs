
using LeaveManagement.Application.DTOs.Authentication;
using LeaveManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthController(
        IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authenticationService.LoginAsync(
                dto,
                cancellationToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Invalid credentials must be 401, never an unhandled 500.
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }



    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
    [FromBody] RegisterEmployeeDto dto,
    CancellationToken cancellationToken)
    {
        try
        {
            var employeeId =
                await _authenticationService.RegisterEmployeeAsync(
                    dto, cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    message = "Employee registered successfully.",
                    employeeId
                });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

}