
namespace LeaveManagement.Domain.Identity;

public sealed class ApplicationUser
{
    public int UserId { get; private set; }

    public int EmployeeId { get; private set; }

    public string Username { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public Role Role { get; private set; }

    private ApplicationUser()
    {
    }

    public ApplicationUser(
        int employeeId,
        string username,
        string passwordHash,
        Role role)
    {
        if (employeeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(employeeId));

        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException(
                "Username is required.", nameof(username));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException(
                "Password hash is required.", nameof(passwordHash));

        if (!Enum.IsDefined(role))
            throw new ArgumentException(
                "Invalid role selected.", nameof(role));

        EmployeeId = employeeId;
        Username = username.Trim();
        PasswordHash = passwordHash;
        Role = role;
    }

    public void UpdatePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException(
                "Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
    }

    public void UpdateRole(Role role)
    {
        if (!Enum.IsDefined(role))
            throw new ArgumentException(
                "Invalid role selected.", nameof(role));

        Role = role;
    }
}