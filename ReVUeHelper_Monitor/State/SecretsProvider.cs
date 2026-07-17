using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReVUeHelper_Monitor.Configuration;

namespace ReVUeHelper_Monitor.State;

/// <summary>
/// Resolves the watched usernames without ever committing them to source control.
/// Resolution order (first non-empty wins):
///   1. secrets.json in StateDirectory  -> { "WatchUsers": [ ... ] }   (server: the no-reboot option)
///   2. env var REVUE_MONITOR_WATCH_USER (comma-separated)             (matches the Explore project pattern)
///   3. IConfiguration "Monitor:WatchUsers"                            (dotnet user-secrets, for local dev)
/// </summary>
public sealed class SecretsProvider
{
    private const string EnvVar = "REVUE_MONITOR_WATCH_USER";

    private readonly MonitorOptions _opt;
    private readonly IConfiguration _config;
    private readonly ILogger<SecretsProvider> _log;

    public SecretsProvider(IOptions<MonitorOptions> opt, IConfiguration config, ILogger<SecretsProvider> log)
    {
        _opt = opt.Value;
        _config = config;
        _log = log;
    }

    public IReadOnlyList<string> GetWatchUsers()
    {
        var users = FromSecretsFile() ?? FromEnvVar() ?? FromConfig() ?? new List<string>();
        var cleaned = users
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (cleaned.Count == 0)
            _log.LogWarning(
                "No watched usernames resolved. Create {Path} with {{\"WatchUsers\":[...]}} " +
                "or set env var {Env}. Nothing will be monitored until then.",
                _opt.SecretsFilePath, EnvVar);

        return cleaned;
    }

    private List<string>? FromSecretsFile()
    {
        try
        {
            if (!File.Exists(_opt.SecretsFilePath)) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(_opt.SecretsFilePath));
            if (!doc.RootElement.TryGetProperty("WatchUsers", out var arr) ||
                arr.ValueKind != JsonValueKind.Array)
                return null;
            return arr.EnumerateArray()
                      .Where(e => e.ValueKind == JsonValueKind.String)
                      .Select(e => e.GetString()!)
                      .ToList();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to read secrets file {Path}.", _opt.SecretsFilePath);
            return null;
        }
    }

    private static List<string>? FromEnvVar()
    {
        var raw = Environment.GetEnvironmentVariable(EnvVar);
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private List<string>? FromConfig()
    {
        var users = _config.GetSection("Monitor:WatchUsers").Get<string[]>();
        return users is { Length: > 0 } ? users.ToList() : null;
    }
}
