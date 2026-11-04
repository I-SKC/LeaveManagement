using LeaveManagement.Domain.Identity;
using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Application.DTOs.Authentication;

public sealed class RegisterEmployeeDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    public DateOnly JoinDate { get; set; } =
        DateOnly.FromDateTime(DateTime.Today);

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public Role Role { get; set; } = Role.Employee;
}