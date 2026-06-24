namespace Dhblog.Api.Settings;

public class LocalAppSettingsProvider : IAppSettingsProvider
{
    private readonly Dictionary<string, string> _cache;

    public LocalAppSettingsProvider(IConfiguration configuration)
    {
        _cache = new Dictionary<string, string>(AppSettingsKeys.Defaults);
        foreach (var key in AppSettingsKeys.All)
        {
            var val = configuration[key] ?? Environment.GetEnvironmentVariable(key.ToUpperInvariant());
            if (!string.IsNullOrEmpty(val))
                _cache[key] = val;
        }
    }

    public Task<string?> GetAsync(string key, CancellationToken ct = default) =>
        Task.FromResult(_cache.TryGetValue(key, out var v) ? v : null);

    public Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>(_cache));

    public Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        _cache[key] = value;
        return Task.CompletedTask;
    }

    public Task RefreshAsync(CancellationToken ct = default) => Task.CompletedTask;
}
