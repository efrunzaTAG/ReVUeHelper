using System.Globalization;
using Microsoft.Extensions.Options;
using ReVUeHelper_Monitor.Alerting;
using ReVUeHelper_Monitor.Configuration;
using ReVUeHelper_Monitor.Data;
using ReVUeHelper_Monitor.State;

namespace ReVUeHelper_Monitor;

/// <summary>
/// The service loop: poll dbo.LogEvents for new watched logins, hand each to the scheduler,
/// advance the watermark, and try to release any queued SMS. Poll cadence is fast during
/// business hours and sparse otherwise. Any per-cycle error is logged and the loop continues.
/// </summary>
public sealed class Worker : BackgroundService
{
    private readonly MonitorOptions _opt;
    private readonly LogEventReader _reader;
    private readonly SecretsProvider _secrets;
    private readonly StateStore _state;
    private readonly NotificationScheduler _scheduler;
    private readonly MonitorClock _clock;
    private readonly ILogger<Worker> _log;

    private readonly HashSet<DayOfWeek> _businessDays;
    private readonly TimeOnly _dayStart;
    private readonly TimeOnly _dayEnd;

    public Worker(
        IOptions<MonitorOptions> opt,
        LogEventReader reader,
        SecretsProvider secrets,
        StateStore state,
        NotificationScheduler scheduler,
        MonitorClock clock,
        ILogger<Worker> log)
    {
        _opt = opt.Value;
        _reader = reader;
        _secrets = secrets;
        _state = state;
        _scheduler = scheduler;
        _clock = clock;
        _log = log;

        _businessDays = _opt.Poll.BusinessDays
            .Select(d => Enum.TryParse<DayOfWeek>(d, true, out var v) ? (DayOfWeek?)v : null)
            .Where(v => v.HasValue).Select(v => v!.Value).ToHashSet();
        _dayStart = TimeOnly.TryParse(_opt.Poll.DayStart, CultureInfo.InvariantCulture, out var s) ? s : new TimeOnly(8, 0);
        _dayEnd = TimeOnly.TryParse(_opt.Poll.DayEnd, CultureInfo.InvariantCulture, out var e) ? e : new TimeOnly(19, 0);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var watchUsers = _secrets.GetWatchUsers();
        _log.LogInformation("ReVUe Monitor starting. Watching {Count} user(s), Logger='{Logger}'.",
            watchUsers.Count, _opt.Logger);

        var wm = _state.LoadWatermark();
        if (wm.LastLogEventId == 0 && _opt.SeedFromCurrentMaxOnFirstRun)
        {
            wm.LastLogEventId = await _reader.GetCurrentMaxIdAsync(stoppingToken);
            _state.SaveWatermark(wm);
            _log.LogInformation("First run: seeded watermark to current MAX(LogEventId) = {Id}. " +
                                "Only logins after this point will alert.", wm.LastLogEventId);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var events = await _reader.ReadNewAsync(wm.LastLogEventId, watchUsers, stoppingToken);
                foreach (var e in events)
                {
                    var alert = new LoginAlert
                    {
                        LogEventId = e.LogEventId,
                        Username = e.Username ?? "(unknown)",
                        MachineName = e.MachineName,
                        OccurredAt = _clock.ResolveEventTime(e.Date, _clock.UtcNow)
                    };
                    _log.LogInformation("Detected login: {Alert} (LogEventId {Id})", alert, alert.LogEventId);
                    await _scheduler.HandleAsync(alert, stoppingToken);

                    wm.LastLogEventId = e.LogEventId;
                    _state.SaveWatermark(wm); // persist per row so a mid-batch crash never re-alerts
                }

                // Release any queued push whose window/cooldown is now satisfied (e.g. overnight -> 08:30).
                await _scheduler.TryFlushPushAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Poll cycle failed; will retry next interval.");
            }

            await DelayAsync(stoppingToken);
        }

        _log.LogInformation("ReVUe Monitor stopping.");
    }

    private async Task DelayAsync(CancellationToken ct)
    {
        var minutes = IsWeekdayDay(_clock.NowLocal)
            ? _opt.Poll.DayIntervalMinutes       // weekday 08:00-19:00 -> every 5 min
            : _opt.Poll.OffHoursIntervalMinutes; // weekday nights + all weekend -> hourly
        try { await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, minutes)), ct); }
        catch (OperationCanceledException) { /* shutting down */ }
    }

    /// <summary>True during the weekday day window; false on weekday nights and all weekend.</summary>
    private bool IsWeekdayDay(DateTimeOffset localNow) =>
        _businessDays.Contains(localNow.DayOfWeek) &&
        MonitorClock.InWindow(localNow, _dayStart, _dayEnd);
}
