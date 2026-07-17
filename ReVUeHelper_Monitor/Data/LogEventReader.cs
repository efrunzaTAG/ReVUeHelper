using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using ReVUeHelper_Monitor.Configuration;

namespace ReVUeHelper_Monitor.Data;

/// <summary>
/// Tails dbo.LogEvents for login rows using a LogEventId watermark and WITH (NOLOCK).
/// Read-only: this class never writes to the ReVue database.
/// </summary>
public sealed class LogEventReader
{
    private readonly MonitorOptions _opt;

    public LogEventReader(IOptions<MonitorOptions> opt) => _opt = opt.Value;

    /// <summary>Highest LogEventId currently in the table (used to seed the watermark on first run).</summary>
    public async Task<long> GetCurrentMaxIdAsync(CancellationToken ct)
    {
        const string sql = "SELECT ISNULL(MAX(LogEventId), 0) FROM dbo.LogEvents WITH (NOLOCK);";
        await using var conn = new SqlConnection(_opt.ConnectionString);
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, cancellationToken: ct));
    }

    /// <summary>
    /// Returns new login rows with LogEventId &gt; <paramref name="afterId"/>, oldest first,
    /// for the watched usernames. Empty list if the watchlist is empty.
    /// </summary>
    public async Task<IReadOnlyList<LogEvent>> ReadNewAsync(
        long afterId, IReadOnlyCollection<string> watchUsers, CancellationToken ct)
    {
        if (watchUsers.Count == 0)
            return Array.Empty<LogEvent>();

        const string sql = @"
SELECT TOP (@take)
       LogEventId, Username, Logger, Level, MachineName, [Message], [Date]
FROM dbo.LogEvents WITH (NOLOCK)
WHERE LogEventId > @afterId
  AND Logger = @logger
  AND Username IN @users
ORDER BY LogEventId ASC;";

        await using var conn = new SqlConnection(_opt.ConnectionString);
        var rows = await conn.QueryAsync<LogEvent>(new CommandDefinition(sql, new
        {
            take = _opt.BatchSize,
            afterId,
            logger = _opt.Logger,
            users = watchUsers
        }, commandTimeout: 60, cancellationToken: ct));

        return rows.ToList();
    }
}
