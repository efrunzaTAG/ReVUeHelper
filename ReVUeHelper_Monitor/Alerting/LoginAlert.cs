namespace ReVUeHelper_Monitor.Alerting;

/// <summary>
/// A resolved login event ready for delivery decisions. Times are in the configured time zone.
/// </summary>
public sealed class LoginAlert
{
    public long LogEventId { get; set; }
    public string Username { get; set; } = "";
    public string? MachineName { get; set; }

    /// <summary>When the login happened (parsed from [Date], or observed time as fallback).</summary>
    public DateTimeOffset OccurredAt { get; set; }

    public override string ToString() =>
        $"{Username}" + (string.IsNullOrEmpty(MachineName) ? "" : $" on {MachineName}") +
        $" at {OccurredAt:yyyy-MM-dd HH:mm}";
}
