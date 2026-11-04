using LeaveManagement.Domain.Identity;

namespace LeaveManagement.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task<ApplicationUser?> GetByIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
    
}