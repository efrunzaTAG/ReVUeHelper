using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReVUeHelper_Monitor.Alerting;
using ReVUeHelper_Monitor.Configuration;

namespace ReVUeHelper_Monitor.State;

/// <summary>Persisted watermark: the highest LogEventId we have fully processed.</summary>
public sealed class WatermarkState
{
    public long LastLogEventId { get; set; }
}

/// <summary>
/// Persisted push (ntfy) delivery state: events waiting for their window/cooldown, and when the
/// last push was actually sent. Persisted so a restart never loses a queued overnight alert.
/// </summary>
public sealed class PendingState
{
    public List<LoginAlert> QueuedAlerts { get; set; } = new();
    public DateTimeOffset? LastPushSentUtc { get; set; }
}

/// <summary>
/// Reads/writes local JSON state files under StateDirectory. All access is serialized so the
/// poll loop and the flush timer can share it safely.
/// </summary>
public sealed class StateStore
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private readonly object _gate = new();
    private readonly string _watermarkPath;
    private readonly string _pendingPath;
    private readonly ILogger<StateStore> _log;

    public StateStore(IOptions<MonitorOptions> opt, ILogger<StateStore> log)
    {
        _log = log;
        var dir = opt.Value.StateDirectory;
        Directory.CreateDirectory(dir);
        _watermarkPath = Path.Combine(dir, "watermark.json");
        _pendingPath = Path.Combine(dir, "pending.json");
    }

    public WatermarkState LoadWatermark() => Load<WatermarkState>(_watermarkPath) ?? new WatermarkState();

    public void SaveWatermark(WatermarkState state) => Save(_watermarkPath, state);

    public PendingState LoadPending() => Load<PendingState>(_pendingPath) ?? new PendingState();

    public void SavePending(PendingState state) => Save(_pendingPath, state);

    private T? Load<T>(string path) where T : class
    {
        lock (_gate)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json, JsonOpts);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to read state file {Path}; treating as empty.", path);
                return null;
            }
        }
    }

    private void Save<T>(string path, T state)
    {
        lock (_gate)
        {
            // Write to a temp file then move, so a crash mid-write can't corrupt the state file.
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(state, JsonOpts));
            File.Move(tmp, path, overwrite: true);
        }
    }
}
