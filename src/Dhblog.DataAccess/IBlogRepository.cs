using Dhblog.Database.Entities;

namespace Dhblog.DataAccess;

public interface IBlogRepository
{
    Task<BlogEntry?> GetEntryByIdAsync(string entryId, CancellationToken ct = default);
    Task<IReadOnlyList<BlogEntry>> GetEntriesByUserIdAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<BlogEntry>> GetEntriesByUserIdsAsync(IEnumerable<string> userIds, CancellationToken ct = default);
    Task CreateEntryAsync(BlogEntry entry, CancellationToken ct = default);
    Task UpdateEntryAsync(BlogEntry entry, CancellationToken ct = default);
    Task DeleteEntryAsync(string entryId, CancellationToken ct = default);
    Task<IReadOnlyList<BlogImage>> GetImagesByEntryIdAsync(string entryId, CancellationToken ct = default);
    Task CreateImageAsync(BlogImage image, CancellationToken ct = default);
    Task DeleteImageAsync(string imageId, CancellationToken ct = default);
}
