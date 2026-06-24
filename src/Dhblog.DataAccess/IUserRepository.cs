using Dhblog.Database.Entities;

namespace Dhblog.DataAccess;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string userId, CancellationToken ct = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default);
    Task CreateAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task DeleteAsync(string userId, CancellationToken ct = default);
}
