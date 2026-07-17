namespace ReVUeHelper_Monitor.Data;

/// <summary>
/// One row of dbo.LogEvents that matched the login watch (Logger + Username).
/// Column names map 1:1 to the table via Dapper.
/// </summary>
public sealed class LogEvent
{
    public long LogEventId { get; set; }
    public string? Username { get; set; }
    public string? Logger { get; set; }
    public string? Level { get; set; }
    public string? MachineName { get; set; }
    public string? Message { get; set; }

    /// <summary>Raw value of the varchar(100) [Date] column, as stored by ReVue.</summary>
    public string? Date { get; set; }
}
