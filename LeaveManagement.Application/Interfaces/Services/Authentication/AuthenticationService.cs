
using LeaveManagement.Application.DTOs.Authentication;
using LeaveManagement.Application.Interfaces.Repositories;
using LeaveManagement.Application.Interfaces.Security;
using LeaveManagement.Application.Interfaces.Services;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Identity;

namespace LeaveManagement.Application.Services.Authentication;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthenticationService(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var user = await _userRepository.GetByUsernameAsync(
            dto.Username,
            cancellationToken);

        if (user is null ||
            !_passwordHasher.VerifyPassword(
                dto.Password,
                user.PasswordHash))
        {
            throw new UnauthorizedAccessException(
                "Invalid username or password.");
        }

        return new LoginResponseDto
        {
            AccessToken = _jwtTokenService.GenerateAccessToken(user),
            TokenType = "Bearer",
            ExpiresAtUtc = _jwtTokenService.GetAccessTokenExpiryUtc(),
            Username = user.Username,
            Role = user.Role.ToString()
        };
    }

    public async Task<int> RegisterEmployeeAsync(
        RegisterEmployeeDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!Enum.IsDefined(dto.Role))
        {
            throw new ArgumentException("Invalid role selected.");
        }

        var email = dto.Email.Trim();

        // Check whether an employee with this email already exists.
        if (await _employeeRepository.ExistsByEmailAsync(
                email,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "An employee with this email already exists.");
        }

        // Create the employee profile.
        var employee = new Employee(
            dto.Name,
            email,
            dto.DepartmentId,
            dto.JoinDate);

        await _employeeRepository.AddAsync(
            employee,
            cancellationToken);

        await _employeeRepository.SaveChangesAsync(
            cancellationToken);

        // Create the login account with the Employee role.
        var passwordHash = _passwordHasher.HashPassword(dto.Password);

        var user = new ApplicationUser(
            employee.EmployeeId,
            email,
            passwordHash,
            dto.Role);

        await _userRepository.AddAsync(
            user,
            cancellationToken);

        await _userRepository.SaveChangesAsync(
            cancellationToken);

        return employee.EmployeeId;
    }
}