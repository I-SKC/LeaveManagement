
using LeaveManagement.Application.Interfaces.Repositories;
using LeaveManagement.Domain.Identity;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationUser?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(
                u => u.Username == username,
                cancellationToken);
    }

    public async Task<ApplicationUser?> GetByIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(
                u => u.UserId == userId,
                cancellationToken);
    }

    public async Task AddAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRoleAsync(
    int userId,
    Role role,
    CancellationToken cancellationToken = default)
    {
        var user = await GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException(
                $"User with ID {userId} was not found.");
        }

        user.UpdateRole(role);

        await _context.SaveChangesAsync(cancellationToken);
    }
}