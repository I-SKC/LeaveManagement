
using LeaveManagement.Application.Interfaces.Security;
using LeaveManagement.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace LeaveManagement.Infrastructure.Security;

public sealed class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<ApplicationUser> _passwordHasher = new();

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return _passwordHasher.HashPassword(
            user: null!,
            password: password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var result = _passwordHasher.VerifyHashedPassword(
            user: null!,
            hashedPassword: passwordHash,
            providedPassword: password);

        return result != PasswordVerificationResult.Failed;
    }
}
