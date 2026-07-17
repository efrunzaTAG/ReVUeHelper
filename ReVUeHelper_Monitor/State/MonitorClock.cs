using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReVUeHelper_Monitor.Configuration;

namespace ReVUeHelper_Monitor.State;

/// <summary>
/// Central time authority: wraps the configured time zone and all window math so the poll loop,
/// the scheduler, and event-time parsing agree on "now" and on local times.
/// </summary>
public sealed class MonitorClock
{
    private readonly MonitorOptions _opt;
    private readonly ILogger<MonitorClock> _log;
    private readonly TimeZoneInfo _tz;

    public MonitorClock(IOptions<MonitorOptions> opt, ILogger<MonitorClock> log)
    {
        _opt = opt.Value;
        _log = log;
        try
        {
            _tz = TimeZoneInfo.FindSystemTimeZoneById(_opt.TimeZone);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unknown TimeZone '{Tz}'; falling back to server local time.", _opt.TimeZone);
            _tz = TimeZoneInfo.Local;
        }
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateTimeOffset NowLocal => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _tz);

    public DateTimeOffset ToLocal(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, _tz);

    /// <summary>
    /// Resolves an event's timestamp from the raw varchar [Date] value. Falls back to
    /// <paramref name="observed"/> (the time the service saw the row) when parsing fails.
    /// </summary>
    public DateTimeOffset ResolveEventTime(string? rawDate, DateTimeOffset observed)
    {
        if (string.IsNullOrWhiteSpace(rawDate))
            return ToLocal(observed);

        var styles = System.Globalization.DateTimeStyles.AssumeLocal;

        if (!string.IsNullOrWhiteSpace(_opt.EventDateFormat))
        {
            if (DateTime.TryParseExact(rawDate, _opt.EventDateFormat,
                    System.Globalization.CultureInfo.InvariantCulture, styles, out var exact))
                return ToLocal(new DateTimeOffset(DateTime.SpecifyKind(exact, DateTimeKind.Unspecified), _tz.GetUtcOffset(exact)));
        }

        if (DateTime.TryParse(rawDate, System.Globalization.CultureInfo.InvariantCulture, styles, out var parsed))
            return ToLocal(new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified), _tz.GetUtcOffset(parsed)));

        _log.LogDebug("Could not parse [Date] '{Raw}'; using observed time.", rawDate);
        return ToLocal(observed);
    }

    /// <summary>True if <paramref name="localTime"/>'s clock time falls within [start, end).</summary>
    public static bool InWindow(DateTimeOffset localTime, TimeOnly start, TimeOnly end)
    {
        var t = TimeOnly.FromDateTime(localTime.DateTime);
        return start <= end
            ? t >= start && t < end          // normal same-day window (e.g. 08:30-21:00)
            : t >= start || t < end;         // overnight-spanning window (start > end)
    }

    /// <summary>The next instant at or after <paramref name="localNow"/> that the window is open.</summary>
    public static DateTimeOffset NextWindowOpen(DateTimeOffset localNow, TimeOnly start, TimeOnly end)
    {
        if (InWindow(localNow, start, end)) return localNow;
        var todayStart = new DateTimeOffset(localNow.Year, localNow.Month, localNow.Day,
            start.Hour, start.Minute, 0, localNow.Offset);
        return localNow < todayStart ? todayStart : todayStart.AddDays(1);
    }
}
