using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace Dhblog.Api.Settings;

public class SsmAppSettingsProvider : IAppSettingsProvider
{
    private readonly IAmazonSimpleSystemsManagement _ssm;
    private readonly string _prefix;
    private Dictionary<string, string> _cache = new(AppSettingsKeys.Defaults);

    public SsmAppSettingsProvider(IAmazonSimpleSystemsManagement ssm, IConfiguration configuration)
    {
        _ssm = ssm;
        var env = configuration["DHBLOG_ENV"] ?? "prod";
        _prefix = $"/dhblog/{env}/settings/";
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var result = new Dictionary<string, string>(AppSettingsKeys.Defaults);
        try
        {
            var response = await _ssm.GetParametersByPathAsync(new GetParametersByPathRequest
            {
                Path = _prefix,
                Recursive = true,
                WithDecryption = true
            }, ct);

            foreach (var p in response.Parameters)
            {
                var key = p.Name.Replace(_prefix, "", StringComparison.Ordinal);
                result[key] = p.Value;
            }
        }
        catch
        {
            // Fall back to defaults if SSM unavailable
        }

        _cache = result;
    }

    public Task<string?> GetAsync(string key, CancellationToken ct = default) =>
        Task.FromResult(_cache.TryGetValue(key, out var v) ? v : null);

    public Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>(_cache));

    public async Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        await _ssm.PutParameterAsync(new PutParameterRequest
        {
            Name = _prefix + key,
            Value = value,
            Type = ParameterType.String,
            Overwrite = true
        }, ct);
        _cache[key] = value;
    }
}
