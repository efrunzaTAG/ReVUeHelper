namespace ReVUeHelper_Monitor.Configuration;

/// <summary>
/// Strongly-typed view of the "Monitor" section of appsettings.json.
/// </summary>
public sealed class MonitorOptions
{
    public const string SectionName = "Monitor";

    public string ConnectionString { get; set; } = "";
    public string StateDirectory { get; set; } = @"C:\ProgramData\ReVUeHelper_Monitor";
    public string Logger { get; set; } = "WhoAmI";
    public int BatchSize { get; set; } = 200;
    public bool SeedFromCurrentMaxOnFirstRun { get; set; } = true;
    public string TimeZone { get; set; } = "Eastern Standard Time";

    /// <summary>Optional exact format for parsing the varchar [Date] column. Empty = best-effort TryParse.</summary>
    public string EventDateFormat { get; set; } = "";

    public PollOptions Poll { get; set; } = new();
    public EmailOptions Email { get; set; } = new();
    public NtfyOptions Ntfy { get; set; } = new();

    public string SecretsFilePath => System.IO.Path.Combine(StateDirectory, "secrets.json");
}

public sealed class PollOptions
{
    /// <summary>Weekdays. Anything not listed is treated as "weekend": off-hours cadence + email-only.</summary>
    public string[] BusinessDays { get; set; } = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };

    /// <summary>Weekday "day" window. Inside it we poll every <see cref="DayIntervalMinutes"/>.</summary>
    public string DayStart { get; set; } = "08:00";
    public string DayEnd { get; set; } = "19:00";

    /// <summary>Poll cadence during the weekday day window.</summary>
    public int DayIntervalMinutes { get; set; } = 5;

    /// <summary>Poll cadence for weekday nights (outside the day window) AND all weekend — the soft, hourly ping.</summary>
    public int OffHoursIntervalMinutes { get; set; } = 60;
}

public sealed class EmailOptions
{
    public bool Enabled { get; set; } = true;
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 25;
    public bool UseSsl { get; set; }
    public string From { get; set; } = "";
    public string[] To { get; set; } = Array.Empty<string>();
}

/// <summary>
/// The urgent, windowed channel: an ntfy.sh push. Same delivery rules the SMS channel had
/// (window + cooldown + queue/release + weekend suppression). The access token is NOT here —
/// it is a secret read from secrets.json via <see cref="ReVUeHelper_Monitor.State.SecretsProvider"/>.
/// </summary>
public sealed class NtfyOptions
{
    public bool Enabled { get; set; }

    /// <summary>ntfy server. Public server is the easy path with instant iOS delivery.</summary>
    public string BaseUrl { get; set; } = "https://ntfy.sh";

    /// <summary>Topic to publish to (the part after the host in the ntfy URL).</summary>
    public string Topic { get; set; } = "";

    /// <summary>Sent as the ntfy "Title" header.</summary>
    public string Title { get; set; } = "ReVUe login";

    /// <summary>Sent as the ntfy "Priority" header (1=min .. 5=max/urgent).</summary>
    public int Priority { get; set; } = 5;

    public string WindowStart { get; set; } = "08:30";
    public string WindowEnd { get; set; } = "21:00";
    public double CooldownHours { get; set; } = 3;

    /// <summary>Weekends are email-only: the push is skipped entirely on non-business days.</summary>
    public bool SuppressOnWeekends { get; set; } = true;
}
