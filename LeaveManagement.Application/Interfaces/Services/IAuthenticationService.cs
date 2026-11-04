using LeaveManagement.Application.DTOs.Authentication;

namespace LeaveManagement.Application.Interfaces.Services;

public interface IAuthenticationService
{
    Task<LoginResponseDto> LoginAsync(
        LoginRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<int> RegisterEmployeeAsync(
        RegisterEmployeeDto dto,
        CancellationToken cancellationToken = default);
}