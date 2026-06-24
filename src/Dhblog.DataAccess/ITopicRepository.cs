using Dhblog.Database.Entities;

namespace Dhblog.DataAccess;

public interface ITopicRepository
{
    Task<Topic?> GetByIdAsync(string topicId, CancellationToken ct = default);
    Task<Topic?> GetByNormalizedKeyAsync(string normalizedKey, CancellationToken ct = default);
    Task<IReadOnlyList<Topic>> SuggestAsync(string prefix, int limit = 10, CancellationToken ct = default);
    Task CreateAsync(Topic topic, CancellationToken ct = default);
    Task<IReadOnlyList<Topic>> ListAsync(CancellationToken ct = default);
    Task UpdateAsync(Topic topic, CancellationToken ct = default);
    Task DeleteAsync(string topicId, CancellationToken ct = default);
    Task DeleteEntryLinksByTopicIdAsync(string topicId, CancellationToken ct = default);
    Task<IReadOnlyList<Topic>> GetByEntryIdAsync(string entryId, CancellationToken ct = default);
    Task LinkEntryTopicAsync(string entryId, string topicId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetEntryIdsByTopicIdAsync(string topicId, CancellationToken ct = default);
}
