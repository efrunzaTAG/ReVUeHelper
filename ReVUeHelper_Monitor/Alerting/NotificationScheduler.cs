using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReVUeHelper_Monitor.Configuration;
using ReVUeHelper_Monitor.State;

namespace ReVUeHelper_Monitor.Alerting;

/// <summary>
/// Applies the delivery rules confirmed with the user:
///   EMAIL: every matching login, no cooldown, any time of day.
///   PUSH (ntfy): only within [WindowStart, WindowEnd]; a 3h cooldown between sends; events seen
///          outside the window or during cooldown are queued and released as ONE summary push
///          when the window is open and the cooldown has elapsed; suppressed entirely on weekends.
/// The queued alerts + last-sent time are persisted so a restart never drops a queued alert.
/// </summary>
public sealed class NotificationScheduler
{
    private readonly MonitorOptions _opt;
    private readonly EmailNotifier _email;
    private readonly NtfyNotifier _push;
    private readonly StateStore _state;
    private readonly MonitorClock _clock;
    private readonly ILogger<NotificationScheduler> _log;

    private readonly object _gate = new();
    private readonly TimeOnly _windowStart;
    private readonly TimeOnly _windowEnd;
    private readonly TimeSpan _cooldown;
    private readonly bool _suppressWeekends;
    private readonly HashSet<DayOfWeek> _businessDays;
    private PendingState _pending;

    public NotificationScheduler(
        IOptions<MonitorOptions> opt,
        EmailNotifier email,
        NtfyNotifier push,
        StateStore state,
        MonitorClock clock,
        ILogger<NotificationScheduler> log)
    {
        _opt = opt.Value;
        _email = email;
        _push = push;
        _state = state;
        _clock = clock;
        _log = log;

        _windowStart = ParseTime(_opt.Ntfy.WindowStart, new TimeOnly(8, 30));
        _windowEnd = ParseTime(_opt.Ntfy.WindowEnd, new TimeOnly(21, 0));
        _cooldown = TimeSpan.FromHours(Math.Max(0, _opt.Ntfy.CooldownHours));
        _suppressWeekends = _opt.Ntfy.SuppressOnWeekends;
        _businessDays = _opt.Poll.BusinessDays
            .Select(d => Enum.TryParse<DayOfWeek>(d, true, out var v) ? (DayOfWeek?)v : null)
            .Where(v => v.HasValue).Select(v => v!.Value).ToHashSet();
        _pending = _state.LoadPending();

        if (_pending.QueuedAlerts.Count > 0)
            _log.LogInformation("Recovered {Count} queued push alert(s) from previous run.", _pending.QueuedAlerts.Count);
    }

    /// <summary>Handle one newly-detected login: email now, queue the push for its window.</summary>
    public async Task HandleAsync(LoginAlert alert, CancellationToken ct)
    {
        // EMAIL — immediate, every time, no cooldown.
        if (_email.Enabled)
        {
            var subject = $"ReVUe login: {alert.Username}";
            var body =
                $"User:       {alert.Username}\n" +
                $"Machine:    {alert.MachineName}\n" +
                $"When:       {alert.OccurredAt:yyyy-MM-dd HH:mm:ss}\n" +
                $"LogEventId: {alert.LogEventId}\n";
            try { await _email.SendAsync(subject, body, ct); }
            catch (Exception ex) { _log.LogError(ex, "Email send failed for LogEventId {Id}.", alert.LogEventId); }
        }

        // PUSH — queue, then try to release. Skipped entirely on weekends (email-only) when configured.
        if (_push.Enabled && !IsWeekendSuppressed())
        {
            lock (_gate)
            {
                _pending.QueuedAlerts.Add(alert);
                _state.SavePending(_pending);
            }
            await TryFlushPushAsync(ct);
        }
    }

    /// <summary>
    /// Release the queued alerts as a single summary push if the window is open and the cooldown
    /// has elapsed. Safe to call every poll tick (that is how an overnight queue gets delivered at
    /// window-open even when no new event arrives).
    /// </summary>
    public async Task TryFlushPushAsync(CancellationToken ct)
    {
        if (!_push.Enabled || IsWeekendSuppressed()) return;

        string title, body;
        int count;
        lock (_gate)
        {
            if (_pending.QueuedAlerts.Count == 0) return;

            var localNow = _clock.NowLocal;
            if (!MonitorClock.InWindow(localNow, _windowStart, _windowEnd))
                return; // outside window -> keep queued

            if (_pending.LastPushSentUtc is { } last && _clock.UtcNow - last < _cooldown)
                return; // still cooling down -> keep queued

            count = _pending.QueuedAlerts.Count;
            (title, body) = BuildSummary(_pending.QueuedAlerts);
        }

        try
        {
            await _push.SendAsync(title, body, ct);
            lock (_gate)
            {
                _pending.LastPushSentUtc = _clock.UtcNow;
                _pending.QueuedAlerts.Clear();
                _state.SavePending(_pending);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Push send failed; keeping {Count} alert(s) queued.", count);
        }
    }

    /// <summary>
    /// Lite summary. Single: title "ReVUe login: {user}", body "{HH:mm}".
    /// Many: title "ReVUe: {n} logins", body "{distinct users} · latest {HH:mm}" (time only, no date).
    /// </summary>
    private (string title, string body) BuildSummary(IReadOnlyList<LoginAlert> alerts)
    {
        var latest = alerts[^1];
        if (alerts.Count == 1)
            return ($"{_opt.Ntfy.Title}: {latest.Username}", $"{latest.OccurredAt:HH:mm}");

        var users = string.Join(", ", alerts.Select(a => a.Username).Distinct(StringComparer.OrdinalIgnoreCase));
        return ($"{_opt.Ntfy.Title}: {alerts.Count} logins", $"{users} · latest {latest.OccurredAt:HH:mm}");
    }

    /// <summary>True when today is a weekend and the push is configured to be suppressed on weekends.</summary>
    private bool IsWeekendSuppressed() =>
        _suppressWeekends && !_businessDays.Contains(_clock.NowLocal.DayOfWeek);

    private static TimeOnly ParseTime(string value, TimeOnly fallback) =>
        TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out var t) ? t : fallback;
}
