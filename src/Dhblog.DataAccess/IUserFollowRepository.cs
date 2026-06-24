using Dhblog.Database.Entities;

namespace Dhblog.DataAccess;

public interface IUserFollowRepository
{
    Task FollowAsync(UserFollow follow, CancellationToken ct = default);
    Task UnfollowAsync(string followerId, string followingId, CancellationToken ct = default);
    Task<bool> IsFollowingAsync(string followerId, string followingId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetFollowingIdsAsync(string followerId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetFollowerIdsAsync(string followingId, CancellationToken ct = default);
}

public interface IPasswordResetRepository
{
    Task CreateAsync(PasswordResetToken token, CancellationToken ct = default);
    Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task MarkUsedAsync(string tokenId, CancellationToken ct = default);
}
