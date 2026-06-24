using Dhblog.Database.Entities;

namespace Dhblog.DataAccess;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(string roleId, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct = default);
    Task CreateAsync(Role role, CancellationToken ct = default);
    Task UpdateAsync(Role role, CancellationToken ct = default);
    Task DeleteAsync(string roleId, CancellationToken ct = default);
}

public interface IFeatureRepository
{
    Task<Feature?> GetByIdAsync(string featureId, CancellationToken ct = default);
    Task<Feature?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Feature>> ListAsync(CancellationToken ct = default);
}

public interface IFeatureRoleRepository
{
    Task<IReadOnlyList<FeatureRole>> GetByRoleIdAsync(string roleId, CancellationToken ct = default);
    Task CreateAsync(FeatureRole featureRole, CancellationToken ct = default);
    Task DeleteAsync(string roleId, string featureId, CancellationToken ct = default);
}
