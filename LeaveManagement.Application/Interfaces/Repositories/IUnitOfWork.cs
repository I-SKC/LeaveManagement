namespace LeaveManagement.Application.Interfaces.Repositories;

/// <summary>
/// Coordinates repository calls that must commit together. All repositories
/// share the same scoped AppDbContext, so a transaction opened here covers
/// each save performed inside the operation.
/// </summary>
public interface IUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
